using System;
using System.IO;
using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;
using Blamite.Util;

namespace Blamite.RTE.H2Vista
{
	/// <summary>
	///     Reads map information out of Halo 2 Vista's memory.
	/// </summary>
	public class H2VistaMapPointerReader
	{
		private readonly long _baseAddress;
		private long _mapHeaderAddress;

		public H2VistaMapPointerReader(ProcessMemoryStream stream)
		{
			// Get the base address of the process's main module
			// All addresses have to be based off of this because the game
			// randomizes its address space
			_baseAddress = (long) stream.BaseProcess.MainModule.BaseAddress;

			var reader = new EndianReader(stream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
			FindMapHeader(reader);
			ReadMapPointers(reader);
			ReadMapHeader(reader);
			ProcessMapHeader();
		}

		/// <summary>
		///     Gets the address of the meta for the currently-loaded (non-shared) map.
		/// </summary>
		public long CurrentMetaAddress { get; private set; }

		/// <summary>
		///     Gets the address of the meta for the currently-loaded shared map.
		///     If no shared map is loaded, this will be the same as <see cref="CurrentMetaAddress" />.
		/// </summary>
		public long SharedMetaAddress { get; private set; }

		/// <summary>
		///     Gets the type of the map that is currently loaded.
		/// </summary>
		public CacheFileType MapType { get; private set; }

		/// <summary>
		///     Gets the name of the map that is currently loaded.
		/// </summary>
		public string MapName { get; private set; }

		/// <summary>
		///     Gets the scenario name of the map that is currently loaded.
		/// </summary>
		public string ScenarioName { get; private set; }

		/// <summary>
		///     Gets the full header of the map that is currently loaded.
		/// </summary>
		public byte[] MapHeader { get; private set; }

		private void FindMapHeader(IReader reader)
		{
			if (TryHeaderAddress(reader, Patch2MapHeaderRelativeAddress))
				return;
			if (TryHeaderAddress(reader, Patch1MapHeaderRelativeAddress))
				return;
			if (TryHeaderAddress(reader, NoPatchMapHeaderRelativeAddress))
				return;

			throw new InvalidOperationException("Unable to locate the map header in the game's memory");
		}

		private bool TryHeaderAddress(IReader reader, long address)
		{
			reader.SeekTo(_baseAddress + address);
			if (reader.ReadUInt32() == MapHeaderMagic)
			{
				_mapHeaderAddress = _baseAddress + address;
				return true;
			}
			return false;
		}

		private void ReadMapPointers(IReader reader)
		{
			// The shared meta pointer is immediately before the map header
			reader.SeekTo(_mapHeaderAddress - 4);
			SharedMetaAddress = reader.ReadUInt32();

			// The current meta pointer is immediately after the main header
			reader.SeekTo(_mapHeaderAddress + MapHeaderSize);
			CurrentMetaAddress = reader.ReadUInt32();
		}

		private void ReadMapHeader(IReader reader)
		{
			reader.SeekTo(_mapHeaderAddress);
			MapHeader = reader.ReadBlock(MapHeaderSize);
		}

		private void ProcessMapHeader()
		{
			using (IReader reader = new EndianStream(new MemoryStream(MapHeader), Endian.LittleEndian))
			{
				reader.SeekTo(MapTypeOffset);
				MapType = (CacheFileType) reader.ReadInt32();

				reader.SeekTo(MapNameOffset);
				MapName = reader.ReadAscii();

				reader.SeekTo(ScenarioNameOffset);
				ScenarioName = reader.ReadAscii();
			}
		}

		#region Addresses

		// Note: all addresses are relative to the module base
		// because the game uses address space layout randomization (ASLR)

		// These addresses point to the beginning of the header of the currently-loaded map in halo2.exe
		// The header here is always 0x800 bytes long
		// The rest of the file does NOT come after this - it only contains the header
		private const long NoPatchMapHeaderRelativeAddress = 0x479B40;
		private const long Patch1MapHeaderRelativeAddress = 0x47CD58;
		private const long Patch2MapHeaderRelativeAddress = 0x47CD68;

		#endregion

		#region Map Header

		// The magic number that appears at the beginning of the map header

		// The size of the map header in bytes
		private const int MapHeaderSize = 0x800;

		// Offset (from the start of the map header) of the int32 indicating the map type
		private const int MapTypeOffset = 0x14C;

		// Offset (from the start of the map header) of the ASCII string indicating the map name
		private const int MapNameOffset = 0x1A4;

		// Offset (from the start of the map header) of the ASCII string indicating the scenario name
		private const int ScenarioNameOffset = 0x1C8;
		private static readonly int MapHeaderMagic = CharConstant.FromString("head");

		#endregion
	}
}