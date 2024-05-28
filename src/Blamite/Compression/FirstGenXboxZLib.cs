using Blamite.IO;
using Blamite.Serialization;
using Blamite.Util;
using System;
using System.IO;
using System.IO.Compression;

namespace Blamite.Compression
{
	public static class FirstGenXboxZLib
	{
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

		public static void CompressCache(string cacheFile, int headerSize)
		{
			string tempFile = Path.GetTempFileName();

			using (FileStream fsOutput = new FileStream(tempFile, FileMode.OpenOrCreate))
			{
				using (FileStream fsInput = new FileStream(cacheFile, FileMode.Open))
				{
					using (EndianReader erInput = new EndianReader(fsInput, Endian.LittleEndian))
					{
						//header is uncompressed
						fsOutput.Write(erInput.ReadBlock(headerSize), 0, headerSize);

						int realsize = (int)fsInput.Length - headerSize;
						byte[] chunkData = new byte[realsize];

						fsInput.Seek(0x800, SeekOrigin.Begin);
						fsInput.Read(chunkData, 0, realsize);

						fsOutput.WriteByte(0x78);
						fsOutput.WriteByte(0x9C);

						using (DeflateStream ds = new DeflateStream(fsOutput, CompressionMode.Compress, true))
						{
							realsize = fsInput.Read(chunkData, 0, chunkData.Length);
							ds.Write(chunkData, 0, chunkData.Length);
						}

						// NOTE: actual zlib has an adler-32 checksum trailer on the end
						uint adler = Adler32.Calculate(chunkData);
						fsOutput.Write(BitConverter.GetBytes(adler), 0, 4);

						// CE xbox has some padding on the end to a 0x800 alignment
						int pad_size = 0x800 - ((int)fsOutput.Length % 0x800);
						byte[] padding = new byte[pad_size];
						fsOutput.Write(padding, 0, pad_size);
					}
				}
			}

			File.Copy(tempFile, cacheFile, true);
			File.Delete(tempFile);
		}

		public static void DecompressCache(string cacheFile, int headerSize, int mapsize)
		{
			string tempFile = Path.GetTempFileName();

			using (FileStream fsOutput = new FileStream(tempFile, FileMode.OpenOrCreate))
			{
				using (FileStream fsInput = new FileStream(cacheFile, FileMode.Open))
				{
					using (EndianReader erInput = new EndianReader(fsInput, Endian.LittleEndian))
					{
						int fileSize = mapsize - headerSize;

						//header is uncompressed
						fsOutput.Write(erInput.ReadBlock(headerSize), 0, headerSize);

						byte[] chunkData = new byte[fileSize];

						fsInput.Seek(headerSize + 2, SeekOrigin.Begin);
						using (DeflateStream ds = new DeflateStream(fsInput, CompressionMode.Decompress, true))
						{
							fileSize = ds.Read(chunkData, 0, chunkData.Length);
						}
						fsOutput.Write(chunkData, 0, fileSize);
					}
				}
			}

			File.Copy(tempFile, cacheFile, true);
			File.Delete(tempFile);
		}
	}
}
