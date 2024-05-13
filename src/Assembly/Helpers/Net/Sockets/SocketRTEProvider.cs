using Blamite.Blam;
using Blamite.IO;
using Blamite.RTE;

namespace Assembly.Helpers.Net.Sockets
{
    public class SocketRTEProvider : IRTEProvider
    {
        private IPokeSessionManager _sessionManager;
        public string ExeName { get; set; }

        public SocketRTEProvider(IPokeSessionManager starter)
        {
            _sessionManager = starter;
        }

        public RTEConnectionType ConnectionType { get; private set; }

        public IStream GetMetaStream(ICacheFile cacheFile, ITag tag)
        {
            return new EndianStream(new SocketStream(_sessionManager, cacheFile.BuildString, cacheFile.InternalName), cacheFile.Endianness);
        }

        public void Kill()
        {
            _sessionManager.Kill(null);
        }
    }
}
