using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Assembly.Helpers.Net.Sockets
{
    public class NetworkPokeClient
    {
        private Socket _socket;

        public void Connect(IPEndPoint endpoint)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(endpoint);
        }

        public void SendCommand(PokeCommand command)
        {
            using (var netStream = new NetworkStream(_socket, false))
            {
                using (var bufStream = new BufferedStream(netStream))
                {
                    CommandSerialization.SerializeCommand(command, bufStream);
                }
            }
        }

        public void ReceiveCommand(IPokeCommandHandler handler)
        {
            using (var stream = new NetworkStream(_socket, false))
            {
                var command = CommandSerialization.DeserializeCommand(stream);
                if (command != null)
                    command.Handle(handler);
                else
                    Close();
            }
        }

        public void Close()
        {
            if (_socket != null)
                _socket.Close();
        }
    }
}