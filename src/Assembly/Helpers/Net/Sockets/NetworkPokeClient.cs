using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Assembly.Helpers.Net.Sockets
{
    public class NetworkPokeClient
    {
        private Socket _socket;

        // TODO: Should we make it possible to set the port number somehow?
        private static int Port = 19002;

        public NetworkPokeClient(IPAddress address)
        {
            var endpoint = new IPEndPoint(address, Port);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(endpoint);
        }

        public void SendCommand(PokeCommand command)
        {
            using (var stream = new NetworkStream(_socket, false))
            {
                CommandSerialization.SerializeCommand(command, stream);
            }
        }

        public void ReceiveCommand(IPokeCommandHandler handler)
        {
            using (var stream = new NetworkStream(_socket, false))
            {
                var command = CommandSerialization.DeserializeCommand(stream);
                command.Handle(handler);
            }
        }
    }
}