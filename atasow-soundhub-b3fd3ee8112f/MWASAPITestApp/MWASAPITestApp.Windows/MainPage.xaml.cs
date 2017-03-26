using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using MWASAPI;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.UI.Core;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MWASAPITestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MAudioClient client;
        MAudioCaptureClient captureclient;
        MWaveFormat waveformat;
        int frameSize;

        CoreDispatcher dispatcher;

        int frequency;
        double phase;

        DateTime begin;

        public MainPage()
        {
            this.InitializeComponent();
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            frequency = (int)sFrequency.Value;
            client = new MAudioClient();
            client.Activated += client_Activated;
            client.BufferReady += client_BufferReady;
            client.ActivateAsync(MAudioDeviceType.Capture);
        }

        void client_Activated(object sender, object e)
        {
            waveformat = client.MixFormat;

            MAudioClientStreamFlags streamflags = new MAudioClientStreamFlags();
            streamflags.StreamEventCallback = true;
            client.Initialize(MAudioClientShareMode.Shared, streamflags, new TimeSpan(0), new TimeSpan(0), waveformat);

            captureclient = client.GetCaptureClient();

            client.Start();
            begin = DateTime.Now;
        }

        void client_BufferReady(object sender, Object e)
        {
            MAudioCaptureInformation captureinf = new MAudioCaptureInformation();
            byte[] data = captureclient.LoadBuffer(captureinf);
            
            TimeSpan time = DateTime.Now - begin;
            begin = DateTime.Now;
            dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                lUpdatePeriod.Text = "Update Period: " + time.TotalMilliseconds.ToString("F3") + " ms";
            }));
        }

        byte[] GetSineSamples(int nSamples, int nChannels, int sampleRate, int freq, double initPhase, out double outPhase)
        {
            float[] data = new float[nSamples * nChannels];
            double phase = initPhase, phaseInc = 2 * Math.PI * freq / sampleRate;

            for (int i = 0; i < nSamples; i++)
            {
                double sinValue = Math.Sin(phase);
                phase = (phase + phaseInc) % (2 * Math.PI);

                for (int j = 0; j < nChannels; j++)
                    data[i * nChannels + j] = (float)sinValue;
            }

            outPhase = phase;

            byte[] bData = new byte[data.Length * sizeof(float)];
            Buffer.BlockCopy(data, 0, bData, 0, bData.Length);
            return bData;
        }

        private void sFrequency_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (sFrequency != null)
                frequency = (int)sFrequency.Value;
        }
    }
}
