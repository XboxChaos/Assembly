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
using System.Net.Sockets;
using System.IO;

namespace Assembly.Helpers.Net.Sockets
{
    public class ServerPokeSessionManager : IPokeSessionManager
    {
        private NetworkPokeServer _server;
        private IPokeCommandHandler _handler;
        private volatile bool _sessionActive;
        private BackgroundWorker _serverBackgroundWoker;

        public event EventHandler SessionActive;
        public event EventHandler<RunWorkerCompletedEventArgs> SessionDead;
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
            }
            catch (Exception ex)
            {
                Kill(null);
            }

            _serverBackgroundWoker = new BackgroundWorker();
            _serverBackgroundWoker.DoWork += DoServerReceiveWork;
            _serverBackgroundWoker.RunWorkerCompleted += DoServerReceiveDone;
            _serverBackgroundWoker.RunWorkerAsync();
            _sessionActive = true;
            SessionActive(this, new EventArgs());
        }

        public void SendMemoryCommand(MemoryCommand memory)
        {
            _server.SendCommandToAll(memory);
            memory.Handle(_handler);
        }

        public void Kill(RunWorkerCompletedEventArgs ex)
        {
            _server.Close();
            if (_sessionActive)
            {
                _sessionActive = false;
                SessionDead(this, ex);
            }
        }

        private void DoServerReceiveWork(object sender, DoWorkEventArgs e)
        {
            while (_server.ReceiveCommand(_handler)) ;
        }

        private void DoServerReceiveDone(object sender, RunWorkerCompletedEventArgs e)
        {
            Kill(e);
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
