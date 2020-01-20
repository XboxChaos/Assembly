using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Diagnostics;
using Assembly.Metro.Dialogs;
using System.ComponentModel;

namespace Assembly.Helpers.Net.Sockets
{
    public class ClientPokeSessionManager : IPokeSessionManager
    {
        private NetworkPokeClient _client;
        private IPokeCommandHandler _handler;
        private bool _sessionActive;
        private BackgroundWorker _worker;

        public event EventHandler SessionActivated;
        public event EventHandler<SessionDiedEventArgs> SessionDied;

        public ClientPokeSessionManager(IPokeCommandHandler handler)
        {
            _client = new NetworkPokeClient();
            _handler = handler;
        }

        public void StartClient(IPEndPoint endpoint)
        {
            _client.Connect(endpoint);

            _worker = new BackgroundWorker();
            _worker.DoWork += DoClientReceiveWork;
            _worker.RunWorkerCompleted += DoClientWorkerCompleted;
            _worker.WorkerSupportsCancellation = true;
            _worker.RunWorkerAsync();
            _sessionActive = true;
            SessionActivated(this, new EventArgs());
        }

        public void SendMemoryCommand(MemoryCommand memory)
        {
           _client.SendCommand(memory);
        }

        public void Kill(SessionDiedEventArgs ex)
        {
            if (_sessionActive)
            {
                _sessionActive = false;
                _worker.CancelAsync();
                _client.Close();
                SessionDied(this, ex);
            }
        }

        private void DoClientReceiveWork(object sender, DoWorkEventArgs e)
        {
            while (!_worker.CancellationPending)
            {
                _client.ReceiveCommand(_handler);
            }
        }

        private void DoClientWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var sessionDiedEvent = new SessionDiedEventArgs(e.Error);
            Kill(sessionDiedEvent);
        }
    }
}