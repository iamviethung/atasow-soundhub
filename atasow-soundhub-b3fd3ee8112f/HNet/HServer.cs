using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using Windows.Networking;
using Windows.Networking.Sockets;
using System.Diagnostics;

using HNet.Converters;

namespace HNet
{
    public class HClientInformation
    {
        bool connectState;

        #region Properties
        public bool Closing { get; private set; }
        private ShortRatingConverter Converter { get; set; }
        public string Name { get; private set; }
        public float Rate { get; internal set; }
        public Guid Guid { get; private set; }
        public bool LackOfResponse { get; private set; }
        public int LackOfReponseTime { get; private set; }
        #endregion

        #region Events
        public event EventHandler RateUpdated;
        public event EventHandler SilentReceived;
        #endregion

        public HClientInformation(Guid guid, string name)
        {
            Guid = guid;
            Name = name;
            Rate = 0;
            Closing = false;
            ResetConnectState();
        }

        public bool Close()
        {
            if (!LackOfResponse)
                Closing = true;

            return LackOfResponse;
        }
        public void Remove()
        {
            if (Converter != null)
                Converter.Stop();
            Converter = null;
        }

        public bool ResetConnectState() {
            LackOfResponse = !connectState;
            connectState = false;

            if (LackOfResponse)
                LackOfReponseTime++;
            else
                LackOfReponseTime = 0;

            return LackOfResponse;
        }
        public void SilentPacket()
        {
            connectState = true;
            if (SilentReceived != null)
                SilentReceived(this, null);
        }
        public void DataPacket(byte[] buffer)
        {
            connectState = true;
            if (Converter != null)
                Converter.Write(buffer);
        }

        public void SetConverter(ShortConverter converter)
        {
            if (converter != null)
            {
                Converter = new ShortRatingConverter(converter);
                Converter.RateCalculated += Converter_RateCalculated;
            }
            else
                Converter = null;
        }
        void Converter_RateCalculated(object sender, RateCalculatedEventArgs e)
        {
            Rate = (float)e.Rate;
            ResetConnectState();

            if (RateUpdated != null)
                RateUpdated(this, null);
        } 
    }

    public enum ServerMessageHandler
    {
        Ping,
        Connect,
        Close,
        Data,
        Silent
    }

    public sealed class HServer
    {
        #region Variables
        Guid serverGuid;
        string serverName;
        List<DatagramSocket> sockets;
        Dictionary<Guid, HClientInformation> clientList;

        byte[] serverHeaderPING, serverHeaderNAME, serverHeaderCLSE;

        const int MAX_BUFFER = 10000;
        #endregion

        public event EventHandler<HClientInformation> ProfileRequest;
        public event EventHandler<Guid> ProfileRemove;

        public HServer(Guid guid, string name) {
            serverGuid = guid;
            serverName = name;
            serverHeaderPING = Encoding.UTF8.GetBytes("SHSvPING");
            serverHeaderNAME = Encoding.UTF8.GetBytes("SHSvNAME");
            serverHeaderCLSE = Encoding.UTF8.GetBytes("SHSvCLSE");

            sockets = new List<DatagramSocket>();
            var ignore = ResetAsync();
        }

        #region Public methods
        public async Task BindPortAsync(HostName hostname, string port)
        {
            DatagramSocket socket = new DatagramSocket();
            socket.Control.QualityOfService = SocketQualityOfService.LowLatency;
            socket.MessageReceived += server_MessageReceived;

            await socket.BindEndpointAsync(hostname, port);
            sockets.Add(socket);
        }
        public void Close()
        {
            foreach (var socket in sockets)
                socket.Dispose();
        }
        public void Close(Guid guid)
        {
            if (clientList.ContainsKey(guid))
            {
                if (clientList[guid].Close())
                    Remove(guid);
            }
            else
                throw new InvalidOperationException("Guid's not exist");
        }
        public async Task ResetAsync()
        {
            // Send CLOSE packet to available clients
            if (clientList != null)
            {
                foreach (var client in clientList)
                    Close(client.Key);

                await Task.Delay(50);
            }

            // Re-initialize a new socket
            Close();
            sockets.Clear();

            clientList = new Dictionary<Guid, HClientInformation>();
        }
        public HClientInformation GetClient(Guid guid)
        {
            return clientList[guid];
        }
        #endregion

        #region Private methods
        private async void server_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            // Get InputStream
            BinaryReader reader;
            try
            {
                reader = new BinaryReader(args.GetDataStream().AsStreamForRead());
            }
            catch (Exception e)
            {
                if (SocketError.GetStatus(e.HResult) != SocketErrorStatus.ConnectionResetByPeer)
                    throw;
                return;
            }

