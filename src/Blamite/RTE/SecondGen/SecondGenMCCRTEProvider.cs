using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;
using Blamite.Serialization;
using System;
using System.Diagnostics;

namespace Blamite.RTE.SecondGen
{
	public class SecondGenMCCRTEProvider : MCCRTEProvider
	{
		/// <summary>
		///     Constructs a new SecondGenMCCRTEProvider.
		/// </summary>
		public SecondGenMCCRTEProvider(EngineDescription engine) : base(engine)
		{ }

		/// <summary>
		///     Obtains a stream which can be used to read and write a cache file's meta in realtime.
		///     The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to get a stream for.</param>
		/// <returns>The stream if it was opened successfully, or null otherwise.</returns>
		public override IStream GetMetaStream(ICacheFile cacheFile)
		{
			if (!CheckBuildInfo())
				return null;

			Process gameProcess = FindGameProcess();
			if (gameProcess == null)
				return null;

			PokingInformation info = RetrieveInformation(gameProcess);

			if (!info.HeaderAddress.HasValue && !info.HeaderPointer.HasValue)
				throw new NotImplementedException("Second Generation poking requires a HeaderAddress value or HeaderPointer value.");
			if (!info.MagicAddress.HasValue)
				throw new NotImplementedException("Second Generation poking requires a MagicAddress value.");
			if (!info.SharedMagicAddress.HasValue)
				throw new NotImplementedException("Second Generation poking requires a SharedMagicAddress value.");

			var gameMemory = new ProcessModuleMemoryStream(gameProcess, _buildInfo.PokingModule);
			var mapInfo = new SecondGenMapPointerReader(gameMemory, _buildInfo, info);

			long metaAddress;
			if (cacheFile.Type != CacheFileType.Shared)
			{
				metaAddress = mapInfo.CurrentCacheAddress;

				// The map isn't shared, so make sure the map names match
				if (mapInfo.MapName != cacheFile.InternalName)
				{
					gameMemory.Close();
					return null;
				}
			}
			else
			{
				metaAddress = mapInfo.SharedCacheAddress;

				// Make sure the shared and current map pointers are different,
				// or that the current map is the shared map
				if (mapInfo.MapType != CacheFileType.Shared && mapInfo.CurrentCacheAddress == mapInfo.SharedCacheAddress)
				{
					gameMemory.Close();
					return null;
				}
			}

			var metaStream = new OffsetStream(gameMemory, metaAddress - cacheFile.MetaArea.BasePointer);
			return new EndianStream(metaStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
		}
	}
}
