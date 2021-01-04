using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;
using Blamite.Serialization;
using Blamite.Util;
using System;
using System.IO;

namespace Blamite.RTE.FirstGen
{
	public class FirstGenMapPointerReader : MapPointerReader
	{

		public FirstGenMapPointerReader(ProcessMemoryStream stream, EngineDescription engineInfo, PokingInformation info)
		{
			_baseAddress = (long)stream.BaseProcess.MainModule.BaseAddress;
			_mapHeaderAddress = _baseAddress + info.HeaderAddress.Value;
			_mapMagicAddress = _baseAddress + info.MagicAddress.Value;

			GetLayoutConstants(engineInfo);

			var reader = new EndianReader(stream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
			ReadMapPointers32(reader);
			ReadMapHeader(reader);
			ProcessMapHeader();
		}

		public FirstGenMapPointerReader(ProcessModuleMemoryStream stream, EngineDescription engineInfo, PokingInformation info)
		{
			_baseAddress = (long)stream.BaseModule.BaseAddress;
			_mapHeaderAddress = _baseAddress + info.HeaderAddress.Value;
			_mapMagicAddress = _baseAddress + info.MagicAddress.Value;

			GetLayoutConstants(engineInfo);

			var reader = new EndianReader(stream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
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
