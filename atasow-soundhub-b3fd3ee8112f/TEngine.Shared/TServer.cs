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
using Windows.UI.Core;
using TEngine.Output;

namespace TEngine
{
    public sealed class TServer
    {
        #region Variables
        HServer server;
        MAudioRenderer renderer;
        Mixer mixer;
        BranchingConverter dataOutput;
        WaveOutputConverter waveOutput;

        const string SHUB_PORT = "52708";
        const int COMMON_FREQ = 8000;

        CoreDispatcher dispatcher;
        #endregion

        #region Events
        public event EventHandler NetworkNotAvailable;
        public event EventHandler<TServerProfile> NewProfile;
        public event EventHandler<Guid> DeleteProfile;
        #endregion

        public TServer(Logger logger = null)
        {
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            // Get data from Settings
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            Object guid = localSettings.Values["Guid"];
            if (guid == null)
                localSettings.Values["Guid"] = guid = Guid.NewGuid();

            Object name = localSettings.Values["Name"];
            if (name == null)
                localSettings.Values["Name"] = name = "SoundHub Server";

            server = new HServer((Guid)guid, (string)name);
            server.ProfileRequest += server_ProfileRequest;
            server.ProfileRemove += server_ProfileRemove;
            server.Logger = logger;

            IsRecording = false;
        }

        public async Task ResetAsync()
        {
            await server.ResetAsync();
        }
        public void Close(Guid guid)
        {
            server.Close(guid);
        }

        async void server_ProfileRequest(object sender, HClientInformation client)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                TServerProfile profile = new TServerProfile(client, mixer);

                if (NewProfile != null)
                    NewProfile(this, profile);
            });
        }
        void server_ProfileRemove(object sender, Guid e)
        {
            if (DeleteProfile != null)
                DeleteProfile(this, e);
        }

        public async Task ListenAsync(string ip)
        {
            await server.BindPortAsync(new HostName(ip), SHUB_PORT);
        }
        public async Task ListenAllConnectedAsync()
        {
            var hostnames = await HostNameProvider.GetConnectedHostsAsync();
            if (hostnames.Count == 0)
            {
                if (NetworkNotAvailable != null)
                    NetworkNotAvailable(this, null);
                return;
            }

            foreach (var host in hostnames)
                await server.BindPortAsync(host, SHUB_PORT);
            await server.BindPortAsync(HostNameProvider.GetLoopbackHost(), SHUB_PORT);
        }
        public async Task ListenViaLoopbackAsync()
        {
            await server.BindPortAsync(HostNameProvider.GetLoopbackHost(), SHUB_PORT);
        }

        public async Task ActivateAsync()
        {
            renderer = new MAudioRenderer(new MAudioRendererProperties(false, true, false));
            await renderer.InitializeAsync().ContinueWith((preTask) =>
            {
                MAudioFormat format = renderer.Format;
                int ratio = format.SampleRate / COMMON_FREQ;

                dataOutput = new BranchingConverter(
                    new FirstOrderFiller(
                        new Duplicator(
                            new FloatStreamConverter(renderer.Stream)), ratio));
                mixer = new Mixer(dataOutput, new TimeSpan(100000), COMMON_FREQ / 100);
            });
        }
        public async Task DeactivateAsync()
        {
            await server.ResetAsync();
            server.Close();
        }

        public async Task StartRecording()
        {
            if (!IsRecording)
            {
                IsRecording = true;
                waveOutput = new WaveOutputConverter();
                await waveOutput.StartRecording();
                dataOutput.Branches.Add(waveOutput);
            }
        }
        public async Task<string> StopRecording()
        {
            if (IsRecording)
            {
                dataOutput.Branches.Clear();
                await waveOutput.StopRecording();
                IsRecording = false;
                return waveOutput.FileName;
            }
            return "";
        }
        public bool IsRecording { get; private set; }
        public double RecordingTime
        {
            get {
                if (!IsRecording)
                    return 0;
                else
                    return waveOutput.Time;
            }
        }
    }
}
