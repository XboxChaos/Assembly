using Blamite.IO;
using Blamite.RTE.PC.Native;
using Blamite.Serialization;
using System;

namespace Blamite.RTE.PC
{
	public class ThirdGenMapPointerReader : BaseMapPointerReader
	{
		public ThirdGenMapPointerReader(ProcessMemoryStream processStream, EngineDescription engineInfo, PokingInformation info)
		{
			_baseAddress = (long)processStream.BaseModule.BaseAddress;

			var reader = new EndianReader(processStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);

			if (info.HeaderPointer.HasValue)
			{
				reader.SeekTo(_baseAddress + info.HeaderPointer.Value);

				long address;
				if (engineInfo.PokingPlatform == RTEConnectionType.LocalProcess32)
				{
					address = reader.ReadUInt32();
					_mapHeaderAddress = address + 0x8;
				}
				else
				{
					address = reader.ReadInt64();
					_mapHeaderAddress = address + 0x10;
				}

				_mapMagicAddress = address + engineInfo.HeaderSize + info.MagicOffset.Value;
			}
			else
			{
				_mapHeaderAddress = _baseAddress + info.HeaderAddress.Value;
				_mapMagicAddress = _baseAddress + info.MagicAddress.Value;
			}

			ReadInformation(reader, engineInfo);
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
