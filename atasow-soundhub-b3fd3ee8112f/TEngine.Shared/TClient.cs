using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using HNet;
using HNet.Converters;
using Windows.Networking;

using MAudio;
using MAudio.MWASAPI;
using Windows.Storage;
using Windows.Networking.Connectivity;

namespace TEngine
{
    public sealed class TClient
    {
        #region Variables
        HClient client;
        MAudioCapturer capturer;

        const string SHUB_PORT = "52708";
        const int COMMON_FREQ = 8000;
        bool scanMode = false;
        Task scanTask;
        #endregion

        #region Events
        public event EventHandler<TClientProfile> NewProfile;
        internal event EventHandler<Guid> DeleteProfile;
        public event EventHandler ScanningStarted;
        public event EventHandler ScanningCanceling;
        public event EventHandler ScanningDone;
        public event EventHandler<ScanProgressEventArgs> ScanProgressUpdated;
        internal event EventHandler<TClientProfile> NewAvailableProfile;
        public event EventHandler<string> ConnectionTimeout;
        #endregion

        public TClient(Logger logger = null)
        {
            // Get data from Settings
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            Object guid = localSettings.Values["Guid"];
            if (guid == null)
                localSettings.Values["Guid"] = guid = Guid.NewGuid();

            Object name = localSettings.Values["Name"];
            if (name == null)
                localSettings.Values["Name"] = name = "SoundHub Client";

            // Initialize the network library
            client = new HClient((Guid)guid, (string)name);
            client.Logger = logger;
            client.Disconnected += client_Disconnected;
            client.PingReceived += client_PingReceived;
            client.ScanProgressUpdated += client_ScanProgressUpdated;
            client.ScanningStarted += (o, e) =>
            {
                if (ScanningStarted != null)
                    ScanningStarted(this, null);
            };

            var ignore = ResetAsync();
        }
        public bool Enable
        {
            get { return client.Enable; }
            set { client.Enable = value; }
        }

        #region HClient events
        void client_Disconnected(object sender, Guid e)
        {
            if (DeleteProfile != null)
                DeleteProfile(this, e);
        }
        void client_PingReceived(object sender, HClientResponseWithNameAndIP e)
        {
            if (scanMode && NewAvailableProfile != null)
                NewAvailableProfile(this, new TClientProfile(e.GUID, e.Name, e.IP));
        }
        void client_ScanProgressUpdated(object sender, ScanProgressEventArgs e)
        {
            if (ScanProgressUpdated != null)
                ScanProgressUpdated(this, e);
        }
        #endregion

        public async Task ResetAsync()
        {
            await client.ResetAsync();
        }
        public void Close(TClientProfile profile)
        {
            if (profile.State == ConnectionState.Connected)
                client.Close(profile.Guid);
        }

        public async Task ConnectAsync(string ip)
        {
            await CancelScanningAsync();

            Guid guid = await client.ConnectAsync(new HostName(ip), SHUB_PORT);

            // Connection timeout
            if (guid == default(Guid))
            {
                if (ConnectionTimeout != null)
                    ConnectionTimeout(this, ip);
                return;
            }

            if (NewProfile != null)
            {
                HServerInformation serverInf = client.GetServer(guid);
                TClientProfile profile = new TClientProfile(serverInf, serverInf.IP);
                profile.State = ConnectionState.Connected;
                NewProfile(this, profile);
            }
        }
        public async Task ConnectAsync(TClientProfile profile)
        {
            if (profile.State == ConnectionState.Connected)
                return;

            await ConnectAsync(profile.LastIPAddress);
        }
        public async Task ConnectViaLoopbackAsync()
        {
            await ConnectAsync(HostNameProvider.GetLoopbackHost().CanonicalName);
        }

        public async Task ActivateAsync()
        {
            capturer = new MAudioCapturer();
            await capturer.InitializeAsync(new MAudioFormat(MAudioEncodingType.PCM16bits, 1, 48000)).
                ContinueWith((preTask) => ActivationPostTask());
        }
        public async Task ActivateAsyncPhone()
        {
            capturer = new MAudioCapturer();
            await capturer.InitializeAsync().
                ContinueWith((preTask) => ActivationPostTask());
        }
        public void ActivationPostTask()
        {
            MAudioFormat format = capturer.Format;
            int ratio = format.NumberOfChannels * format.SampleRate / COMMON_FREQ;

            switch (format.EncodingType)
            {
                case MAudioEncodingType.PCM16bits:
                    capturer.Stream.OutputStream =
                        new ConverterStream(new ShortSimplifier(client, ratio, 1));
                    break;
                case MAudioEncodingType.Float:
                    capturer.Stream.OutputStream =
                        new ConverterStream(new FloatSimplifier(new FloatToPCMConverter(client), ratio, 1));
                    break;
            }
            capturer.Start();
        }
        public async Task DeactivateAsync()
        {
            await client.ResetAsync();
            client.Close();
        }

        public async Task ScanAsync(HostName host)
        {
            if (scanMode)
            {
                await scanTask;
                return;
            }

            scanMode = true;

            scanTask = client.PingAllNetworkAsync(host, SHUB_PORT);
            await scanTask;
            
            scanMode = false;

            if (ScanningDone != null)
                ScanningDone(this, null);
        }
        public async Task ScanViaDefaultAsync()
        {
            await ScanAsync((await HostNameProvider.GetConnectedHostsAsync())[0]);
        }
        public async Task<List<HostName>> GetListOfHostAsync()
        {
            return await HostNameProvider.GetConnectedHostsAsync();
        }

        public async Task CancelScanningAsync()
        {
            if (scanMode)
            {
                if (ScanningCanceling != null)
                    ScanningCanceling(this, null);
                client.CancelPingAllNetwork();
                await scanTask;
            }
        }
        public int Scanned
        {
            get
            {
                return client.Scanned;
            }
        }
        public int ScanTotal
        {
            get
            {
                return client.ScanTotal;
            }
        }
    }
}
