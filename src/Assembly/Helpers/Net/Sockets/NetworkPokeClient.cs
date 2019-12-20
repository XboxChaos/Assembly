using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

namespace Assembly.Helpers.Net.Sockets
{
    public class NetworkPokeClient
    {
        private Socket _socket;
        private IPEndPoint _endpoint;

        // TODO: Should we make it possible to set the port number somehow?
        private static int Port = 19002;

        public NetworkPokeClient(IPAddress address)
        {
            _endpoint = new IPEndPoint(address, Port);
        }

        public bool Connect()
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Connect(_endpoint);
                return true;
            }
            catch (SocketException)
            {

            }
            return false;
        }

        public bool SendCommand(PokeCommand command)
        {
            try
            {
                using (var stream = new NetworkStream(_socket, false))
                {
                    CommandSerialization.SerializeCommand(command, stream);
                }
                return true;
            }
            catch (IOException)
            {

            }
            return false;
        }

        public bool ReceiveCommand(IPokeCommandHandler handler)
        {
            try
            {
                using (var stream = new NetworkStream(_socket, false))
                {
                    var command = CommandSerialization.DeserializeCommand(stream);
                    if (command != null)
                        command.Handle(handler);
                }
                return true;
            }
            catch (IOException)
            {

            }
            return false;
        }

        public void Close()
        {
            _socket.Close();
        }
    }
}