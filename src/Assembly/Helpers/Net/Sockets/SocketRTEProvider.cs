using Blamite.Blam;
using Blamite.IO;
using Blamite.RTE;

namespace Assembly.Helpers.Net.Sockets
{
    public class SocketRTEProvider : RTEProvider
    {
        private IPokeSessionManager _sessionManager;
        public string ExeName { get; set; }

        public SocketRTEProvider(IPokeSessionManager starter) : base(null)
        {
            _sessionManager = starter;
        }

        public override RTEConnectionType ConnectionType { get; }

        public override IStream GetCacheStream(ICacheFile cacheFile, ITag tag)
        {
            return new EndianStream(new SocketStream(_sessionManager, cacheFile.BuildString, cacheFile.InternalName), cacheFile.Endianness);
        }

        public void Kill()
        {
            _sessionManager.Kill(null);
        }
    }
}
