using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assembly.Metro.Dialogs;
using System.Collections.ObjectModel;
using System.Net;
using System.ComponentModel;

namespace Assembly.Helpers.Net.Sockets
{
    public class ServerPokeSessionManager : IPokeSessionManager
    {
        private NetworkPokeServer _server;
        private IPokeCommandHandler _handler;
        private volatile bool _sessionActive;
        private BackgroundWorker _serverBackgroundWoker;

        public event EventHandler SessionActive;
        public event EventHandler SessionDead;
        public event EventHandler<ClientEventArgs> ClientConnected;
        public event EventHandler<ClientEventArgs> ClientDisconnected;

        public ServerPokeSessionManager(IPokeCommandHandler handler)
        {
            _server = new NetworkPokeServer();
            _server.ClientConnected += netpokeClientConnected;
            _server.ClientDisconnected += netpokeClientDisconnected;

            _handler = handler;
        }

        public void StartServer(IPEndPoint endpoint)
        {
            try
            {
                _server.Listen(endpoint);
                _serverBackgroundWoker = new BackgroundWorker();
                _serverBackgroundWoker.DoWork += DoServerReceiveWork;
                _serverBackgroundWoker.RunWorkerCompleted += DoServerReceiveDone;
                _serverBackgroundWoker.RunWorkerAsync();
                _sessionActive = true;
                SessionActive(this, new EventArgs());
            }
            catch (Exception)
            {
                Kill();
            }
        }

        public void SendMemoryCommand(MemoryCommand memory)
        {
            _server.SendCommandToAll(memory);
            memory.Handle(_handler);
        }

        public void Kill()
        {
            _server.Close();
            if (_sessionActive)
            {
                _sessionActive = false;
                SessionDead(this, new EventArgs());
            }
        }

        private void DoServerReceiveWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (_server.ReceiveCommand(_handler)) ;
            }
            catch (Exception)
            {
                Kill();
            }
        }

        private void DoServerReceiveDone(object sender, RunWorkerCompletedEventArgs e)
        {
            Kill();
        }

        private void netpokeClientConnected(object sender, ClientEventArgs e)
        {
            ClientConnected(sender, e);
        }

        private void netpokeClientDisconnected(object sender, ClientEventArgs e)
        {
            ClientDisconnected(sender, e);
        }
    }
}
