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

        public NetworkPokeClient(IPAddress address)
        {
            var endpoint = new IPEndPoint(address, 12345);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(endpoint);
        }

        public void SendCommand(PokeCommand command)
        {
            using (var stream = new NetworkStream(_socket, false))
            {
                var type = (byte)command.Type;
                stream.WriteByte(type);
                command.Serialize(stream);
            }
        }


    }
}