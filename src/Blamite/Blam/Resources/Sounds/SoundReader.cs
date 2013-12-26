using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.Resources.Sounds
{
	/// <summary>
	/// 
	/// </summary>
	public static class SoundReader
	{
		/// <summary>
		/// Reads the resource data for a sound from a stream and passes it back into the ISound.
		/// </summary>
		/// <param name="reader">The stream to read the sound data from.</param>
		/// <param name="sound">The sound's metadata.</param>
		/// <param name="resourceCacheFolder"> </param>
		/// <param name="buildInfo">Information about the cache file's target engine.</param>
		/// <param name="cacheFile"> </param>
		/// <param name="resourcePages"> </param>
		public static void ReadSoundData(IReader reader, ISound sound, ICacheFile cacheFile, ResourcePage[] resourcePages,
			string resourceCacheFolder, EngineDescription buildInfo)
		{
			// TODO: me
		}
	}
}

