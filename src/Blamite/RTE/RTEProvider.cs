using Blamite.Blam;
using Blamite.IO;
using Blamite.Serialization;

namespace Blamite.RTE
{
	/// <summary>
	///     Real-time editing connection types.
	/// </summary>
	public enum RTEConnectionType
	{
		None,
		ConsoleXbox,
		ConsoleXbox360,
		ConsoleXboxOne,
		LocalProcess32,
		LocalProcess64
	}

	public abstract class RTEProvider
	{
		protected readonly EngineDescription _buildInfo;
		protected bool _hadToGuessVersion = false;

		protected RTEProvider(EngineDescription engine)
		{
			_buildInfo = engine;
		}

		public abstract RTEConnectionType ConnectionType { get; }

		protected string GuessError
		{
			get
			{
				return _hadToGuessVersion ?
				"\r\n\r\nNOTE: Due to missing definitions for the current game version, or the inability to confirm the game version, the latest available definitions were used as a fallback. As a result this could be the cause of the error. Please confirm your poking XMLs." :
				string.Empty;
			}
		}

		/// <summary>
		/// If GetCacheStream returns null, this should explain why.
		/// </summary>
		public string ErrorMessage { get; protected set; }

		/// <summary>
		///     Obtains a stream which can be used to read and write a cache file's meta in realtime.
		///     The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to get a stream for.</param>
		/// <param name="tag">The tag to be poked; only needed for Eldorado.</param>
		/// <returns>The stream if it was opened successfully, or null otherwise, with <seealso cref="ErrorMessage"/> containing the reason.</returns>
		public abstract IStream GetCacheStream(ICacheFile cacheFile, ITag tag);

	}
}
