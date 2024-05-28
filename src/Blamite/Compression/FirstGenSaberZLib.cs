using Blamite.IO;
using Blamite.Serialization;
using Blamite.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Blamite.Compression
{
	public static class FirstGenSaberZLib
	{
		private static readonly int _chunkSize = 0x20000;
		private static readonly int _chunkMask = 0x1FFFF;

		public static CompressionState AnalyzeCache(IReader reader, EngineDescription engineInfo, out StructureValueCollection headerValues)
		{
			reader.SeekTo(0);
			var headerLayout = engineInfo.Layouts.GetLayout("header");
			headerValues = StructureReader.ReadStructure(reader, headerLayout);

			var metaOffset = (int)headerValues.GetInteger("meta offset");

			if (metaOffset >= reader.Length)
				return CompressionState.Compressed;

			reader.SeekTo(metaOffset);
			StructureValueCollection tagTableValues = StructureReader.ReadStructure(reader, engineInfo.Layouts.GetLayout("meta header"));

			if ((uint)tagTableValues.GetInteger("magic") != CharConstant.FromString("tags"))
				return CompressionState.Compressed;

			return CompressionState.Decompressed;
		}

		public static void CompressCache(string cacheFile)
		{
			using (MemoryStream msOutput = new MemoryStream())
			{
				using (EndianWriter ewOutput = new EndianWriter(msOutput, Endian.LittleEndian))
				{
					using (FileStream fsInput = new FileStream(cacheFile, FileMode.Open))
					{
						List<int> chunk_offsets = new List<int>();

						int datalength = (int)fsInput.Length;
						int chunkcount = ((datalength + _chunkMask) & ~_chunkMask) / _chunkSize;

						ewOutput.WriteInt32(chunkcount);

						int datastart = 0x40000;
						msOutput.Position = datastart;

						for (int i = 0; i < chunkcount; i++)
						{
							chunk_offsets.Add((int)msOutput.Position);
							int size = _chunkSize;
							if (i == chunkcount - 1)
								size = (int)fsInput.Length % _chunkSize;

							ewOutput.WriteInt32(size);
							ewOutput.WriteInt16(5416);

							using (DeflateStream ds = new DeflateStream(msOutput, CompressionLevel.Fastest, true))
							{
								byte[] chunkData = new byte[size];
								fsInput.Read(chunkData, 0, size);
								ds.Write(chunkData, 0, chunkData.Length);
							}
						}

						msOutput.Position = 0x4;
						for (int i = 0; i < chunkcount; i++)
							ewOutput.WriteInt32(chunk_offsets[i]);
					}
				}

				File.WriteAllBytes(cacheFile, msOutput.ToArray());
			}
		}

		public static void DecompressCache(string cacheFile)
		{
			using (MemoryStream msOutput = new MemoryStream())
			{
				using (FileStream fsInput = new FileStream(cacheFile, FileMode.Open))
				{
					using (EndianReader erInput = new EndianReader(fsInput, Endian.LittleEndian))
					{
						int chunk_count = erInput.ReadInt32();

						List<int> chunk_offsets = new List<int>();

						for (int i = 0; i < chunk_count; i++)
						{
							int offset = erInput.ReadInt32();

							if (offset >= fsInput.Length)
								throw new ArgumentException("Chunk " + i + " has an offset past the end of the file.");

							chunk_offsets.Add(offset);
						}

						for (int i = 0; i < chunk_offsets.Count; i++)
						{
							fsInput.Seek(chunk_offsets[i], SeekOrigin.Begin);

							int realsize = erInput.ReadInt32();

							fsInput.Seek(chunk_offsets[i] + 6, SeekOrigin.Begin);

							byte[] chunkData = new byte[realsize];

							using (DeflateStream ds = new DeflateStream(fsInput, CompressionMode.Decompress, true))
							{
								ds.Read(chunkData, 0, chunkData.Length);
							}

							msOutput.Write(chunkData, 0, realsize);
						}
					}
				}

				File.WriteAllBytes(cacheFile, msOutput.ToArray());
			}
		}
	}
}
