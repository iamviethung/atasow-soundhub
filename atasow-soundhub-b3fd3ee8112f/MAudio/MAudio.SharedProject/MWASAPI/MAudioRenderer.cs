using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using M = MWASAPI;
using Windows.Media;

namespace MAudio.MWASAPI
{
    #region Support classes
    public struct MAudioRendererProperties
    {
        public bool HardwareOffload;
        public bool BackgroundCapable;
        public bool RawMode;

        public MAudioRendererProperties(bool hardwareOffload, bool backgroundCapable, bool rawMode)
        {
            HardwareOffload = hardwareOffload;
            BackgroundCapable = backgroundCapable;
            RawMode = rawMode;
        }
    }
    #endregion

    public class MAudioRenderer : IMAudioRenderer
    {
        #region Variables
        // MWASAPI objects
        M.MAudioClient client;
        M.MAudioRenderClient renderer;
        M.MWaveFormat format;
        MAudioRendererProperties properties;

        // Other variables
        int frameCount = 0;
        int frameSize = 0;
        MRenderStream stream;
        SystemMediaTransportControls mediaControl;
        TaskCompletionSource<bool> initialized = new TaskCompletionSource<bool>();
        #endregion

        // Constructor
        public MAudioRenderer(MAudioRendererProperties properties)
        {
            this.properties = properties;

            mediaControl = SystemMediaTransportControls.GetForCurrentView();
            
            client = new M.MAudioClient();
            client.Activated += client_Activated;
            client.BufferReady += client_BufferReady;
        }

        #region Initialization
        public async Task InitializeAsync()
        {
            client.ActivateAsync(M.MAudioDeviceType.Render);

            await initialized.Task;
            format = client.MixFormat;

            ClientInitialize();
        }
        public async Task InitializeAsync(MAudioFormat format)
        {
            client.ActivateAsync(M.MAudioDeviceType.Render);

            await initialized.Task;
            this.format = client.IsFormatSupported(M.MAudioClientShareMode.Shared, format.MWASAPIFormat);

            ClientInitialize();
        }
        void ClientInitialize()
        {
            M.MAudioClientProperties mproperties;
            mproperties.IsOffload = properties.HardwareOffload;
            mproperties.Category = properties.BackgroundCapable ?
                M.MAudioStreamCategory.BackgroundCapableMedia : M.MAudioStreamCategory.ForegroundOnlyMedia;
            mproperties.Options = properties.RawMode ?
                M.MAudioClientStreamOption.Raw : M.MAudioClientStreamOption.None;
            client.SetClientProperties(mproperties);

            if (properties.BackgroundCapable)
                mediaControl.IsPlayEnabled = mediaControl.IsPauseEnabled = true;

            M.MAudioClientStreamFlags flags = new M.MAudioClientStreamFlags();
            flags.StreamEventCallback = true;
            client.Initialize(M.MAudioClientShareMode.Shared, flags, new TimeSpan(0), new TimeSpan(0), format);

            frameCount = client.BufferSize;
            frameSize = format.FrameSize;
            renderer = client.GetRenderClient();

            int bufferSize = frameCount * frameSize;

            stream = new MRenderStream(bufferSize / 2, bufferSize * 2, bufferSize);
            stream.BufferReady += stream_BufferReady;
        }
        #endregion

        #region Public functions
        public void Start() {
            if (client.State == M.MAudioClientState.Initialized ||
                client.State == M.MAudioClientState.Stopped ||
                client.State == M.MAudioClientState.JustReset)
            {
                client.Start();
                mediaControl.PlaybackStatus = MediaPlaybackStatus.Playing;
            }
        }
        public void Stop()
        {
            if (client.State == M.MAudioClientState.Started)
            {
                client.Stop();
                mediaControl.PlaybackStatus = MediaPlaybackStatus.Paused;
            }
        }
        public void Reset()
        {
            if (client.State == M.MAudioClientState.Initialized ||
                client.State == M.MAudioClientState.Stopped ||
                client.State == M.MAudioClientState.JustReset)
            {
                stream.Flush();
                client.Reset();
            }
        }
        #endregion

        #region Private functions
        // Client Activated
        void client_Activated(object sender, object e)
        {
            initialized.SetResult(true);
        }

        // Client Data request
        void client_BufferReady(object sender, object e)
        {
            int curPadding = client.CurrentPadding;
            int nFrameAvailable = frameCount - curPadding;
            if (nFrameAvailable > 0)
            {
                byte[] data = new byte[nFrameAvailable * frameSize];
                stream.Read(data);
                renderer.LoadBuffer(nFrameAvailable, data, 0, M.MAudioClientSilentFlag.None);
            }
        }

        void stream_BufferReady(object sender, EventArgs e)
        {
            byte[] data = new byte[frameCount * frameSize];
            stream.Read(data);

            renderer.LoadBuffer(frameCount, data, 0, M.MAudioClientSilentFlag.None);
            Start();
        }
        #endregion

        #region Properties
        public MRenderStream Stream
        {
            get { return stream; }
        }
        public MAudioFormat Format
        {
            get { return new MAudioFormat(format); }
        }
        public SystemMediaTransportControls SystemMediaTransportControls
        {
            get { return mediaControl; }
        }
        public bool BackgroundCapable
        {
            get { return properties.BackgroundCapable; }
        }
        #endregion
    }
}
