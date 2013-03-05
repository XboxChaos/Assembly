using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.SecondGen;
using Blamite.Blam.ThirdGen;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam
{
    /// <summary>
    /// Provides methods for loading cache files.
    /// </summary>
    public static class CacheFileLoader
    {
        /// <summary>
        /// Loads a cache file from a stream.
        /// </summary>
        /// <param name="reader">The stream to read from.</param>
        /// <param name="infoLoader">The BuildInfoLoader responsible for loading build information for the cache file.</param>
        /// <returns>The cache file that was loaded.</returns>
        /// <exception cref="ArgumentException">Thrown if the cache file is invalid.</exception>
        /// <exception cref="NotSupportedException">Thrown if the cache file's target engine is not supported.</exception>
        public static ICacheFile LoadCacheFile(IReader reader, BuildInfoLoader infoLoader)
        {
            BuildInformation tempInfo;
            return LoadCacheFile(reader, infoLoader, out tempInfo);
        }

        /// <summary>
        /// Loads a cache file from a stream.
        /// </summary>
        /// <param name="reader">The stream to read from.</param>
        /// <param name="infoLoader">The BuildInfoLoader responsible for loading build information for the cache file.</param>
        /// <param name="buildInfo">The variable to store build information to.</param>
        /// <returns>The cache file that was loaded.</returns>
        /// <exception cref="ArgumentException">Thrown if the cache file is invalid.</exception>
        /// <exception cref="NotSupportedException">Thrown if the cache file's target engine is not supported.</exception>
        public static ICacheFile LoadCacheFile(IReader reader, BuildInfoLoader infoLoader, out BuildInformation buildInfo)
        {
            // Set the reader's endianness based upon the file's header magic
            reader.SeekTo(0);
            byte[] headerMagic = reader.ReadBlock(4);
            reader.Endianness = DetermineCacheFileEndianness(headerMagic);

            // Load engine version info
            CacheFileVersionInfo version = new CacheFileVersionInfo(reader);
            if (version.Engine != EngineType.SecondGeneration && version.Engine != EngineType.ThirdGeneration)
                throw new NotSupportedException("Engine not supported");

            // Load build info
            buildInfo = infoLoader.LoadBuild(version.BuildString);
            if (buildInfo == null)
                throw new NotSupportedException("Engine version \"" + version.BuildString + "\" not supported");

            // Load the cache file depending upon the engine version
            switch (version.Engine)
            {
                case EngineType.SecondGeneration:
                    return new SecondGenCacheFile(reader, buildInfo, version.BuildString);

                case EngineType.ThirdGeneration:
                    return new ThirdGenCacheFile(reader, buildInfo, version.BuildString);

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
