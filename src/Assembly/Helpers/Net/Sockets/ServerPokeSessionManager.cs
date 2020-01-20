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
        private BackgroundWorker _worker;

        public event EventHandler SessionActivated;
        public event EventHandler<SessionDiedEventArgs> SessionDied;
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
            _server.Listen(endpoint);

            _worker = new BackgroundWorker();
            _worker.DoWork += DoServerReceiveWork;
            _worker.RunWorkerCompleted += DoServerReceiveDone;
            _worker.WorkerSupportsCancellation = true;
            _worker.RunWorkerAsync();
            _sessionActive = true;
            SessionActivated(this, new EventArgs());
        }

        public void SendMemoryCommand(MemoryCommand memory)
        {
            _server.SendCommandToAll(memory);
            memory.Handle(_handler);
        }

        public void Kill(SessionDiedEventArgs ex)
        {
            if (_sessionActive)
            {
                _sessionActive = false;
                _worker.CancelAsync();
                _server.Close();
                SessionDied(this, ex);
            }
        }

        private void DoServerReceiveWork(object sender, DoWorkEventArgs e)
        {
            while (!_worker.CancellationPending)
            {
                _server.ReceiveCommand(_handler);
            }
        }

        private void DoServerReceiveDone(object sender, RunWorkerCompletedEventArgs e)
        {
            var sessionDiedEvent = new SessionDiedEventArgs(e.Error);
            Kill(sessionDiedEvent);
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
