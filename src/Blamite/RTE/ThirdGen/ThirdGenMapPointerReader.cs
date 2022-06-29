using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;
using Blamite.Serialization;
using Blamite.Util;
using System;
using System.IO;

namespace Blamite.RTE.ThirdGen
{
	public class ThirdGenMapPointerReader : MapPointerReader
	{
		public ThirdGenMapPointerReader(ProcessMemoryStream processStream, EngineDescription engineInfo, PokingInformation info)
		{
			_baseAddress = (long)processStream.BaseProcess.MainModule.BaseAddress;
			GetLayoutConstants(engineInfo);

			var reader = new EndianReader(processStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);

			if (info.HeaderPointer.HasValue)
			{
				reader.SeekTo(_baseAddress + info.HeaderPointer.Value);
				long address = reader.ReadUInt32();
				_mapHeaderAddress = address + 0x8;
				_mapMagicAddress = address + engineInfo.HeaderSize + info.MagicOffset.Value;
			}
			else
			{
				_mapHeaderAddress = _baseAddress + info.HeaderAddress.Value;
				_mapMagicAddress = _baseAddress + info.MagicAddress.Value;
			}

			ReadMapPointers32(reader);
			ReadMapHeader(reader);
			ProcessMapHeader();
		}

		public ThirdGenMapPointerReader(ProcessModuleMemoryStream moduleStream, EngineDescription engineInfo, PokingInformation info)
		{
			_baseAddress = (long)moduleStream.BaseModule.BaseAddress;
			GetLayoutConstants(engineInfo);

			var reader = new EndianReader(moduleStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);

			if (info.HeaderPointer.HasValue)
			{
				reader.SeekTo(_baseAddress + info.HeaderPointer.Value);
				long address = reader.ReadInt64();
				_mapHeaderAddress = address + 0x10;
				_mapMagicAddress = address + engineInfo.HeaderSize + info.MagicOffset.Value;
			}
			else
			{
				_mapHeaderAddress = _baseAddress + info.HeaderAddress.Value;
				_mapMagicAddress = _baseAddress + info.MagicAddress.Value;
			}

			ReadMapPointers64(reader);
			ReadMapHeader(reader);
			ProcessMapHeader();
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
