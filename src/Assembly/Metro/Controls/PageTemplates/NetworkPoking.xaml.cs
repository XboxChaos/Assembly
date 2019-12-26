using Assembly.Helpers.Net.Sockets;
using System;
using System.Net;
using System.Windows;
using System.Windows.Input;
using Assembly.Metro.Dialogs;
using System.ComponentModel;

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
                App.AssemblyStorage.AssemblyNetworkPoke.PokeSessionManager.SessionDead += SessionManager_SessionDead;

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

        private void ServerSessionManager_SessionActive(object sender, EventArgs e)
        {
            Dispatcher.InvokeAsync((Action)delegate
            {
                App.AssemblyStorage.AssemblyNetworkPoke.Address = svrAddressBox.Text;
                App.AssemblyStorage.AssemblyNetworkPoke.Port = svrPortBox.Text;
                SetServerVisibility();
                MetroMessageBox.Show("Network Poke Server Started", "Network poking server started successfully on port " + svrPortBox.Text);
            });
        }

        private void ClientSessionManager_SessionActive(object sender, EventArgs e)
        {
            Dispatcher.InvokeAsync((Action)delegate
            {
                App.AssemblyStorage.AssemblyNetworkPoke.Address = svrAddressBox.Text;
                App.AssemblyStorage.AssemblyNetworkPoke.Port = svrPortBox.Text;
                SetClientVisibility();
                MetroMessageBox.Show("Network Poke Client Started", "Network poking successfully connected as a client to " + svrAddressBox.Text);
            });
        }

        private void SessionManager_SessionDead(object sender, RunWorkerCompletedEventArgs e)
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

                serverSessionManager.SessionActive += ServerSessionManager_SessionActive;
                serverSessionManager.SessionActive += App.AssemblyStorage.AssemblySettings.HomeWindow.ServerSessionManager_SessionActive;
                serverSessionManager.SessionDead += SessionManager_SessionDead;
                serverSessionManager.SessionDead += App.AssemblyStorage.AssemblySettings.HomeWindow.SessionManager_SessionDead;
                serverSessionManager.ClientConnected += App.AssemblyStorage.AssemblySettings.HomeWindow.SessionManager_ClientConnected;
                serverSessionManager.ClientDisconnected += App.AssemblyStorage.AssemblySettings.HomeWindow.SessionManager_ClientDisconnected;

                App.AssemblyStorage.AssemblyNetworkPoke.PokeSessionManager = serverSessionManager;
                serverSessionManager.StartServer(endpoint);
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

                clientSesionManager.SessionActive += ClientSessionManager_SessionActive;
                clientSesionManager.SessionActive += App.AssemblyStorage.AssemblySettings.HomeWindow.ClientSessionManager_SessionActive;
                clientSesionManager.SessionDead += SessionManager_SessionDead;
                clientSesionManager.SessionDead += App.AssemblyStorage.AssemblySettings.HomeWindow.SessionManager_SessionDead;

                App.AssemblyStorage.AssemblyNetworkPoke.PokeSessionManager = clientSesionManager;
                if (!clientSesionManager.StartClient(endpoint))
                {
                    MetroMessageBox.Show("Network Poke Client Failure", "Could not start network poke connection to " + svrAddressBox.Text);
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
            srvAddressBar.Visibility = Visibility.Collapsed;
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
