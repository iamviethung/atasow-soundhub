using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using TEngine;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.Storage;

using Windows.UI.Popups;
using Windows.Networking;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.Networking.Connectivity;
using System.Collections.ObjectModel;
using HNet;
using Windows.ApplicationModel.DataTransfer;

namespace SoundHub
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        #region Variables
        CoreDispatcher dispatcher;

        TClient client;
        TClientProfileManager clientManager;

        ProfileFolder openingFolder = null;
        ObservableCollection<AdapterProfile> adapterProfiles = null;
        AppState state = AppState.None;

        bool manually = false;
        #endregion

        public MainPage()
        {
            this.InitializeComponent();
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            InitializeEngine();
            InitializeUI();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await client.ActivateAsync();
        }
        private async void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            await client.DeactivateAsync();
        }

        #region Folder panel code
        private void l_folders_ItemClick(object sender, ItemClickEventArgs e)
        {
            int index = l_folders.Items.IndexOf(e.ClickedItem);
            OpeningFolder = clientManager.Folders[index];
        }
        public ProfileFolder OpeningFolder
        {
            get { return openingFolder; }
            private set
            {
                if (openingFolder != null)
                    openingFolder.Selected = false;

                openingFolder = value;
                
                if (openingFolder != null)
                    openingFolder.Selected = true;

                NotifyPropertyChanged("OpeningFolder");
                CheckForAdvise();
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
        #endregion

        #region Profile panel code
        private void l_profiles_ItemClick(object sender, ItemClickEventArgs e)
        {
            TClientProfile profile = (TClientProfile)e.ClickedItem;
            if (profile.State == ConnectionState.Disconnected)
                Task.Run(() => client.ConnectAsync(profile.LastIPAddress));
        }
        private void l_profiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                CheckAppBarButtonVisibility();
                appbar.IsOpen = true;
            }
        }
        void CheckAppBarButtonVisibility()
        {
            if (l_profiles.SelectedItem != null)
            {
                if (((TClientProfile)l_profiles.SelectedItem).State == ConnectionState.Connected)
                    b_disconnect.Visibility = Windows.UI.Xaml.Visibility.Visible;
                else
                    b_connect.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }
        private async void b_connect_Click(object sender, RoutedEventArgs e)
        {
            TClientProfile selected = (TClientProfile)l_profiles.SelectedItem;
            b_connect.IsEnabled = false;

            await client.CancelScanningAsync();
            if (selected.State == ConnectionState.Disconnected)
                await client.ConnectAsync(selected.LastIPAddress);
            appbar.IsOpen = false;
            b_connect.IsEnabled = true;
        }
        private void b_disconnect_Click(object sender, RoutedEventArgs e)
        {
            TClientProfile selected = (TClientProfile)l_profiles.SelectedItem;

            if (selected.State == ConnectionState.Connected ||
                selected.State == ConnectionState.LackOfResponse)
                client.Close(selected);
            appbar.IsOpen = false;
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
                    p_beforescan.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    p_progress.Maximum = client.ScanTotal;
                    p_progress.Value = 0;
                    t_progress.Text = 0 + "/" + client.ScanTotal;
                    p_scanning.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    t_scanadvise.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    State = AppState.Scanning;
                });
            };
            client.ScanningCanceling += (o, e) =>
            {
                var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    State = AppState.ScanningCanceling;
                });
            };
            client.ScanningDone += (o, e) =>
            {
                var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    p_beforescan.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    p_scanning.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    appbar_Opened(this, null);
                    CheckForAdvise();
                    State = AppState.None;
                });
            };
            client.ScanProgressUpdated += (o, e) =>
            {
                var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    p_progress.Value = e.Scanned;
                    t_progress.Text = e.Scanned + "/" + client.ScanTotal;
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

        #region Appbar events
        public ObservableCollection<AdapterProfile> AdapterProfiles
        {
            get
            {
                return adapterProfiles;
            }
            private set {
                adapterProfiles = value;
                NotifyPropertyChanged("AdapterProfiles");
            }
        }

        private void b_scan_Click(object sender, RoutedEventArgs e)
        {
            HostName host = ((AdapterProfile)c_adapter.SelectedItem).Host;
            Task.Run(() => client.ScanAsync(host));
        }
        private void b_cancel_Click(object sender, RoutedEventArgs e)
        {
            var ignore = client.CancelScanningAsync();
        }
        private async void Flyout_Opened(object sender, object e)
        {
            t_ipinput.Text = "";
            b_connectflyout.IsEnabled = false;
            DataPackageView dataPackageView = Clipboard.GetContent();
            b_pasteflyout.IsEnabled = dataPackageView.Contains(StandardDataFormats.Text) &&
                HostNameProvider.IsIPv4(await dataPackageView.GetTextAsync());
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
        private async void b_pasteflyout_Click(object sender, RoutedEventArgs e)
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
                t_ipinput.Text = await dataPackageView.GetTextAsync();
        }
        private void appBar_Closed(object sender, object e)
        {
            l_profiles.SelectedItem = null;

            b_disconnect.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            b_connect.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }
        private async void appbar_Opened(object sender, object e)
        {
            if (p_beforescan.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                var adapter = await AdapterProfile.GetListOfAdaptersAsync();

                AdapterProfiles = new ObservableCollection<AdapterProfile>(adapter);
                AdapterProfiles.Add(new AdapterProfile(new HostName("127.0.0.1"), "Loopback Connection"));

                c_adapter.SelectedIndex = 0;
            }
        }
        #endregion

        #region Record button
        private void RecordButton_Enable(object sender, EventArgs e)
        {
            client.Enable = true;
        }
        private void RecordButton_Disable(object sender, EventArgs e)
        {
            client.Enable = false;
        }
        #endregion

        public AppState State
        {
            get { return state; }
            private set { state = value; NotifyPropertyChanged("State"); }
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
    }
}
