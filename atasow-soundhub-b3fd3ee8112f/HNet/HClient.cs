using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using Windows.Networking;
using Windows.Networking.Sockets;

using HNet.Converters;
using System.Diagnostics;

namespace HNet
{
    public class HServerInformation
    {
        #region Properties
        public DatagramSocket Socket { get; set; }
        public ShortStreamConverter Converter { get; set; }
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string IP { get; set; }
        #endregion
    }

    public struct HClientResponseWithNameAndIP
    {
        public Guid GUID;
        public string Name;
        public string IP;
    }
    public struct ScanProgressEventArgs
    {
        public int Scanned { get; internal set; }
        public string LastIP { get; internal set; }
    }

    public sealed class HClient : ShortConverter
    {
        #region Constants
        const int CONNECT_TIMEOUT = 1000; // ms
        const int SCAN_LIMIT = 10;
        #endregion

        #region Variables
        Guid clientGuid;
        string clientName;
        List<HServerInformation> serverList;

        byte[] clientPINGPacket,
            clientDATAHeader,
            clientCONNHeader,
            clientCLSEPacket,
            clientSLNTPacket;
        TaskCompletionSource<HClientResponseWithNameAndIP> nameResponse;

        const int MAX_BUFFER = 100;

        // Test connectivity mode variables
        bool testConnectivityMode = false;
        List<Guid> responseServers = new List<Guid>();

        // Scan mode variables
        bool scanMode = false;
        bool scanCancel = false;
        int scanTotal, scanned;
        int scanNow;

        public event EventHandler<Guid> Connected;
        public event EventHandler<Guid> Disconnected;
        public event EventHandler<HClientResponseWithNameAndIP> PingReceived;
        public event EventHandler ScanningStarted;
        public event EventHandler<ScanProgressEventArgs> ScanProgressUpdated;
        #endregion
        
        public HClient(Guid guid, string name)
        {
            clientGuid = guid;
            clientName = name;
            Enable = false;

            byte[] clientGuidData = clientGuid.ToByteArray();

            clientPINGPacket = new byte[24];
            Buffer.BlockCopy(Encoding.UTF8.GetBytes("SHClPING"), 0, clientPINGPacket, 0, 8);
            Buffer.BlockCopy(clientGuidData, 0, clientPINGPacket, 8, 16);

            clientCONNHeader = new byte[24];
            Buffer.BlockCopy(Encoding.UTF8.GetBytes("SHClCONN"), 0, clientCONNHeader, 0, 8);
            Buffer.BlockCopy(clientGuidData, 0, clientCONNHeader, 8, 16);

            clientCLSEPacket = new byte[24];
            Buffer.BlockCopy(Encoding.UTF8.GetBytes("SHClCLSE"), 0, clientCLSEPacket, 0, 8);
            Buffer.BlockCopy(clientGuidData, 0, clientCLSEPacket, 8, 16);

            clientSLNTPacket = new byte[24];
            Buffer.BlockCopy(Encoding.UTF8.GetBytes("SHClSLNT"), 0, clientSLNTPacket, 0, 8);
            Buffer.BlockCopy(clientGuidData, 0, clientSLNTPacket, 8, 16);

            clientDATAHeader = Encoding.UTF8.GetBytes("SHClDATA");

            Task.Factory.StartNew(() => ResetAsync());
        }

