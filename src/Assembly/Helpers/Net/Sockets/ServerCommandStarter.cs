using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assembly.Helpers.Net.Sockets
{
    public class ServerCommandStarter : IPokeCommandStarter
    {
        private NetworkPokeServer _server;
        private IPokeCommandHandler _handler;

        public ServerCommandStarter(IPokeCommandHandler handler)
        {
            _server = new NetworkPokeServer();
            _handler = handler;
        }

        public bool StartServer()
        {
            if (!_server.Listen())
            {
                return false;
            }

            var thread = new Thread(new ThreadStart(delegate
            {
                while (true)
                {
                    _server.ReceiveCommand(_handler);
                }
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
            return false;
        }
    }
}
