using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using M = MWASAPI;

namespace MAudio.MWASAPI
{
    public class MAudioCapturer : IMAudioCapturer
    {
        #region Variables
        // MWASAPI objects
        M.MAudioClient client;
        M.MAudioCaptureClient capturer;
        M.MWaveFormat format;

        // Other variables
        int frameCount = 0;
        int frameSize = 0;
        MCaptureStream stream;
        TaskCompletionSource<bool> initialized = new TaskCompletionSource<bool>();
        #endregion

        // Constructor
        public MAudioCapturer()
        {            
            client = new M.MAudioClient();
            client.Activated += client_Activated;
            client.BufferReady += client_BufferReady;
        }

        #region Initialization
        public async Task InitializeAsync()
        {
            client.ActivateAsync(M.MAudioDeviceType.Capture);

            await initialized.Task;
            format = client.MixFormat;

            await Task.Run(() => ClientInitialize());
        }
        public async Task InitializeAsync(MAudioFormat format)
        {
            client.ActivateAsync(M.MAudioDeviceType.Capture);

            await initialized.Task;
            this.format = client.IsFormatSupported(M.MAudioClientShareMode.Shared, format.MWASAPIFormat);

            await Task.Run(() => ClientInitialize());
        }
        void ClientInitialize()
        {
            M.MAudioClientStreamFlags flags = new M.MAudioClientStreamFlags();
            flags.StreamEventCallback = true;
            client.Initialize(M.MAudioClientShareMode.Shared, flags, new TimeSpan(0), new TimeSpan(0), format);

            frameCount = client.BufferSize;
            frameSize = format.FrameSize;
            capturer = client.GetCaptureClient();

            int bufferSize = frameCount * frameSize;

            stream = new MCaptureStream(bufferSize);
        }
        #endregion

        #region Public functions
        public void Start() {
            if (client.State == M.MAudioClientState.Initialized ||
                client.State == M.MAudioClientState.Stopped ||
                client.State == M.MAudioClientState.JustReset)
            {
                client.Start();
            }
        }
        public void Stop()
        {
            if (client.State == M.MAudioClientState.Started)
            {
                client.Stop();
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
            int available = capturer.NextPacketSize;
            if (available > 0)
            {
                byte[] data = capturer.LoadBuffer(new M.MAudioCaptureInformation());
                stream.Write(data);
            }
        }
        #endregion

        #region Properties
        public MCaptureStream Stream
        {
            get { return stream; }
        }
        public MAudioFormat Format
        {
            get { return new MAudioFormat(format); }
        }
        #endregion
    }
}
