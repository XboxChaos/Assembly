using Blamite.Blam;
using Blamite.IO;
using Blamite.Serialization;
using Blamite.Util;
using System.IO;

namespace Blamite.RTE
{
	public abstract class MapPointerReader
	{
		protected long _baseAddress;
		protected long _mapHeaderAddress;
		protected long _mapMagicAddress;

		/// <summary>
		///     Gets the address of the cache for the currently-loaded (non-shared) map.
		/// </summary>
		public long CurrentCacheAddress { get; internal set; }

		/// <summary>
		///     Gets the type of the map that is currently loaded.
		/// </summary>
		public CacheFileType MapType { get; private set; }

		/// <summary>
		///     Gets the name of the map that is currently loaded.
		/// </summary>
		public string MapName { get; private set; }

		/// <summary>
		///     Gets the full header of the map that is currently loaded.
		/// </summary>
		public byte[] MapHeader { get; private set; }

		// The size of the map header in bytes
		protected int MapHeaderSize;

		// Offset (from the start of the map header) of the int32 indicating the map type
		protected int MapTypeOffset;

		// Offset (from the start of the map header) of the ASCII string indicating the map name
		protected int MapNameOffset;

		protected void GetLayoutConstants(EngineDescription engineInfo)
		{
			MapHeaderSize = engineInfo.HeaderSize;

			var layout = engineInfo.Layouts.GetLayout("header");
			MapTypeOffset = layout.GetFieldOffset("type");
			MapNameOffset = layout.GetFieldOffset("internal name");
		}

		protected void ReadInformation(EndianReader reader, EngineDescription engineInfo)
		{
			GetLayoutConstants(engineInfo);

			if (engineInfo.PokingPlatform == RTEConnectionType.LocalProcess32)
				ReadMapPointers32(reader);
			else
				ReadMapPointers64(reader);

			ReadMapHeader(reader);
			ProcessMapHeader();
		}

		protected abstract void ReadMapPointers32(IReader reader);
		protected abstract void ReadMapPointers64(IReader reader);

		protected void ReadMapHeader(IReader reader)
		{
			reader.SeekTo(_mapHeaderAddress);
			MapHeader = reader.ReadBlock(MapHeaderSize);
		}

		protected void ProcessMapHeader()
		{
			using (IReader reader = new EndianStream(new MemoryStream(MapHeader), Endian.LittleEndian))
			{
				reader.SeekTo(MapTypeOffset);
				MapType = (CacheFileType)reader.ReadInt32();

				reader.SeekTo(MapNameOffset);
				MapName = reader.ReadAscii();

			}
		}

		protected static readonly int MapHeaderMagic = CharConstant.FromString("head");
	}
}
