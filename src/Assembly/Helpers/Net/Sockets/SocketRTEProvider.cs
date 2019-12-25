using Blamite.Blam;
using Blamite.IO;
using Blamite.RTE;
using Blamite.RTE.H2Vista;

namespace Assembly.Helpers.Net.Sockets
{
    public class SocketRTEProvider : IRTEProvider
    {
        private IPokeSessionManager _starter;
        public string ExeName { get; set; }

        public SocketRTEProvider(IPokeSessionManager starter)
        {
            _starter = starter;
        }

        public RTEConnectionType ConnectionType { get; private set; }

        public IStream GetMetaStream(ICacheFile cacheFile)
        {
            return new EndianStream(new SocketStream(_starter, cacheFile.BuildString, cacheFile.InternalName), cacheFile.Endianness);
        }

        public void Kill()
        {
            _starter.Kill();
        }
    }
}
