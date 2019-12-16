using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace Assembly.Helpers.Net.Sockets
{
    public class NetworkPokeServer
    {
        private readonly List<Socket> _clients = new List<Socket>();

        public NetworkPokeServer()
        {
            var hostIp = IPAddress.Any;
            var hostEndpoint = new IPEndPoint(hostIp, 12345);
            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind to our local endpoint
            listener.Bind(hostEndpoint);
            listener.Listen(128); // Listen with a pending connection queue size of 128

            // Begin accepting incoming connections
            listener.BeginAccept(ConnectClient, listener);

        }

        private void ConnectClient(IAsyncResult result)
        {
            var listener = (Socket)result.AsyncState;
            var client = listener.EndAccept(result);
            listener.BeginAccept(ConnectClient, listener);
            lock (_clients)
            {
                _clients.Add(client);
            }
        }

        public void ReceiveCommand(IPokeCommandHandler handler)
        {
            List<Socket> readyClients;
            lock (_clients)
            {
                readyClients = new List<Socket>(_clients);
            }
            if (readyClients.Count == 0)
                return;

            //wait for a client to have a command
            Socket.Select(readyClients, null, null, -1);
            foreach (var socket in readyClients)
            {
                using (var stream = new NetworkStream(socket, false))
                {
                    var commandType = (PokeCommandType)stream.ReadByte();
                    PokeCommand command;
                    switch (commandType)
                    {
                        case PokeCommandType.Test:
                            command = new TestCommand();
                            break;
                        default:
                            continue;
                    }
                    command.Deserialize(stream);
                    command.Handle(handler);
                }
            }
        }
    }
}
