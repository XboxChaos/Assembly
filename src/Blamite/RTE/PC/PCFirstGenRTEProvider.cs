using Blamite.Blam;
using Blamite.IO;
using Blamite.RTE.PC.Native;
using Blamite.Serialization;
using System;
using System.Diagnostics;

namespace Blamite.RTE.PC
{
	public class PCFirstGenRTEProvider : PCRTEProvider
	{
		/// <summary>
		///     Constructs a new PCFirstGenRTEProvider.
		/// </summary>
		public PCFirstGenRTEProvider(EngineDescription engine) : base(engine)
		{
		}

		/// <summary>
		///     The type of connection that the provider will establish.
		/// </summary>
		public override RTEConnectionType ConnectionType
		{
			get { return _buildInfo.PokingPlatform; }
		}

		/// <summary>
		///     Obtains a stream which can be used to read and write a cache file's meta in realtime.
		///     The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to get a stream for.</param>
		/// <param name="tag">The tag to be poked; only needed for Eldorado.</param>
		/// <returns>The stream if it was opened successfully, or null otherwise.</returns>
		public override IStream GetCacheStream(ICacheFile cacheFile, ITag tag = null)
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

			if (!info.HeaderAddress.HasValue)
			{
				ErrorMessage = "First Generation poking requires a HeaderAddress value.";
				return null;
			}
			if (!info.MagicAddress.HasValue)
			{
				ErrorMessage = "First Generation poking requires a MagicAddress value.";
				return null;
			}

			ProcessMemoryStream gameMemory = new ProcessMemoryStream(gameProcess, gameModule);

			_baseAddress = (long)gameMemory.BaseModule.BaseAddress;
			_mapHeaderAddress = _baseAddress + info.HeaderAddress.Value;
			_mapMagicAddress = _baseAddress + info.MagicAddress.Value;

			var reader = new EndianReader(gameMemory, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
			ReadInformation(reader, _buildInfo);

			long memoryAddress = CurrentCacheAddress;
			if (CurrentMapName != cacheFile.InternalName)
			{
				gameMemory.Close();
				if (string.IsNullOrEmpty(CurrentMapName))
					ErrorMessage = "Tried to poke map \"" + cacheFile.InternalName + "\" but the game is not currently running any map." + GuessError;
				else
					ErrorMessage = "Tried to poke map \"" + cacheFile.InternalName + "\" but the game is currently in map \"" + CurrentMapName + "\"." + GuessError;
				return null;
			}

			if (memoryAddress == 0)
			{
				ErrorMessage = "Map file base memory address is reading as 0. Check your poking definition." + GuessError;
				return null;
			}

			OffsetStream gameStream = new OffsetStream(gameMemory, memoryAddress - cacheFile.MetaArea.BasePointer);
			return new EndianStream(gameStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
		}

		protected override void ReadMapPointers32(IReader reader)
		{
			reader.SeekTo(_mapMagicAddress);
			CurrentCacheAddress = reader.ReadUInt32();
		}

		protected override void ReadMapPointers64(IReader reader)
		{
			reader.SeekTo(_mapMagicAddress);
			CurrentCacheAddress = reader.ReadInt64();
		}
	}
}
