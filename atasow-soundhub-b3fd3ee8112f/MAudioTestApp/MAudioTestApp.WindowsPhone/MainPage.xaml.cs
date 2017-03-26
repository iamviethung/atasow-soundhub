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
using Windows.UI.Xaml.Shapes;
using Windows.UI.Core;
using System.Threading.Tasks;

using Windows.Networking;
using Windows.Networking.Sockets;
using HNet;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MAudioTestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const int PORT = 2709;

        MAudioCapturer capturer;
        MAudioFormat format;

        TaskCompletionSource<StreamSocket> socket;

        Polyline line;
        Point start;
        double length, height;

        CoreDispatcher dispatcher;
        
        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            socket = new TaskCompletionSource<StreamSocket>(null);

            capturer = new MAudioCapturer();
            capturer.InitializeAsync().ContinueWith((preTask) =>
            {
                format = capturer.Format;
            });
        }

        #region Navigation
        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
        }
        void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            Application.Current.Exit();
        }
        #endregion

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            TextBox tIP = (TextBox)FindName("tIP");
            StreamSocket client = new StreamSocket();
            await client.ConnectAsync(new HostName(tIP.Text), PORT.ToString());
            socket.SetResult(client);
            ((Button)sender).IsEnabled = false;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Grid rootPanel = (Grid)this.FindName("rootPanel");
            line = (Polyline)this.FindName("plOscilloscope");

            Size size = rootPanel.RenderSize;
            length = size.Width - 20;
            height = Math.Min(length / 2, size.Height - 20);
            start = new Point(10, size.Height / 2);

            await socket.Task;
            RatingStream rateStream = new RatingStream(socket.Task.Result.OutputStream.AsStreamForWrite());
            rateStream.WriteRateCalculated += rateStream_WriteRateCalculated;
            capturer.Stream.OutputStream = rateStream;
            capturer.Start();
        }

        void rateStream_WriteRateCalculated(object sender, RateCalculatedEventArgs e)
        {
            dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                TextBlock tRate = (TextBlock)FindName("tRate");
                tRate.Text = (e.Rate / format.FrameSize).ToString("F1");
            });
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (socket.Task.Result != null)
                socket.Task.Result.Dispose();
        }
    }
}