        #region Public methods
        public async Task<Guid> ConnectAsync(HostName hostName, string port)
        {
            if (nameResponse != null)
                throw new InvalidOperationException("The client is connecting to another server");

            nameResponse = new TaskCompletionSource<HClientResponseWithNameAndIP>();

            // Connect to the server
            DatagramSocket socket = new DatagramSocket();
            socket.Control.QualityOfService = SocketQualityOfService.LowLatency;
            socket.MessageReceived += client_MessageReceived;
            await socket.ConnectAsync(hostName, port);

            // Create new information
            HServerInformation serverInf = new HServerInformation();
            serverInf.Socket = socket;
            serverInf.Converter = new ShortStreamConverter(socket.OutputStream.AsStreamForWrite());

            // Ping the server
            serverInf.Converter.Write(clientCONNHeader);
            serverInf.Converter.Write(Encoding.UTF8.GetBytes(clientName));
            serverInf.Converter.Flush();

            // Wait for ping (or timeout)
            Task.WhenAny(nameResponse.Task, Task.Delay(CONNECT_TIMEOUT)).Wait();
            if (!nameResponse.Task.IsCompleted)
            {
                nameResponse.TrySetCanceled();
                socket.Dispose();

                Log(serverInf.Guid, "Connection timeout");

                nameResponse = null;

                return default(Guid);
            }

            // Add new entry
            serverInf.Guid = nameResponse.Task.Result.GUID;
            serverInf.Name = nameResponse.Task.Result.Name;
            serverInf.IP = nameResponse.Task.Result.IP;
            Log(serverInf.Guid, "Profile added");
            serverList.Add(serverInf);

            nameResponse = null;

            // Fire event
            if (Connected != null)
                Connected(this, serverInf.Guid);

            return serverInf.Guid;
        }
        public void Close()
        {
            foreach (var server in serverList)
            {
                if (server.Socket != null)
                    server.Socket.Dispose();
            }
        }
        public void Close(Guid guid)
        {
            HServerInformation serverInf = serverList.FirstOrDefault((inf) => inf.Guid == guid);
            if (serverInf.Guid != default(Guid))
            {
                serverInf.Converter.Write(clientCLSEPacket);
                serverInf.Converter.Flush();
            }
        }
        public async Task ResetAsync()
        {
            if (serverList != null)
            {
                CloseAll();
                await Task.Delay(50);
                Close();
            }

            serverList = new List<HServerInformation>();
        }
        public HServerInformation GetServer(Guid guid)
        {
            HServerInformation serverInf = serverList.FirstOrDefault((inf) => inf.Guid == guid);
            if (serverInf.Guid == default(Guid))
                throw new InvalidOperationException("Guid's not exist");
            return serverInf;
        }
        #endregion

        #region Private methods
        private void client_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            // Get InputStream
            BinaryReader reader;
            try
            {
                reader = new BinaryReader(args.GetDataStream().AsStreamForRead());
            }
            catch (Exception e)
            {
                if (SocketError.GetStatus(e.HResult) == SocketErrorStatus.ConnectionResetByPeer)
                {
                    if (!testConnectivityMode && !scanMode)
                        Task.Factory.StartNew(() => TestConnectivityAsync());
                    return;
                }
                else
                    throw;
            }
            byte[] header = reader.ReadBytes(8);
            string headerstr = Encoding.UTF8.GetString(header, 0, header.Length);

            // PING request
            if (headerstr == "SHSvPING")
                Header_PING(reader, sender.Information.RemoteAddress.CanonicalName);
            // NAME request
            if (headerstr == "SHSvNAME")
                Header_NAME(reader, sender.Information.RemoteAddress.CanonicalName);
            // CLOSE request
            if (headerstr == "SHSvCLSE")
                Header_CLSE(reader);
        }
        private void Header_PING(BinaryReader reader, string ip)
        {
            // Add GUID
            Guid guid = new Guid(reader.ReadBytes(16));
            string name = reader.ReadString();

            // Test Connectivity if needed
            if (testConnectivityMode)
                responseServers.Add(guid);
            else if (PingReceived != null)
                PingReceived(this, new HClientResponseWithNameAndIP() {
                    GUID = guid,
                    Name = name,
                    IP = ip
                });

            Log(guid, "PING from \"" + name + "\" received");
        }
        private void Header_NAME(BinaryReader reader, string ip)
        {
            Guid guid = new Guid(reader.ReadBytes(16));
            string name = reader.ReadString();

            HServerInformation serverInf = serverList.FirstOrDefault((inf) => inf.Guid == guid);
            if (serverInf != null)
                serverInf.Name = name;
            else if (nameResponse != null)
            {
                HClientResponseWithNameAndIP response = new HClientResponseWithNameAndIP()
                {
                    GUID = guid,
                    Name = name,
                    IP = ip
                };
                nameResponse.SetResult(response);
            }

            Log(guid, "NAME received: \"" + name + "\"");
        }
        private void Header_CLSE(BinaryReader reader)
        {
            Guid guid = new Guid(reader.ReadBytes(16));

            HServerInformation serverInf = serverList.FirstOrDefault((inf) => inf.Guid == guid);
            if (serverInf != null)
            {
                serverList.Remove(serverInf);
                if (Disconnected != null)
                    Disconnected(this, serverInf.Guid);
            }

            Log(guid, "CLOSE received");
        }
        private void Log(Guid guid, string msg)
        {
            if (Logger != null)
                Logger.Write("Client to " + guid.ToString() + " : " + msg);
        }
        #endregion

