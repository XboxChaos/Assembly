using System;
using Blamite.Blam.SecondGen;
using Blamite.Blam.ThirdGen;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam
{
	/// <summary>
	///     Provides methods for loading cache files.
	/// </summary>
	public static class CacheFileLoader
	{
		/// <summary>
		///     Loads a cache file from a stream.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="engineDb">The engine database to use to process the cache file.</param>
		/// <returns>The cache file that was loaded.</returns>
		/// <exception cref="ArgumentException">Thrown if the cache file is invalid.</exception>
		/// <exception cref="NotSupportedException">Thrown if the cache file's target engine is not supported.</exception>
		public static ICacheFile LoadCacheFile(IReader reader, EngineDatabase engineDb)
		{
			EngineDescription tempDesc;
			return LoadCacheFile(reader, engineDb, out tempDesc);
		}

		/// <summary>
		///     Loads a cache file from a stream.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="engineDb">The engine database to use to process the cache file.</param>
		/// <param name="engineInfo">On output, this will contain the cache file's engine description.</param>
		/// <returns>The cache file that was loaded.</returns>
		/// <exception cref="ArgumentException">Thrown if the cache file is invalid.</exception>
		/// <exception cref="NotSupportedException">Thrown if the cache file's target engine is not supported.</exception>
		public static ICacheFile LoadCacheFile(IReader reader, EngineDatabase engineDb, out EngineDescription engineInfo)
		{
			// Set the reader's endianness based upon the file's header magic
			reader.SeekTo(0);
			byte[] headerMagic = reader.ReadBlock(4);
			reader.Endianness = DetermineCacheFileEndianness(headerMagic);

			// Load engine version info
			var version = new CacheFileVersionInfo(reader);
			if (version.Engine != EngineType.SecondGeneration && version.Engine != EngineType.ThirdGeneration)
				throw new NotSupportedException("Engine not supported");

			// Load build info
			engineInfo = engineDb.FindEngineByVersion(version.BuildString);
			if (engineInfo == null)
				throw new NotSupportedException("Engine version \"" + version.BuildString + "\" not supported");

			// Load the cache file depending upon the engine version
			switch (version.Engine)
			{
				case EngineType.SecondGeneration:
					return new SecondGenCacheFile(reader, engineInfo, version.BuildString);

				case EngineType.ThirdGeneration:
					return new ThirdGenCacheFile(reader, engineInfo, version.BuildString);

				default:
					throw new NotSupportedException("Engine not supported");
			}
		}

		private static Endian DetermineCacheFileEndianness(byte[] headerMagic)
		{
			if (headerMagic[0] == 'h' && headerMagic[1] == 'e' && headerMagic[2] == 'a' && headerMagic[3] == 'd')
				return Endian.BigEndian;
			if (headerMagic[0] == 'd' && headerMagic[1] == 'a' && headerMagic[2] == 'e' && headerMagic[3] == 'h')
				return Endian.LittleEndian;

			throw new ArgumentException("Invalid cache file header magic");
		}
	}
}