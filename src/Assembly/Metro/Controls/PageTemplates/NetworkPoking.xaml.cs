using Assembly.Helpers.Net.Sockets;
using System;
using System.Net;
using System.Windows;
using System.Windows.Input;
using Assembly.Metro.Dialogs;
using System.ComponentModel;
using System.Net.Sockets;

namespace Assembly.Metro.Controls.PageTemplates
{
    /// <summary>
    /// Interaction logic for NetworkPoking.xaml
    /// </summary>
    public partial class NetworkPoking
    {
        public NetworkPoking()
        {
            InitializeComponent();
            DataContext = App.AssemblyStorage.AssemblyNetworkPoke;

            InitNetPokeView();
        }

        private IPEndPoint CreateIPEndpoint(string ipAddress, string port)
        {
            try
            {
                if (ipAddress == null)
                {
                    return new IPEndPoint(IPAddress.Any, Int32.Parse(port));
                }
                else
                {
                    return new IPEndPoint(IPAddress.Parse(ipAddress), Int32.Parse(port));
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void InitNetPokeView()
        {
            svrAddressBox.Text = App.AssemblyStorage.AssemblyNetworkPoke.Address;
            svrPortBox.Text = App.AssemblyStorage.AssemblyNetworkPoke.Port;

            if (App.AssemblyStorage.AssemblyNetworkPoke.IsConnected)
            {
                App.AssemblyStorage.AssemblyNetworkPoke.PokeSessionManager.SessionDied += SessionManager_SessionDied;

                if (App.AssemblyStorage.AssemblyNetworkPoke.IsServer)
                {
                    SetServerVisibility();
                }
                else
                {
                    SetClientVisibility();
                }
            }
        }

        private void ServerSessionManager_SessionActivated(object sender, EventArgs e)
        {
            Dispatcher.InvokeAsync((Action)delegate
            {
                App.AssemblyStorage.AssemblyNetworkPoke.Address = svrAddressBox.Text;
                App.AssemblyStorage.AssemblyNetworkPoke.Port = svrPortBox.Text;
                SetServerVisibility();
                MetroMessageBox.Show("Group Poke Server Started", "Group poking server started successfully on port " + svrPortBox.Text);
            });
        }

        private void ClientSessionManager_SessionActivated(object sender, EventArgs e)
        {
            Dispatcher.InvokeAsync((Action)delegate
            {
                App.AssemblyStorage.AssemblyNetworkPoke.Address = svrAddressBox.Text;
                App.AssemblyStorage.AssemblyNetworkPoke.Port = svrPortBox.Text;
                SetClientVisibility();
                MetroMessageBox.Show("Group Poke Client Started", "Group poking successfully connected as a client to " + svrAddressBox.Text);
            });
        }

        private void SessionManager_SessionDied(object sender, SessionDiedEventArgs e)
        {
            Dispatcher.InvokeAsync((Action)delegate
            {
                SetUnconnectedVisibility();
            });
        }


        private void startServerBtn_Click(object sender, RoutedEventArgs e)
        {
            //first check input is there.
            var endpoint = CreateIPEndpoint(null, svrPortBox.Text);
            if (endpoint != null)
            {
                var serverSessionManager = new ServerPokeSessionManager(new PokeCommandDispatcher());

                serverSessionManager.SessionActivated += ServerSessionManager_SessionActivated;
                serverSessionManager.SessionActivated += App.AssemblyStorage.AssemblySettings.HomeWindow.ServerSessionManager_SessionActivated;
                serverSessionManager.SessionDied += SessionManager_SessionDied;
                serverSessionManager.SessionDied += App.AssemblyStorage.AssemblySettings.HomeWindow.SessionManager_SessionDied;
                serverSessionManager.ClientConnected += App.AssemblyStorage.AssemblySettings.HomeWindow.SessionManager_ClientConnected;
                serverSessionManager.ClientDisconnected += App.AssemblyStorage.AssemblySettings.HomeWindow.SessionManager_ClientDisconnected;

                App.AssemblyStorage.AssemblyNetworkPoke.PokeSessionManager = serverSessionManager;
                try
                {
                    serverSessionManager.StartServer(endpoint);
                }
                catch (SocketException)
                {
                    MetroMessageBox.Show("Group poke server start failure.", "Server could not bind port or address.  May already be in use by another process.");
                }
            }
            else
            {
                MetroMessageBox.Show("Port Invalid", "Server listen port was invalid.");
            }
        }

        private void startClientBtn_Click(object sender, RoutedEventArgs e)
        {
            var endpoint = CreateIPEndpoint(svrAddressBox.Text, svrPortBox.Text);
            if (endpoint != null)
            {
                var clientSesionManager = new ClientPokeSessionManager(new PokeCommandDispatcher());

                clientSesionManager.SessionActivated += ClientSessionManager_SessionActivated;
                clientSesionManager.SessionActivated += App.AssemblyStorage.AssemblySettings.HomeWindow.ClientSessionManager_SessionActivated;
                clientSesionManager.SessionDied += SessionManager_SessionDied;
                clientSesionManager.SessionDied += App.AssemblyStorage.AssemblySettings.HomeWindow.SessionManager_SessionDied;

                App.AssemblyStorage.AssemblyNetworkPoke.PokeSessionManager = clientSesionManager;
                try
                {
                    clientSesionManager.StartClient(endpoint);
                }
                catch (SocketException)
                {
                    MetroMessageBox.Show("Group Poke Client Failure", "Could not start group poke connection to " + svrAddressBox.Text);
                }
            }
            else
            {
                MetroMessageBox.Show("Server Details Invalid", "Remote port or address was not the correct format.");
            }
        }

        private void disconnectButton_Click(object sender, RoutedEventArgs e)
        {
            App.AssemblyStorage.AssemblyNetworkPoke.PokeSessionManager.Kill(null);
        }

        private void SetServerVisibility()
        {
            startClientBtn.Visibility = Visibility.Collapsed;
            startServerBtn.Visibility = Visibility.Collapsed;
            disconnectButton.Visibility = Visibility.Visible;
            connectedClientsGrid.Visibility = Visibility.Visible;
            clientList.Visibility = Visibility.Visible;
            svrAddressBox.IsReadOnly = true;
            svrPortBox.IsReadOnly = true;
            UpdateLayout();
        }

        private void SetClientVisibility()
        {
            startClientBtn.Visibility = Visibility.Collapsed;
            startServerBtn.Visibility = Visibility.Collapsed;
            disconnectButton.Visibility = Visibility.Visible;
            svrAddressBox.IsReadOnly = true;
            svrPortBox.IsReadOnly = true;
            UpdateLayout();
        }

        private void SetUnconnectedVisibility()
        {
            startClientBtn.Visibility = Visibility.Visible;
            startServerBtn.Visibility = Visibility.Visible;
            disconnectButton.Visibility = Visibility.Collapsed;
            srvAddressBar.Visibility = Visibility.Visible;
            connectedClientsGrid.Visibility = Visibility.Collapsed;
            clientList.Visibility = Visibility.Collapsed;
            svrAddressBox.IsReadOnly = false;
            svrPortBox.IsReadOnly = false;
            UpdateLayout();
        }

        private void PreviewPortInput(object sender, TextCompositionEventArgs e)
        {
            foreach (var ch in e.Text)
            {
                if (!Char.IsDigit(ch))
                {
                    e.Handled = true;
                    break;
                }
            }
        }
    }
}
