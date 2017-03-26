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

using Windows.UI.Core;

using HNet;
using HNet.Converters;
using Windows.Networking;

using MAudio;
using MAudio.MWASAPI;
using TEngine;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace PhoneClientTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        CoreDispatcher dispatcher;

        HClient client;
        MAudioCapturer capturer;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            client = new HClient(Guid.NewGuid(), "Client");
        }

        #region UI events
        private async void bConnectClick(object sender, RoutedEventArgs e)
        {
            //await client.ConnectAsync(tIP.Text);
            Status("Connected to " + tIP.Text + " at " + tPort.Text);
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            capturer = new MAudioCapturer();
            var ignore = capturer.InitializeAsync().
                ContinueWith((preTask) =>
                {
                    MAudioFormat format = capturer.Format;
                    int ratio = format.NumberOfChannels * format.SampleRate / 16000;

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
                });
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            //client.DeactivateAsync();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += (o, ea) => Application.Current.Exit();
        }
        #endregion

        #region UI helper
        void Status(string msg)
        {
            var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                tStatus.Text = "Status: " + msg;
            });
        }
        #endregion
    }
}
