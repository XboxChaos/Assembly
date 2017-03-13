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
            string ns = null;
            return LoadCacheFile(map_reader, null, null, out ns, null, engineDb, out tempDesc);
		}

        private static FileStream TryInitFilestream(string filepath)
        {
            try
            {
                FileStream fs = File.OpenRead(filepath);
                return fs;
            }
            catch (Exception e)
            {
                return null;
            }
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
        public static ICacheFile LoadCacheFile(IReader map_reader, IReader tag_reader, IReader string_reader, out string tagnamesLocation, string filesLocation, EngineDatabase engineDb, out EngineDescription engineInfo)
		{
			// Set the reader's endianness based upon the file's header magic
            map_reader.SeekTo(0);
            byte[] headerMagic = map_reader.ReadBlock(4);
            Endian engianess = DetermineCacheFileEndianness(headerMagic);
            map_reader.Endianness = engianess;
            if(tag_reader != null) tag_reader.Endianness = engianess;
            if (tag_reader != null) string_reader.Endianness = engianess;

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
                    tagnamesLocation = null;
                    return new SecondGenCacheFile(map_reader, engineInfo, version.BuildString);

				case EngineType.ThirdGeneration:
                    tagnamesLocation = null;
                    return new ThirdGenCacheFile(map_reader, engineInfo, version.BuildString);

                case EngineType.FourthGeneration:
                    if (tag_reader == null || tag_reader.BaseStream.Length == 0) throw new Exception("Can't load version 4 cache file without tags file. Please make sure that tags.dat is in the same folder at the map file.");
                    if (string_reader == null || tag_reader.BaseStream.Length == 0) throw new Exception("Can't load version 4 cache file without strings file. Please make sure that tags.dat is in the same folder at the map file.");

					// Load the tag names csv file
					string tagnames_filename = "tagnames_";
					if (engineInfo.AltTagNames != null)
						tagnames_filename += engineInfo.AltTagNames + ".csv";
					else
						tagnames_filename += version.BuildString + ".csv";
                    string tagnames_location = filesLocation != null ? filesLocation + tagnames_filename : "";
                    if (!File.Exists(tagnames_location)) tagnames_location = "tagnames\\" + tagnames_filename;
                    if (!File.Exists(tagnames_location)) tagnames_location = null;

                    FileStream tagnamesFileStream = tagnames_location != null ? TryInitFilestream(tagnames_location) : null;
                    EndianReader tagnames_reader = null;
                    if (tagnamesFileStream != null) 
                    {
                        tagnames_reader = new EndianReader(tagnamesFileStream, Endian.BigEndian);
                        tagnames_reader.Endianness = engianess;
                    }

                    tagnamesLocation = tagnames_location;

                    FourthGenCacheFile cache_file = new FourthGenCacheFile(map_reader, tag_reader, string_reader, tagnames_reader, engineInfo, version.BuildString);
                    tagnamesFileStream.Close();
                    return cache_file;

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