            byte[] header = reader.ReadBytes(8);
            string headerstr = Encoding.UTF8.GetString(header, 0, header.Length);
            Guid guid = new Guid(reader.ReadBytes(16));
            ServerMessageHandler handler = ServerMessageHandler.Close;

            // Identify the handler
            if (clientList.ContainsKey(guid) && clientList[guid].Closing)
                handler = ServerMessageHandler.Close;
            else if (!clientList.ContainsKey(guid) && headerstr != "SHClCONN" && headerstr != "SHClPING")
                handler = ServerMessageHandler.Close;
            else
            {
                if (headerstr == "SHClPING") handler = ServerMessageHandler.Ping;
                if (headerstr == "SHClCONN") handler = ServerMessageHandler.Connect;
                if (headerstr == "SHClCLSE") handler = ServerMessageHandler.Close;
                if (headerstr == "SHClDATA") handler = ServerMessageHandler.Data;
                if (headerstr == "SHClSLNT") handler = ServerMessageHandler.Silent;
            }

            BinaryWriter writer = null;
            // For data, we don't need to get the output stream
            if (handler != ServerMessageHandler.Data && handler != ServerMessageHandler.Silent)
            {
                var outputStream = await sender.GetOutputStreamAsync(args.RemoteAddress, args.RemotePort);
                writer = new BinaryWriter(outputStream.AsStreamForWrite());
            }

            // Handle the message
            switch (handler)
            {
                case ServerMessageHandler.Close:
                    Header_CLSE(guid, writer);
                    break;
                case ServerMessageHandler.Connect:
                    Header_CONN(guid, reader, writer);
                    break;
                case ServerMessageHandler.Ping:
                    Header_PING(guid, writer);
                    break;
                case ServerMessageHandler.Data:
                    Header_DATA(guid, reader);
                    break;
                case ServerMessageHandler.Silent:
                    Header_SLNT(guid);
                    break;
            }
        }
        private void Header_PING(Guid guid, BinaryWriter writer)
        {
            Log(guid, "PING received");

            // PING response
            writer.Write(serverHeaderPING);
            writer.Write(serverGuid.ToByteArray());
            writer.Write(serverName);
            writer.Flush();

            Log(guid, "PING sent");
        }
        private void Header_CONN(Guid guid, BinaryReader reader, BinaryWriter writer)
        {
            // Add GUID, Name and request profile
            byte[] buffer = reader.ReadBytes(MAX_BUFFER);
            string name = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            Log(guid, "CONN received");

            HClientInformation client = new HClientInformation(guid, name);
            if (!clientList.ContainsKey(guid))
                clientList.Add(guid, client);

            Log(guid, "Profile request for \"" + name + "\"");
            if (ProfileRequest != null)
                ProfileRequest(this, client);

            // NAME response
            writer.Write(serverHeaderNAME);
            writer.Write(serverGuid.ToByteArray());
            writer.Write(serverName);
            writer.Flush();

            Log(guid, "NAME sent");
        }
        private void Header_CLSE(Guid guid, BinaryWriter writer)
        {
            // Remove profile
            Log(guid, "CLOSE received");

            if (clientList.ContainsKey(guid))
                Remove(guid);

            // CLOSE response
            writer.Write(serverHeaderCLSE);
            writer.Write(serverGuid.ToByteArray());
            writer.Flush();

            Log(guid, "CLOSE sent");
        }
        private void Header_DATA(Guid guid, BinaryReader reader)
        {
            if (!clientList.ContainsKey(guid))
                return;

            if (clientList[guid] != null)
            {
                byte[] buffer = reader.ReadBytes(MAX_BUFFER);
                clientList[guid].DataPacket(buffer);
            }
        }
        private void Header_SLNT(Guid guid)
        {
            if (!clientList.ContainsKey(guid))
                return;

            if (clientList[guid] != null)
                clientList[guid].SilentPacket();
        }
        private void Log(Guid guid, string msg)
        {
            if (Logger != null)
                Logger.Write("Server to " + guid.ToString() + " : " + msg);
        }

        void Remove(Guid guid)
        {
            if (clientList.ContainsKey(guid))
            {
                clientList[guid].Remove();
                clientList.Remove(guid);
                if (ProfileRemove != null)
                    ProfileRemove(this, guid);
                Log(guid, "Profile remove");
            }
        }
        #endregion

        #region Properties
        public Logger Logger
        {
            get;
            set;
        }
        #endregion
    }
}