        #region Functional methods
        public void Write(short[] buffer)
        {
            if (Enable)
                foreach (var socket in serverList)
                {
                    socket.Converter.Write(clientDATAHeader);
                    socket.Converter.Write(clientGuid.ToByteArray());
                    socket.Converter.Write(buffer);
                    socket.Converter.Flush();
                }
            else
            {
                foreach (var socket in serverList)
                {
                    socket.Converter.Write(clientSLNTPacket);
                    socket.Converter.Flush();
                }
            }
        }
        public void Write(byte[] buffer)
        {
            short[] sBuffer = new short[buffer.Length/2];
            Buffer.BlockCopy(buffer, 0, sBuffer, 0, buffer.Length);
        }
        public void PingAllServers()
        {
            foreach (var socket in serverList)
            {
                socket.Converter.Write(clientPINGPacket);
                socket.Converter.Flush();
                Log(socket.Guid, "PING sent");
            }
        }
        public void CloseAll()
        {
            foreach (var socket in serverList)
            {
                Close(socket.Guid);
                Log(socket.Guid, "CLOSE sent");
            }
        }

        private async Task TestConnectivityAsync()
        {
            if (testConnectivityMode)
                return;

            testConnectivityMode = true;
            responseServers.Clear();

            PingAllServers();
            await Task.Delay(50);

            var removeList = serverList.FindAll((server) => !responseServers.Contains(server.Guid));
            foreach (var server in removeList)
            {
                serverList.Remove(server);
                if (Disconnected != null)
                    Disconnected(this, server.Guid);
            }

            testConnectivityMode = false;
        }
        #endregion

        #region Scanning methods
        public async Task PingAllNetworkAsync(HostName hostName, string port)
        {
            if (hostName.IPInformation == null) return;

            while (testConnectivityMode)
                await Task.Delay(10);

            int free = 32 - hostName.IPInformation.PrefixLength.Value;
            List<ulong> ipList = GetPingQueue(hostName.CanonicalName, free);

            scanTotal = ipList.Count;
            scanned = 0;
            scanNow = 0;
            scanCancel = false;
            scanMode = true;

            if (ScanningStarted != null)
                ScanningStarted(this, null);

            await PingHostInQueueAsync(ipList, port);

            scanMode = false;
        }
        public void CancelPingAllNetwork()
        {
            scanCancel = true;
        }
        public List<ulong> GetPingQueue(string host, int free)
        {
            List<ulong> queue = new List<ulong>();
            queue.Add(HostNameProvider.IPDecompose(host));

            for (int i = 0; i < free; i++)
            {
                queue.AddRange(queue);
                ulong mask = (ulong)1 << i;
                for (int j = queue.Count / 2; j < queue.Count; j++)
                    queue[j] ^= mask;
            }

            return queue;
        }
        private async Task PingHostInQueueAsync(List<ulong> queue, string port)
        {
            if (scanCancel)
                return;

            List<Task> scanningTasks = new List<Task>(SCAN_LIMIT);

            foreach (var ip in queue)
            {
                while (scanNow >= SCAN_LIMIT)
                {
                    await Task.WhenAny(scanningTasks);
                    scanningTasks.RemoveAll((task) => task.IsCompleted);
                }

                if (scanCancel)
                    return;

                scanningTasks.Add(ScanTask(HostNameProvider.IPReconstruct(ip), port));
            }
        }
        private async Task ScanTask(string ip, string port)
        {
            scanNow++;
            try
            {
                using (DatagramSocket socket = new DatagramSocket())
                {
                    socket.MessageReceived += client_MessageReceived;
                    await socket.ConnectAsync(new HostName(ip), port);

                    Stream stream = socket.OutputStream.AsStreamForWrite();
                    stream.Write(clientPINGPacket, 0, clientPINGPacket.Length);
                    stream.Flush();
                    await Task.Delay(50);

                    scanned++;
                    if (ScanProgressUpdated != null)
                        ScanProgressUpdated(this, new ScanProgressEventArgs() { Scanned = scanned, LastIP = ip });
                    Logger.Write("Pinged to " + ip);
                }
            }
            catch (Exception) { }
            scanNow--;
        }
        #endregion

        #region Properties
        public Logger Logger
        {
            get;
            set;
        }
        public bool Enable { get; set; }
        public int Scanned
        {
            get
            {
                if (!scanMode) return -1;
                return scanned;
            }
        }
        public int ScanTotal
        {
            get
            {
                if (!scanMode) return -1;
                return scanTotal;
            }
        }
        #endregion
    }
}
