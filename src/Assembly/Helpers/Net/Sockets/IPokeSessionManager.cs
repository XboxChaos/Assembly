using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly.Helpers.Net.Sockets
{
    public interface IPokeSessionManager
    {
        void SendMemoryCommand(MemoryCommand freeze);

        void Kill(SessionDiedEventArgs ex);

        event EventHandler SessionActivated;
        event EventHandler<SessionDiedEventArgs> SessionDied;
    }

    public class ClientEventArgs : EventArgs
    {
        public ClientEventArgs(string clientInfo)
        {
            ClientInfo = clientInfo;
        }

        public string ClientInfo { get; set; }
    }

    public class SessionDiedEventArgs : EventArgs
    {
        public SessionDiedEventArgs(Exception error)
        {
            Error = error;
        }

        public Exception Error { get; set; }
    }
}
