using Blamite.Blam;
using Blamite.IO;
using Blamite.RTE;
using Blamite.RTE.H2Vista;

namespace Assembly.Helpers.Net.Sockets
{
    public class SocketRTEProvider : IRTEProvider
    {
        private IPokeCommandStarter _starter;
        public string ExeName { get; set; }

        public SocketRTEProvider(IPokeCommandStarter starter)
        {
            _starter = starter;
        }

        public RTEConnectionType ConnectionType { get; private set; }

        public IStream GetMetaStream(ICacheFile cacheFile)
        {
            return new EndianStream(new SocketStream(_starter), cacheFile.Endianness);
        }

        public bool IsDead()
        {
            return _starter.IsDead();
        }
    }
}
