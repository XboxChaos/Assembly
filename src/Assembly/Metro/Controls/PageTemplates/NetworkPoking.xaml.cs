using Assembly.Helpers.Net.Sockets;
using System;
using System.Net;
using System.Windows;
using System.Windows.Input;
using Assembly.Metro.Dialogs;

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

        private void startServerBtn_Click(object sender, RoutedEventArgs e)
        {
            //first check input is there.
            var endpoint = CreateIPEndpoint(null, svrPortBox.Text);
            if (endpoint != null)
            {
                var serverCommandStarter = new ServerCommandStarter(new PokeCommandDispatcher(), App.AssemblyStorage.AssemblyNetworkPoke.Clients);

                if (serverCommandStarter.StartServer(endpoint))
                {
                    App.AssemblyStorage.AssemblyNetworkPoke.NetworkRteProvider = new SocketRTEProvider(serverCommandStarter);
                    App.AssemblyStorage.AssemblyNetworkPoke.IsConnected = true;
                    App.AssemblyStorage.AssemblyNetworkPoke.IsServer = true;
                    App.AssemblyStorage.AssemblyNetworkPoke.Address = svrAddressBox.Text;
                    App.AssemblyStorage.AssemblyNetworkPoke.Port = svrPortBox.Text;
                    SetServerVisibility();
                    MetroMessageBox.Show("Server Start Success", "Network poke server successfully started!");
                }
                else
                {
                    MetroMessageBox.Show("Unable to start server", "Unable to bind to address and port.  Possible another network poke server is running already.");
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
                var clientCommandStarter = new ClientCommandStarter(new PokeCommandDispatcher());

                if (clientCommandStarter.StartClient(endpoint))
                {
                    App.AssemblyStorage.AssemblyNetworkPoke.NetworkRteProvider = new SocketRTEProvider(clientCommandStarter);
                    App.AssemblyStorage.AssemblyNetworkPoke.IsConnected = true;
                    App.AssemblyStorage.AssemblyNetworkPoke.IsServer = false;
                    App.AssemblyStorage.AssemblyNetworkPoke.Address = svrAddressBox.Text;
                    App.AssemblyStorage.AssemblyNetworkPoke.Port = svrPortBox.Text;
                    SetClientVisibility();
                    MetroMessageBox.Show("Client Start Success", "Network poke client successfully connected to " + svrAddressBox.Text + "!");
                }
                else
                {
                    MetroMessageBox.Show("Unable to start client", "Remote address is not a currently available network poke server.");
                }
            }
            else
            {
                MetroMessageBox.Show("Server Details Invalid", "Remote port or address was not the correct format.");
            }
        }

        private void disconnectButton_Click(object sender, RoutedEventArgs e)
        {
            App.AssemblyStorage.AssemblyNetworkPoke.NetworkRteProvider.Kill();
            App.AssemblyStorage.AssemblyNetworkPoke.IsConnected = false;
            App.AssemblyStorage.AssemblyNetworkPoke.IsServer = false;
            SetUnconnectedVisibility();
            App.AssemblyStorage.AssemblyNetworkPoke.NetworkRteProvider = null;
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
        }

        private void SetClientVisibility()
        {
            startClientBtn.Visibility = Visibility.Collapsed;
            startServerBtn.Visibility = Visibility.Collapsed;
            disconnectButton.Visibility = Visibility.Visible;
            svrAddressBox.IsReadOnly = true;
            svrPortBox.IsReadOnly = true;
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
