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
        public event EventHandler SessionDead;

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
                Kill();
                return false;
            }

            var serverBackgroundWorker = new BackgroundWorker();
            serverBackgroundWorker.DoWork += DoClientReceiveWork;
            serverBackgroundWorker.RunWorkerAsync();
            _sessionActive = true;
            SessionActive(this, new EventArgs());
            return true;
        }

        public void SendMemoryCommand(MemoryCommand memory)
        {
            try
            {
                _client.SendCommand(memory);
            }
            catch (Exception)
            {
                Kill();
                _handler.HandleMemoryCommand(memory);
            }
        }

        public void Kill()
        {
            _client.Close();
            if (_sessionActive)
            {
                _sessionActive = false;
                SessionDead(this, new EventArgs());
            }
        }

        private void DoClientReceiveWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (true)
                {
                    _client.ReceiveCommand(_handler);
                }
            }
            catch (Exception)
            {
                Kill();
            }
        }
    }
}