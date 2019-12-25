using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly.Helpers.Net.Sockets
{
    public class PokeCommandDispatcher : IPokeCommandHandler
    {
        public bool HandleMemoryCommand(MemoryCommand memory)
        {
            var maps = App.AssemblyStorage.AssemblyNetworkPoke.Maps;

            foreach (var map in maps)
            {
                if (map.HandleMemoryCommand(memory))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
