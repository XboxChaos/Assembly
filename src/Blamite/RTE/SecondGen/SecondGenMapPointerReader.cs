using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;
using Blamite.Serialization;
using Blamite.Util;
using System;
using System.IO;

namespace Blamite.RTE.SecondGen
{
	public class SecondGenMapPointerReader : MapPointerReader
	{
		private long _mapSharedMagicAddress;

		public SecondGenMapPointerReader(ProcessMemoryStream processStream, EngineDescription engineInfo, PokingInformation info)
		{
			_baseAddress = (long)processStream.BaseProcess.MainModule.BaseAddress;
			_mapHeaderAddress = _baseAddress + info.HeaderAddress.Value;
			_mapMagicAddress = _baseAddress + info.MagicAddress.Value;
			_mapSharedMagicAddress = _baseAddress + info.SharedMagicAddress.Value;

			GetLayoutConstants(engineInfo);

			var reader = new EndianReader(processStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
			ReadMapPointers32(reader);
			ReadMapHeader(reader);
			ProcessMapHeader();
		}

		public SecondGenMapPointerReader(ProcessModuleMemoryStream moduleStream, EngineDescription engineInfo, PokingInformation info)
		{
			_baseAddress = (long)moduleStream.BaseModule.BaseAddress;

			var reader = new EndianReader(moduleStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);

			if (info.HeaderPointer.HasValue)
			{
				reader.SeekTo(_baseAddress + info.HeaderPointer.Value);
				_mapHeaderAddress = reader.ReadInt64();
			}
			else
				_mapHeaderAddress = _baseAddress + info.HeaderAddress.Value;

			_mapMagicAddress = _baseAddress + info.MagicAddress.Value;
			_mapSharedMagicAddress = _baseAddress + info.SharedMagicAddress.Value;

			GetLayoutConstants(engineInfo);

			ReadMapPointers64(reader);
			ReadMapHeader(reader);
			ProcessMapHeader();
		}

		/// <summary>
		///     Gets the address of the cache for the currently-loaded shared map.
		///     If no shared map is loaded, this will be the same as <see cref="CurrentCacheAddress" />.
		/// </summary>
		public long SharedCacheAddress { get; private set; }

		protected override void ReadMapPointers32(IReader reader)
		{
			// The shared meta pointer is immediately before the map header
			reader.SeekTo(_mapSharedMagicAddress);
			SharedCacheAddress = reader.ReadUInt32();

			reader.SeekTo(_mapMagicAddress);
			CurrentCacheAddress = reader.ReadUInt32();
		}

		protected override void ReadMapPointers64(IReader reader)
		{
			// The shared meta pointer is immediately before the map header
			reader.SeekTo(_mapSharedMagicAddress);
			SharedCacheAddress = reader.ReadInt64();

			// The current meta pointer is immediately after the main header
			reader.SeekTo(_mapMagicAddress);
			CurrentCacheAddress = reader.ReadInt64();
		}
	}
}
