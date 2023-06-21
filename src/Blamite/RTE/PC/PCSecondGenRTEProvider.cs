using Blamite.Blam;
using Blamite.IO;
using Blamite.RTE.PC.Native;
using Blamite.Serialization;
using System;
using System.Diagnostics;
using System.IO;

namespace Blamite.RTE.PC
{
	public class PCSecondGenRTEProvider : RTEProvider
	{
		/// <summary>
		///     Constructs a new SecondGenRTEProvider.
		/// </summary>
		public PCSecondGenRTEProvider(EngineDescription engine) : base(engine)
		{
		}

		/// <summary>
		///     The type of connection that the provider will establish.
		/// </summary>
		public new RTEConnectionType ConnectionType
		{
			get { return _buildInfo.PokingPlatform; }
		}

		/// <summary>
		///     Obtains a stream which can be used to read and write a cache file's meta in realtime.
		///     The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to get a stream for.</param>
		/// <returns>The stream if it was opened successfully, or null otherwise.</returns>
		public override IStream GetCacheStream(ICacheFile cacheFile)
		{
			if (!CheckBuildInfo())
				return null; //ErrorMessage was handled by above.

			Process gameProcess = FindGameProcess();
			if (gameProcess == null)
				return null; //ErrorMessage was handled by above.

			ProcessModule gameModule = FindGameModule(gameProcess, out bool moduleError);
			if (moduleError)
				return null; //ErrorMessage was handled by above.

			PokingInformation info = RetrieveInformation(gameProcess, gameModule);
			if (info == null)
				return null; //ErrorMessage was handled by above.

			if (!info.HeaderAddress.HasValue && !info.HeaderPointer.HasValue)
			{
				ErrorMessage = "Second Generation poking requires a HeaderAddress valueor HeaderPointer value.";
				return null;
			}
			if (!info.MagicAddress.HasValue)
			{
				ErrorMessage = "Second Generation poking requires a MagicAddress value.";
				return null;
			}
			if (!info.SharedMagicAddress.HasValue)
			{
				ErrorMessage = "Second Generation poking requires a SharedMagicAddress value.";
				return null;
			}

			ProcessMemoryStream gameMemory = new ProcessMemoryStream(gameProcess, gameModule);
			SecondGenMapPointerReader mapInfo = new SecondGenMapPointerReader(gameMemory, _buildInfo, info);

			long memoryAddress;
			if (cacheFile.Type != CacheFileType.Shared)
			{
				memoryAddress = mapInfo.CurrentCacheAddress;

				// The map isn't shared, so make sure the map names match
				if (mapInfo.MapName != cacheFile.InternalName)
				{
					gameMemory.Close();
					ErrorMessage = "Tried to poke map \"" + cacheFile.InternalName + "\" but the game is currently in map \"" + mapInfo.MapName + "\"." + GuessError;
					return null;
				}
			}
			else
			{
				memoryAddress = mapInfo.SharedCacheAddress;

				// Make sure the shared and current map pointers are different,
				// or that the current map is the shared map
				if (mapInfo.MapType != CacheFileType.Shared && mapInfo.CurrentCacheAddress == mapInfo.SharedCacheAddress)
				{
					gameMemory.Close();
					if (string.IsNullOrEmpty(mapInfo.MapName))
						ErrorMessage = "Tried to poke map \"" + cacheFile.InternalName + "\" but the game is not currently running any map." + GuessError;
					else
						ErrorMessage = "Tried to poke map \"" + cacheFile.InternalName + "\" but the game is currently in map \"" + mapInfo.MapName + "\"." + GuessError;
					return null;
				}
			}

			if (memoryAddress == 0)
			{
				ErrorMessage = "Map file base memory address is reading as 0. Check your poking definition." + GuessError;
				return null;
			}

			OffsetStream gameStream = new OffsetStream(gameMemory, memoryAddress - cacheFile.MetaArea.BasePointer);
			return new EndianStream(gameStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
		}
	}
}
