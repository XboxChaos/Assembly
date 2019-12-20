using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Diagnostics;
using Assembly.Metro.Dialogs;

namespace Assembly.Helpers.Net.Sockets
{
    public class ClientCommandStarter : IPokeCommandStarter
    {
        private NetworkPokeClient _client;
        private IPokeCommandHandler _handler;
        private volatile bool _isFailed;

        public ClientCommandStarter(string IpAddress, IPokeCommandHandler handler)
        {
            _client = new NetworkPokeClient(IPAddress.Parse(IpAddress));
            _handler = handler;
            _isFailed = false;
        }

        public bool StartClient()
        {
            if (!_client.Connect())
            {
                return false;
            }

            var thread = new Thread(new ThreadStart(delegate
            {
                while (_client.ReceiveCommand(_handler))
                {
                }
                Kill();
            }));
            thread.IsBackground = true;
            thread.Start();

            return true;
        }

        public void StartTestCommand(TestCommand test)
        {
            if (!_client.SendCommand(test))
            {
                Kill();
            }
        }

        public void StartMemoryCommand(MemoryCommand memory)
        {
            if (!_client.SendCommand(memory))
            {
                // if we died, just send the command along local only.
                Kill();
                _handler.HandleMemoryCommand(memory);
            }
        }

        public bool IsDead()
        {
            return _isFailed;
        }

        public bool Kill()
        {
            if (!_isFailed)
            {
                _isFailed = true;
                _client.Close();
                App.AssemblyStorage.AssemblySettings.HomeWindow.Dispatcher.Invoke(new Action(
                    () =>
                        MetroMessageBox.Show("Client Connection Closed", "Connection to the poke server was closed.  Reverting to local poke...")));
            }
            return IsDead();
        }
    }
}