using System;
using System.IO;
using Blamite.Blam.SecondGen;
using Blamite.Blam.ThirdGen;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Blam.FirstGen;
using System.Collections.Generic;

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
		/// <param name="filePath">The full file path of the cache file.</param>
		/// <param name="engineDb">The engine database to use to process the cache file.</param>
		/// <returns>The cache file that was loaded.</returns>
		/// <exception cref="ArgumentException">Thrown if the cache file is invalid.</exception>
		/// <exception cref="NotSupportedException">Thrown if the cache file's target engine is not supported.</exception>
		public static ICacheFile LoadCacheFile(IReader reader, string filePath, EngineDatabase engineDb)
		{
			return LoadCacheFile(reader, filePath, engineDb, out _);
		}

		/// <summary>
		///     Loads a cache file from a stream.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="filePath">The full file path of the cache file.</param>
		/// <param name="engineDb">The engine database to use to process the cache file.</param>
		/// <param name="engineInfo">On output, this will contain the cache file's engine description.</param>
		/// <returns>The cache file that was loaded.</returns>
		/// <exception cref="NotSupportedException">Thrown if the cache file's target engine is not supported.</exception>
		public static ICacheFile LoadCacheFile(IReader reader, string filePath, EngineDatabase engineDb, out EngineDescription engineInfo)
		{
			engineInfo = FindEngineDescription(reader, engineDb);

			if (engineInfo == null)
				throw new NotSupportedException("Engine build of given cache file \"" + Path.GetFileName(filePath) + "\" not supported");

			// Load the cache file depending upon the engine version
			switch (engineInfo.Engine)
			{
				case EngineType.FirstGeneration:
					return new FirstGenCacheFile(reader, engineInfo, filePath);

				case EngineType.SecondGeneration:
					return new SecondGenCacheFile(reader, engineInfo, filePath);

				case EngineType.ThirdGeneration:
					return new ThirdGenCacheFile(reader, engineInfo, filePath);

				default:
					throw new NotSupportedException("Engine not supported");
			}
		}

		/// <summary>
		///		Finds the EngineDatabase that matches a cache file from a stream, if available.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="engineDb">The engine database to use to process the cache file.</param>
		/// <returns>The EngineDescription used to describe the cache file, otherwise null.</returns>
		public static EngineDescription FindEngineDescription(IReader reader, EngineDatabase engineDb)
		{
			// Set the reader's endianness based upon the file's header magic
			reader.SeekTo(0);
			byte[] headerMagic = reader.ReadBlock(4);
			reader.Endianness = DetermineCacheFileEndianness(headerMagic);

			reader.SeekTo(0x4);
			int fileVersion = reader.ReadInt32();

			var matches = engineDb.FindEnginesByVersion(fileVersion, reader.Endianness);

			Dictionary<int, string> offsetCache = new Dictionary<int, string>();

			foreach (EngineDescription engine in matches)
			{
				if (offsetCache.ContainsKey(engine.BuildStringOffset))
				{
					if (offsetCache[engine.BuildStringOffset] == engine.BuildVersion)
						return engine;
					else
						continue;
				}	

				reader.SeekTo(engine.BuildStringOffset);
				string buildString = reader.ReadAscii();

				if (buildString == engine.BuildVersion)
					return engine;

				offsetCache[engine.BuildStringOffset] = buildString;
			}

			return null;
		}

		public static Endian DetermineCacheFileEndianness(byte[] headerMagic)
		{
			if (headerMagic[0] == 'h' && headerMagic[1] == 'e' && headerMagic[2] == 'a' && headerMagic[3] == 'd')
				return Endian.BigEndian;
			if (headerMagic[0] == 'd' && headerMagic[1] == 'a' && headerMagic[2] == 'e' && headerMagic[3] == 'h')
				return Endian.LittleEndian;

			throw new ArgumentException("Invalid cache file header magic");
		}
	}
}