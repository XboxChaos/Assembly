using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assembly.Metro.Dialogs;
using System.Collections.ObjectModel;

namespace Assembly.Helpers.Net.Sockets
{
    public class ServerCommandStarter : IPokeCommandStarter
    {
        private NetworkPokeServer _server;
        private IPokeCommandHandler _handler;
        private volatile bool _isFailed;

        public ServerCommandStarter(IPokeCommandHandler handler, ObservableCollection<string> clientList)
        {
            _server = new NetworkPokeServer(clientList);
            _handler = handler;
            _isFailed = false;
        }

        public bool StartServer()
        {
            if (!_server.Listen())
            {
                return false;
            }

            var thread = new Thread(new ThreadStart(delegate
            {
                while (_server.ReceiveCommand(_handler))
                {
                    
                }
                Kill();
            }));
            thread.IsBackground = true;
            thread.Start();

            return true;
        }


        public void StartMemoryCommand(MemoryCommand memory)
        {
            _server.SendCommandToAll(memory);
            memory.Handle(_handler);
        }

        public void StartTestCommand(TestCommand test)
        {
            throw new NotImplementedException();
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
                _server.Close();
                App.AssemblyStorage.AssemblySettings.HomeWindow.Dispatcher.Invoke(new Action(
                    () =>
                        MetroMessageBox.Show("Server Killed", "Poke server was killed.  Reverting to local poke...")));
            }
            return IsDead();
        }
    }
}
