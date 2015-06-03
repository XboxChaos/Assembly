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
		private readonly IRTEProvider _provider;
		private readonly ITag _tag;

		public RTEStreamManager(IRTEProvider provider, ICacheFile cacheFile, ITag tag)
		{
			_provider = provider;
			_cacheFile = cacheFile;
			_tag = tag;
		}

		public IReader OpenRead()
		{
			return _provider.GetMetaStream(_cacheFile, _tag);
		}

		public IWriter OpenWrite()
		{
			return _provider.GetMetaStream(_cacheFile, _tag);
		}

		public IStream OpenReadWrite()
		{
			return _provider.GetMetaStream(_cacheFile, _tag);
		}
	}
}