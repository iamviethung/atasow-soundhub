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

using System.Threading.Tasks;
using Windows.UI.Core;

using TEngine;
using HNet;
using HNet.Converters;
using AMath;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace HNetTestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Variables
        CoreDispatcher dispatcher;

        Logger logger;
        TServer server;
        TClient client;
        TServerProfileManager serverManager;
        TClientProfileManager clientManager;

        DateTime origin;
        #endregion

        public MainPage()
        {
            this.InitializeComponent();
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            logger = new Logger(ServerLogTime);

            server = new TServer(Guid.NewGuid(), "Server Name", logger);
            client = new TClient(Guid.NewGuid(), "Client Name", logger);

            serverManager = new TServerProfileManager(server);
            clientManager = new TClientProfileManager(client);

            server.NetworkNotAvailable += event_NetworkNotAvailable;
            client.ConnectionTimeout += client_ConnectionTimeout;

            serverManager.ConnectedFolder.New += ServerConnectedFolder_New;
            serverManager.ConnectedFolder.Delete += ServerConnectedFolder_Delete;

            clientManager.ConnectedFolder.New += ClientConnectedFolder_New;
            clientManager.ConnectedFolder.Delete += ClientConnectedFolder_Delete;

            clientManager.AvailableFolder.New += AvailableFolder_New;
            clientManager.AvailableFolder.Updated += AvailableFolder_Updated;

            SocketReset();
        }

        #region TEngine events
        void event_NetworkNotAvailable(object sender, EventArgs e)
        {
            if (sender == server)
                logger.Write("Server: Network is not available");
            else
                logger.Write("Client: Network is not available");
        }
        void ServerConnectedFolder_New(object sender, IProfile e)
        {
            logger.Write("Server folder: " + ((ProfileFolder)sender).Name);
            logger.Write("\t\tAdded: " + e.Name + " - " + e.Guid);
            foreach (TServerProfile profile in (ProfileFolder)sender)
                logger.Write("\t\t\t" + profile.Name + " - " + profile.Guid);
        }
        void ClientConnectedFolder_New(object sender, IProfile e)
        {
            logger.Write("Client folder: " + ((ProfileFolder)sender).Name);
            logger.Write("\t\tAdded: " + e.Name + " - " + e.Guid);
            foreach (TClientProfile profile in (ProfileFolder)sender)
                logger.Write("\t\t\t" + profile.Name + " - " + profile.Guid + " @ " + profile.LastIPAddress);
        }
        void ServerConnectedFolder_Delete(object sender, IProfile e)
        {
            logger.Write("Server folder: " + ((ProfileFolder)sender).Name);
            logger.Write("\t\tRemoved: " + e.Name + " - " + e.Guid);
            foreach (TServerProfile profile in (ProfileFolder)sender)
                logger.Write("\t\t\t" + profile.Name + " - " + profile.Guid);
        }
        void ClientConnectedFolder_Delete(object sender, IProfile e)
        {
            logger.Write("Client folder: " + ((ProfileFolder)sender).Name);
            logger.Write("\t\tRemoved: " + e.Name + " - " + e.Guid);
            foreach (TClientProfile profile in (ProfileFolder)sender)
                logger.Write("\t\t\t" + profile.Name + " - " + profile.Guid + " @ " + profile.LastIPAddress);
        }
        void AvailableFolder_Updated(object sender, EventArgs e)
        {
            logger.Write("Client folder updated: " + ((ProfileFolder)sender).Name);
            foreach (TClientProfile profile in (ProfileFolder)sender)
                logger.Write("\t\t\t" + profile.Name + " - " + profile.Guid + " @ " + profile.LastIPAddress);
        }
        void AvailableFolder_New(object sender, IProfile e)
        {
            ClientConnectedFolder_New(sender, e);
        }
        void client_ConnectionTimeout(object sender, string e)
        {
            logger.Write("Client: Connection to " + e + " timed out");
        }
        #endregion

        #region Logging and Status methods
        void Log(string log, TextBlock textBlock, ScrollViewer scrollViewer)
        {
            var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                textBlock.Text += log;
                scrollViewer.UpdateLayout();
                scrollViewer.ChangeView(0, scrollViewer.ScrollableHeight, 1);
            });
        }
        void ServerLog(string log)
        {
            Log(log, tServerLog, sServer);
        }
        void ServerLogLine(string log)
        {
            ServerLog(log + '\n');
        }
        void ServerLogTime(string log)
        {
            if (origin != null)
                ServerLogLine((DateTime.Now - origin).TotalSeconds.ToString("F3") + "> " + log);
            else ServerLogLine(log);
        }
        void ClientLog(string log)
        {
            Log(log, tClientLog, sClient);
        }
        void ClientLogLine(string log)
        {
            ClientLog(log + '\n');
        }
        void ClientLogTime(string log)
        {
            if (origin != null)
                ClientLogLine((DateTime.Now - origin).TotalSeconds.ToString("F3") + "> " + log);
            else ClientLogLine(log);
        }

        void Status(string status, TextBlock textBlock)
        {
            var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                textBlock.Text = "Status: " + status;
            });
        }
        void ServerStatus(string status)
        {
            Status(status, tServerStt);
        }
        void ClientStatus(string status)
        {
            Status(status, tClientStt);
        }
        #endregion

        #region UI Helper
        void SocketReset()
        {
            server.Reset();
            client.ResetAsync();
        }
        void UIChange(bool connected)
        {
            var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                bConnect.IsEnabled = tPort.IsEnabled = !connected;
                bServerClose.IsEnabled = bClientClose.IsEnabled = connected;
            });
            if (!connected)
                SocketReset();
        }
        #endregion

        #region UI events
        private async void bConnect_Click(object sender, RoutedEventArgs e)
        {
            origin = DateTime.Now;

            var host = await HostNameProvider.GetConnectedHostsAsync();
            if (host.Count == 0)
            {
                event_NetworkNotAvailable(client, null);
                return;
            }

            await server.ListenViaDefaultAsync();
            //await client.ScanAsync(host[0]);

            //ServerLogTime("Scan done");
            //await client.ConnectAsync((TClientProfile)clientManager.AvailableFolder[0]);

            UIChange(true);
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var ignore = server.ActivateAsync();
            ignore = client.ActivateAsync();
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            server.Deactivate();
            client.DeactivateAsync();
        }
        private void bSend_Click(object sender, RoutedEventArgs e) { }
        private void bClose_Click(object sender, RoutedEventArgs e)
        {
            if (sender == bServerClose)
                server.Reset();
            else
                client.ResetAsync();
            ((Button)sender).IsEnabled = false;

            if (!bServerClose.IsEnabled && !bClientClose.IsEnabled)
                UIChange(false);
        }
        #endregion

    }
}
