using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Assembly.Helpers.Net.Sockets
{
    public class PokeCommandDispatcher : IPokeCommandHandler
    {
        public bool HandleMemoryCommand(MemoryCommand memory)
        {
            var maps = App.AssemblyStorage.AssemblyNetworkPoke.Maps;

            foreach (var map in maps)
            {
                var cacheFile = map.Item1;
                var rteProvider = map.Item2;

                if (memory.BuildName == cacheFile.BuildString && memory.CacheName == cacheFile.InternalName)
                {
                    using (var metaStream = rteProvider.GetMetaStream(cacheFile))
                    {
                        if (metaStream != null)
                        {
                            foreach (var action in memory.Actions)
                            {
                                if (cacheFile.MetaArea.ContainsBlockPointer(action.Position, (uint)action.Buffer.Length))
                                {
                                    metaStream.SeekTo(action.Position);
                                    metaStream.WriteBlock(action.Buffer);
                                }
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
