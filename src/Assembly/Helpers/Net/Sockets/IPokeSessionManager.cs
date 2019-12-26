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

        void Kill(RunWorkerCompletedEventArgs ex);

        event EventHandler SessionActive;
        event EventHandler<RunWorkerCompletedEventArgs> SessionDead;
    }

    public class ClientEventArgs : EventArgs
    {
        public ClientEventArgs(string clientInfo)
        {
            ClientInfo = clientInfo;
        }

        public string ClientInfo { get; set; }
    }
}
