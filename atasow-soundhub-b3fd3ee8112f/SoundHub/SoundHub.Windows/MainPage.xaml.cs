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

using TEngine;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.Storage;
using Windows.UI.Popups;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Networking;

namespace SoundHub
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        #region Variables
        CoreDispatcher dispatcher;

        TServer server;
        TServerProfileManager serverManager;

        ProfileFolder openingFolder = null;
        TServerProfile openingProfile = null;
        AppState state = AppState.None;

        ObservableCollection<AdapterProfile> adapterProfiles;
        #endregion

        public MainPage()
        {
            this.InitializeComponent();
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            var ignore = InitializeEngine();
            InitializeUI();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await server.ActivateAsync();
            await server.ListenAllConnectedAsync();
            AdapterProfiles = new ObservableCollection<AdapterProfile>(
                await AdapterProfile.GetListOfAdaptersAsync());
            AdapterProfiles.Add(new AdapterProfile(new HostName("127.0.0.1"), "Loopback Connection"));
        }
        private async void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            await server.DeactivateAsync();
        }

        #region Folder panel code
        private void l_folders_ItemClick(object sender, ItemClickEventArgs e)
        {
            int index = l_folders.Items.IndexOf(e.ClickedItem);
            OpeningFolder = serverManager.Folders[index];
        }
        public ProfileFolder OpeningFolder
        {
            get { return openingFolder; }
            private set
            {
                if (openingFolder != null) {
                    openingFolder.CollectionChanged -= openingFolder_CollectionChanged;
                    openingFolder.Selected = false;
                }

                openingFolder = value;

                if (openingFolder != null)
                {
                    openingFolder.CollectionChanged += openingFolder_CollectionChanged;
                    openingFolder.Selected = true;
                }

                if (!openingFolder.Contains(openingProfile))
                    OpeningProfile = null;

                NotifyPropertyChanged("OpeningFolder");
            }
        }
        void openingFolder_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
                if (OpeningProfile == e.OldItems[0])
                    OpeningProfile = null;
        }
        #endregion

        #region Profile panel code
        private void l_profiles_ItemClick(object sender, ItemClickEventArgs e)
        {
            OpeningProfile = (TServerProfile)e.ClickedItem;
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
                TServerProfile selected = (TServerProfile)l_profiles.SelectedItem;
                if (selected.State == ConnectionState.Connected ||
                    selected.State == ConnectionState.LackOfResponse)
                    b_disconnect.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }
        private void b_disconnect_Click(object sender, RoutedEventArgs e)
        {
            TServerProfile selected = (TServerProfile)l_profiles.SelectedItem;

            if (selected.State == ConnectionState.Connected ||
                selected.State == ConnectionState.LackOfResponse)
                server.Close(selected.Guid);
            appbar.IsOpen = false;
        }
        #endregion

        #region Detail panel code
        public TServerProfile OpeningProfile
        {
            get { return openingProfile; }
            private set
            {
                if (openingProfile != null)
                    openingProfile.DetachOutput();

                openingProfile = value;

                if (openingProfile != null)
                    openingProfile.AttachOutput(p_osc);

                NotifyPropertyChanged("OpeningProfile");
            }
        }
        #endregion

        #region Initialization
        void InitializeUI()
        {
            l_folders.DataContext = serverManager.Folders;
            OpeningFolder = serverManager.ConnectedFolder;
            OpeningProfile = null;
        }
        async Task InitializeEngine()
        {
            server = new TServer();
            serverManager = new TServerProfileManager(server, dispatcher);

            await server.ResetAsync();

            server.NetworkNotAvailable += (o, e) =>
            {
                var ignore = dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
                {
                    MessageDialog dialog = new MessageDialog(
                        "Network is not available. Please connect to a network and then restart the appplication.");
                    dialog.Commands.Add(new UICommand("Exit application", new UICommandInvokedHandler((c) => {
                        Application.Current.Exit();
                    })));
                    await dialog.ShowAsync();
                });
            };
        }
        #endregion

        #region Recording
        private void b_recording_Click(object sender, RoutedEventArgs e)
        {
            if (!server.IsRecording)
            {
                Task.Run(() => StartRecording());
                b_recording.Icon = new SymbolIcon(Symbol.Stop);
                b_recording.Label = "Stop recording";
            }
            else
            {
                Task.Run(() => StopRecording());
                b_recording.Icon = new SymbolIcon(Symbol.Play);
                b_recording.Label = "Start recording";
            }
        }
        private async Task StartRecording()
        {
            await server.StartRecording();
            var ignore = dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
            {
                State = AppState.Recording;
            });
        }
        private async Task StopRecording()
        {
            string filename = await server.StopRecording();
            var ignore = dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
            {
                State = AppState.None;
                MessageDialog dialog = new MessageDialog(
                    "The record " + filename + " is saved in the Music library.");
                dialog.Commands.Add(new UICommand("OK"));
                await dialog.ShowAsync();
            });
        }
        #endregion

        #region UI events
        public AppState State
        {
            get { return state; }
            private set { state = value; NotifyPropertyChanged("State"); }
        }
        private void appBar_Closed(object sender, object e)
        {
            l_profiles.SelectedItem = null;

            b_disconnect.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        public ObservableCollection<AdapterProfile> AdapterProfiles
        {
            get
            {
                return adapterProfiles;
            }
            private set
            {
                adapterProfiles = value;
                NotifyPropertyChanged("AdapterProfiles");
            }
        }
        private void l_adapter_ItemClick(object sender, ItemClickEventArgs e)
        {
            AdapterProfile profile = (AdapterProfile)e.ClickedItem;

            DataPackage package = new DataPackage();
            package.RequestedOperation = DataPackageOperation.Copy;
            package.SetText(profile.HostName);
            Clipboard.SetContent(package);
        }
        #endregion

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
