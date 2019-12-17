using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Diagnostics;

namespace Assembly.Helpers.Net.Sockets
{
    public class ClientCommandStarter : IPokeCommandStarter
    {
        private NetworkPokeClient _client;
        public ClientCommandStarter(string IpAddress, IPokeCommandHandler handler)
        {
            _client = new NetworkPokeClient(IPAddress.Parse(IpAddress));
            var thread = new Thread(new ThreadStart(delegate
            {
                while (true)
                {
                    _client.ReceiveCommand(handler);
                }
            }));
            thread.Start();
        }

        public void StartTestCommand(TestCommand test)
        {
            _client.SendCommand(test);
        }

        public void StartMemoryCommand(MemoryCommand memory)
        {
            _client.SendCommand(memory);
        }
    }
}