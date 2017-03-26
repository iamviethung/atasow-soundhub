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

using MAudio;
using MAudio.MWASAPI;
using System.Threading.Tasks;
using Windows.UI.Core;

using HNet;
using Windows.Networking;
using Windows.Networking.Sockets;
using HNet.Streams;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MAudioTestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const int PORT = 2709;
        
        IMAudioRenderer renderer;
        MAudioFormat format;

        HWirelessServer server;

        CoreDispatcher dispatcher;
        TaskCompletionSource<StreamSocket> socket;

        public MainPage()
        {
            this.InitializeComponent();
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            socket = new TaskCompletionSource<StreamSocket>(null);

            server = new HWirelessServer();
            server.ConnectionReceived += server_ConnectionReceived;
            server.ListenAsync(PORT).ContinueWith(async (preTask) =>
            {
                if (preTask.Result != null)
                    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        tIP.Text += preTask.Result.CanonicalName;
                    });
            });

            renderer = new MAudioRenderer(new MAudioRendererProperties(false, true, false));
            var soundTask = renderer.InitializeAsync();

            Task.WhenAll(new Task[] { soundTask, socket.Task }).
                ContinueWith((preTask) =>
                {
                    format = renderer.Format;

                    DuplicateInputStream dupStream = new DuplicateInputStream(
                        socket.Task.Result.InputStream.AsStreamForRead(), format.FrameSize / 2);
                    RateStream rateStream = new RateStream(dupStream);

                    //RateStream rateStream = new RateStream(socket.Task.Result.InputStream.AsStreamForRead());
                    rateStream.ReadRateCalculated += rateStream_ReadRateCalculated;
                    renderer.Stream.InputStream = rateStream;
                    renderer.Start();
                });
        }

        void rateStream_ReadRateCalculated(object sender, RateStreamCalculatedEventArgs e)
        {
            dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                tRate.Text = "Frame Rate: " + (e.Rate / format.FrameSize).ToString("F1");
            });
        }

        async void server_ConnectionReceived(object sender, HServerConnectionReceivedEventArgs e)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                tIP.Text += " - Connected";
            });
            socket.SetResult(e.Socket);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (server != null)
                server.Close();
        }
    }
}
