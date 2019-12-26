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

        public event EventHandler SessionActive;
        public event EventHandler<RunWorkerCompletedEventArgs> SessionDead;

        public ClientPokeSessionManager(IPokeCommandHandler handler)
        {
            _client = new NetworkPokeClient();
            _handler = handler;
        }

        public bool StartClient(IPEndPoint endpoint)
        {
            try
            {
                _client.Connect(endpoint);
            }
            catch (Exception)
            {
                Kill(null);
                return false;
            }

            var serverBackgroundWorker = new BackgroundWorker();
            serverBackgroundWorker.DoWork += DoClientReceiveWork;
            serverBackgroundWorker.RunWorkerCompleted += DoClientWorkerCompleted;
            serverBackgroundWorker.RunWorkerAsync();
            _sessionActive = true;
            SessionActive(this, new EventArgs());
            return true;
        }

        public void SendMemoryCommand(MemoryCommand memory)
        {
           _client.SendCommand(memory);
        }

        public void Kill(RunWorkerCompletedEventArgs ex)
        {
            _client.Close();
            if (_sessionActive)
            {
                _sessionActive = false;
                SessionDead(this, ex);
            }
        }

        private void DoClientReceiveWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                _client.ReceiveCommand(_handler);
            }
        }

        private void DoClientWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Kill(e);
        }
    }
}