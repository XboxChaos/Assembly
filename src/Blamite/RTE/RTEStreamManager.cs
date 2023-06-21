using Blamite.Blam;
using Blamite.IO;

namespace Blamite.RTE
{
	/// <summary>
	///     An implementation of IStreamManager which returns streams opened from an IRTEProvider.
	/// </summary>
	public class RTEStreamManager : IStreamManager
	{
		private readonly ICacheFile _cacheFile;
		private readonly RTEProvider _provider;

		public RTEStreamManager(RTEProvider provider, ICacheFile cacheFile)
		{
			_provider = provider;
			_cacheFile = cacheFile;
		}

		public IReader OpenRead()
		{
			return _provider.GetCacheStream(_cacheFile);
		}

		public IWriter OpenWrite()
		{
			return _provider.GetCacheStream(_cacheFile);
		}

		public IStream OpenReadWrite()
		{
			return _provider.GetCacheStream(_cacheFile);
		}
	}
}