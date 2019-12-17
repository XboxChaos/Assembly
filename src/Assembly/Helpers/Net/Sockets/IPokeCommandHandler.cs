using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.Net.Sockets
{
    public interface IPokeCommandHandler
    {
        void HandleTestCommand(TestCommand test);

        void HandleMemoryCommand(MemoryCommand memory);
    }
}
