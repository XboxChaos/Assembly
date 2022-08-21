using Blamite.Blam;
using Blamite.IO;
using Blamite.RTE.PC.Native;
using Blamite.Serialization;
using System;
using System.Diagnostics;
using System.IO;

namespace Blamite.RTE.PC
{
	public class PCThirdGenRTEProvider : BaseRTEProvider
	{
		/// <summary>
		///     Constructs a new ThirdGenRTEProvider.
		/// </summary>
		public PCThirdGenRTEProvider(EngineDescription engine) : base(engine)
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
		public override IStream GetCacheStream(ICacheFile cacheFile = null)
		{
			if (!CheckBuildInfo())
				return null; //ErrorMessage was handled by above.

			Process gameProcess = FindGameProcess();
			if (gameProcess == null)
				return null; //ErrorMessage was handled by above.

			PokingInformation info = RetrieveInformation(gameProcess);
			if (info == null)
				return null; //ErrorMessage was handled by above.

			if ((!info.HeaderPointer.HasValue || !info.MagicOffset.HasValue) && (!info.HeaderAddress.HasValue || !info.MagicAddress.HasValue))
			{
				ErrorMessage = "Third Generation poking requires either HeaderAddress and MagicAddress values (Halo 3, ODST), or HeaderPointer and MagicOffset values (Reach and later).";
				return null;
			}

			Stream gameMemory;
			ThirdGenMapPointerReader mapInfo;

			if (!string.IsNullOrEmpty(_buildInfo.PokingModule))
			{
				gameMemory = new ProcessModuleMemoryStream(gameProcess, _buildInfo.PokingModule);
				mapInfo = new ThirdGenMapPointerReader((ProcessModuleMemoryStream)gameMemory, _buildInfo, info);
			}
			else
			{
				gameMemory = new ProcessMemoryStream(gameProcess);
				mapInfo = new ThirdGenMapPointerReader((ProcessMemoryStream)gameMemory, _buildInfo, info);
			}

			long memoryAddress = mapInfo.CurrentCacheAddress;
			if (cacheFile != null && mapInfo.MapName != cacheFile.InternalName)
			{
				gameMemory.Close();
				if (string.IsNullOrEmpty(mapInfo.MapName))
					ErrorMessage = "Tried to poke map \"" + cacheFile.InternalName + "\" but the game is not currently running any map." + GuessError;
				else
					ErrorMessage = "Tried to poke map \"" + cacheFile.InternalName + "\" but the game is currently in map \"" + mapInfo.MapName + "\"." + GuessError;
				return null;
			}

			if (memoryAddress == 0)
			{
				ErrorMessage = "Map file base memory address is reading as 0. Check your poking definition." + GuessError;
				return null;
			}

			OffsetStream gameStream = new OffsetStream(gameMemory, memoryAddress);
			return new EndianStream(gameStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
		}
	}
}
