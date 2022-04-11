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

			return LoadCacheFileWithEngineDescription(reader, filePath, engineInfo);
		}

		/// <summary>
		///     Loads a cache file from a stream using a supplied EngineDescription
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="filePath">The full file path of the cache file.</param>
		/// <param name="engineInfo">The engine description that will.</param>
		/// <returns>The cache file that was loaded.</returns>
		/// <exception cref="NotSupportedException">Thrown if the cache file's target engine is not supported.</exception>
		public static ICacheFile LoadCacheFileWithEngineDescription(IReader reader, string filePath, EngineDescription engineInfo)
		{
			if (engineInfo == null)
				throw new NotSupportedException("Engine build of given cache file \"" + Path.GetFileName(filePath) + "\" not supported");

			//reader is often initialized with Big Endian, switch it to match the EngineDescription
			reader.Endianness = engineInfo.Endian;

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
		///		Finds the first EngineDatabase that matches a cache file from a stream, if available.
		///		This is the legacy behavior for uses where showing a selection UI isn't as feasible.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="engineDb">The engine database to use to process the cache file.</param>
		/// <returns>The EngineDescription used to describe the cache file, otherwise null.</returns>
		public static EngineDescription FindEngineDescription(IReader reader, EngineDatabase engineDb)
		{
			var matches = FindEngineDescriptions(reader, engineDb);

			if (matches.Count > 0)
				return matches[0];
			else
				return null;
		}

		/// <summary>
		///		Finds any EngineDatabase that matches a cache file from a stream, if available.
		///		List return is intended to be passed to the user to make the final choice in case of > 1 matches.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="engineDb">The engine database to use to process the cache file.</param>
		/// <returns>List containing matching EngineDescriptions that could possibly describe the cache file, if any.</returns>
		public static List<EngineDescription> FindEngineDescriptions(IReader reader, EngineDatabase engineDb)
		{
			// Set the reader's endianness based upon the file's header magic
			reader.SeekTo(0);
			byte[] headerMagic = reader.ReadBlock(4);
			reader.Endianness = DetermineCacheFileEndianness(headerMagic);

			reader.SeekTo(0x4);
			int fileVersion = reader.ReadInt32();

			var possibleEngines = engineDb.FindEnginesByVersion(fileVersion, reader.Endianness);

			//reduce extra reads by caching the value at each offset
			Dictionary<int, string> offsetCache = new Dictionary<int, string>();

			List<EngineDescription> matches = new List<EngineDescription>();

			foreach (EngineDescription engine in possibleEngines)
			{
				if (offsetCache.ContainsKey(engine.BuildStringOffset))
				{
					if (offsetCache[engine.BuildStringOffset] == engine.BuildVersion)
					{
						matches.Add(engine);
						if (!string.IsNullOrEmpty(engine.BuildVersion))
							break;
					}
						
					continue;
				}

				reader.SeekTo(engine.BuildStringOffset);
				string buildString = reader.ReadAscii(0x20);

				if (buildString == engine.BuildVersion)
				{
					matches.Add(engine);
					if (!string.IsNullOrEmpty(engine.BuildVersion))
						break;
				}

				offsetCache[engine.BuildStringOffset] = buildString;
			}

			return matches;
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