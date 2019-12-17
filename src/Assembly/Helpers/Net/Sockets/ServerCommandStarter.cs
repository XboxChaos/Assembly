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
            var thread = new Thread(new ThreadStart(delegate
            {
                while (true)
                {
                    _server.ReceiveCommand(handler);
                }
            }));
            thread.Start();
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
    }
}
