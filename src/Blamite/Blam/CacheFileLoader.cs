using System;
using Blamite.Blam.SecondGen;
using Blamite.Blam.ThirdGen;
using Blamite.Blam.FourthGen;
using Blamite.Serialization;
using Blamite.IO;
using System.IO;

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
        //public static ICacheFile LoadCacheFile(IReader map_reader, IReader tag_reader, IReader string_reader, EngineDatabase engineDb)
        public static ICacheFile LoadCacheFile(IReader map_reader, EngineDatabase engineDb)
		{
			EngineDescription tempDesc;
            return LoadCacheFile(map_reader, null, null, engineDb, out tempDesc);
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
        public static ICacheFile LoadCacheFile(IReader map_reader, IReader tag_reader, IReader string_reader, EngineDatabase engineDb, out EngineDescription engineInfo)
		{
			// Set the reader's endianness based upon the file's header magic
            map_reader.SeekTo(0);
            byte[] headerMagic = map_reader.ReadBlock(4);
            map_reader.Endianness = DetermineCacheFileEndianness(headerMagic);
            tag_reader.Endianness = DetermineCacheFileEndianness(headerMagic);
            string_reader.Endianness = DetermineCacheFileEndianness(headerMagic);

			// Load engine version info
            var version = new CacheFileVersionInfo(map_reader);
            if (version.Engine != EngineType.SecondGeneration && version.Engine != EngineType.ThirdGeneration && version.Engine != EngineType.FourthGeneration)
				throw new NotSupportedException("Engine not supported");

			// Load build info
			engineInfo = engineDb.FindEngineByVersion(version.BuildString);
			if (engineInfo == null)
				throw new NotSupportedException("Engine version \"" + version.BuildString + "\" not supported");

			// Load the cache file depending upon the engine version
			switch (version.Engine)
			{
				case EngineType.SecondGeneration:
                    return new SecondGenCacheFile(map_reader, engineInfo, version.BuildString);

				case EngineType.ThirdGeneration:
                    return new ThirdGenCacheFile(map_reader, engineInfo, version.BuildString);

                case EngineType.FourthGeneration:
                    if (tag_reader == null) throw new Exception("Can't load Version 4 cache file without tags file.");
                    if (string_reader == null) throw new Exception("Can't load Version 4 cache file without strings file.");

                    return new FourthGenCacheFile(map_reader, tag_reader, string_reader, engineInfo, version.BuildString);

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