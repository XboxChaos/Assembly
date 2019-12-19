using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly.Helpers.Net.Sockets
{
    public interface IPokeCommandStarter
    {
        void StartTestCommand(TestCommand test);

        void StartMemoryCommand(MemoryCommand freeze);

        bool IsDead();
    }
}
