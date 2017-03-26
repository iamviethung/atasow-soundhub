using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using TEngine;
using Windows.UI.Core;
using System.Threading.Tasks;
using Windows.Storage;
using System.ComponentModel;
using Windows.UI.Popups;
using HNet;

namespace SoundHub
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        #region Variables
        CoreDispatcher dispatcher;

        TClient client;
        TClientProfileManager clientManager;

        ProfileFolder opening = null;
        ApplicationDataContainer localSettings;
        bool initialized = false;

        bool manually = false;
        #endregion

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
            App.Current.Suspending += async (o, e) => await client.DeactivateAsync();
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            localSettings = ApplicationData.Current.LocalSettings;
            
            InitializeEngine();
            InitializeUI();
        }

        #region Page events
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!initialized)
            {
                initialized = true;

                var hostnames = await HostNameProvider.GetConnectedHostsAsync();
                if (hostnames.Count == 0) {
                    MessageDialog dialog = new MessageDialog(
                        "Network is not available. Please connect to a network and then restart the appplication.");
                    dialog.Commands.Add(new UICommand("Exit application", new UICommandInvokedHandler((c) =>
                    {
                        Application.Current.Exit();
                    })));
                    await dialog.ShowAsync();
                }

                await client.ActivateAsyncPhone();
            }
        }
        #endregion

        #region Folder panel code
        private void l_folders_ItemClick(object sender, ItemClickEventArgs e)
        {
            int index = l_folders.Items.IndexOf(e.ClickedItem);
            OpeningFolder = clientManager.Folders[index];
        }
        public ProfileFolder OpeningFolder
        {
            get { return opening; }
            private set
            {
                opening = value;
                NotifyPropertyChanged("OpeningFolder");
                CheckForAdvise();
                p_root.SelectedIndex = 1;
            }
        }
        void CheckForAdvise()
        {
            var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (OpeningFolder == clientManager.AvailableFolder &&
                    clientManager.AvailableFolder.Count == 0)
                    t_scanadvise.Visibility = Windows.UI.Xaml.Visibility.Visible;
                else
                    t_scanadvise.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region Profile panel code
        private void l_profiles_ItemClick(object sender, ItemClickEventArgs e)
        {
            TClientProfile profile = (TClientProfile)e.ClickedItem;
            if (profile.State == ConnectionState.Disconnected)
                Task.Run(() => client.ConnectAsync(profile.LastIPAddress));
            else
                client.Close(profile);

        }
        #endregion

        #region Initialization
        void InitializeUI()
        {
            l_folders.DataContext = clientManager.Folders;
            OpeningFolder = clientManager.AvailableFolder;
        }
        void InitializeEngine()
        {
            client = new TClient();
            clientManager = new TClientProfileManager(client, dispatcher);

            InitializeEngineEvents();
        }
        void InitializeEngineEvents()
        {
            #region Scan events
            client.ScanningStarted += (o, e) =>
            {
                var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    p_progress.Maximum = client.ScanTotal;
                    p_progress.Value = 0;
                    t_progress.Text = 0 + "/" + client.ScanTotal;
                    g_scan.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    b_cancel.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    b_scan.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    CheckForAdvise();
                });
            };
            client.ScanningDone += (o, e) =>
            {
                var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    b_scan.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    g_scan.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    b_cancel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    CheckForAdvise();
                });
            };
            client.ScanProgressUpdated += (o, e) =>
            {
                var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    p_progress.Value = e.Scanned;
                    t_progress.Text = e.Scanned + "/" + client.ScanTotal;
                    t_lastip.Text = e.LastIP;
                    CheckForAdvise();
                });
            };
            #endregion
            #region Connection events
            client.ConnectionTimeout += (o, e) =>
            {
                var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
                {
                    MessageDialog dialog = new MessageDialog("Cannot connect to the server.");
                    dialog.Commands.Add(new UICommand("OK"));
                    await dialog.ShowAsync();
                });
                manually = false;
            };
            client.NewProfile += (o, e) =>
            {
                if (manually)
                {
                    var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                    {
                        OpeningFolder = clientManager.ConnectedFolder;
                    });
                }
                manually = false;
            };
            #endregion
        }
        #endregion

        #region UI events
        private void b_scan_Click(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() => client.ScanViaDefaultAsync());
        }
        private void b_cancel_Click(object sender, RoutedEventArgs e)
        {
            var ignore = client.CancelScanningAsync();
        }
        private void RecordButton_Enable(object sender, EventArgs e)
        {
            client.Enable = true;
        }
        private void RecordButton_Disable(object sender, EventArgs e)
        {
            client.Enable = false;
        }
        private void RecordButton_Locked(object sender, EventArgs e)
        {
            p_root.Items.Remove(p_folders);
            p_root.Items.Remove(p_profiles);
        }
        private void RecordButton_Unlocked(object sender, EventArgs e)
        {
            p_root.Items.Insert(0, p_folders);
            p_root.Items.Insert(1, p_profiles);
            p_folders.Visibility = p_profiles.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }
        private void p_root_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            b_record.UnpressButton();
            if (p_root.SelectedIndex == 2)
                appbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            else
                appbar.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }
        private void b_options_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(OptionsPage));
        }
        private void Flyout_Opened(object sender, object e)
        {
            t_ipinput.Text = "";
            b_connectflyout.IsEnabled = false;
        }
        private void t_ipinput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (HostNameProvider.IsIPv4(t_ipinput.Text))
                b_connectflyout.IsEnabled = true;
            else b_connectflyout.IsEnabled = false;
        }
        private void b_connectflyout_Click(object sender, RoutedEventArgs e)
        {
            string ip = t_ipinput.Text;
            manually = true;
            Task.Run(() => client.ConnectAsync(ip));
            b_manual.Flyout.Hide();
        }
        #endregion

    }
}
