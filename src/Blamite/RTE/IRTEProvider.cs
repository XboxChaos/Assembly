using Blamite.Blam;
using Blamite.IO;

namespace Blamite.RTE
{
	/// <summary>
	///     Real-time editing connection types.
	/// </summary>
	public enum RteConnectionType
	{
		ConsoleX360,
		LocalProcess
	}

	/// <summary>
	///     The interface for an RTE (real-time editing) provider.
	/// </summary>
	public interface IRteProvider
	{
		/// <summary>
		///     The type of connection that the provider will establish.
		/// </summary>
		RteConnectionType ConnectionType { get; }

		/// <summary>
		///     Obtains a stream which can be used to read and write a cache file's meta in realtime.
		///     The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to get a stream for.</param>
		/// <returns>The stream if it was opened successfully, or null otherwise.</returns>
		IStream GetMetaStream(ICacheFile cacheFile);
	}
}