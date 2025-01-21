using Blamite.Blam;
using Blamite.IO;
using Blamite.Serialization;
using Blamite.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Blamite.Compression
{
	public static class SecondGenSaberZLib
	{
		private static readonly int _chunkSize = 0x40000;
		private static readonly int _chunkMask = 0x3FFFF;

		public static CompressionState AnalyzeCache(IReader reader, EngineDescription engineInfo, out StructureValueCollection headerValues)
		{
			var segmenter = new FileSegmenter(engineInfo.SegmentAlignment);
			reader.SeekTo(0);
			var headerLayout = engineInfo.Layouts.GetLayout("header");
			headerValues = StructureReader.ReadStructure(reader, headerLayout);

			uint metaOffset = (uint)headerValues.GetInteger("meta offset");
			var metaSize = (uint)headerValues.GetInteger("meta size");
			uint metaOffsetMask = (uint)headerValues.GetInteger("meta offset mask");

			var metaSegment = new FileSegment(
				segmenter.DefineSegment(metaOffset, metaSize, 0x200, SegmentResizeOrigin.Beginning), segmenter);
			var MetaArea = new FileSegmentGroup(new MetaOffsetConverter(metaSegment, metaOffsetMask));
			MetaArea.AddSegment(metaSegment);

			if (MetaArea.Offset >= reader.Length)
				return CompressionState.Compressed;

			reader.SeekTo(MetaArea.Offset);
			StructureValueCollection tagTableValues = StructureReader.ReadStructure(reader, engineInfo.Layouts.GetLayout("meta header"));

			if ((uint)tagTableValues.GetInteger("magic") != CharConstant.FromString("tags"))
				return CompressionState.Compressed;

			return CompressionState.Decompressed;
		}

		public static void CompressCache(string cacheFile, StructureValueCollection headerValues, StructureLayout headerLayout)
		{
			using (MemoryStream msOutput = new MemoryStream())
			{
				using (EndianWriter ewOutput = new EndianWriter(msOutput, Endian.LittleEndian))
				{
					using (FileStream fsInput = new FileStream(cacheFile, FileMode.Open))
					{
						List<FileChunk> Chunks = new List<FileChunk>();

						int headerSize = (int)headerLayout.Size;
						bool newFormat = headerValues.HasInteger("compression data chunk size");
						uint dataStart = newFormat ? (uint)headerSize : 0x3000;

						//header is uncompressed
						byte[] header = new byte[headerSize];
						fsInput.Read(header, 0, headerSize);
						msOutput.Write(header, 0, headerSize);

						int datalength = (int)fsInput.Length - headerSize;
						int chunkcount = ((datalength + _chunkMask) & ~_chunkMask) / _chunkSize;

						msOutput.Position = dataStart;

						while (fsInput.Position < fsInput.Length)
						{
							int size = _chunkSize;
							if (fsInput.Length - fsInput.Position < size)
								size = datalength % _chunkSize;

							int start = (int)msOutput.Position;

							ewOutput.WriteInt16(5416);

							using (DeflateStream ds = new DeflateStream(msOutput, CompressionMode.Compress, true))
							{
								byte[] chunkData = new byte[size];
								fsInput.Read(chunkData, 0, size);
								ds.Write(chunkData, 0, chunkData.Length);
							}

							int complength = (int)msOutput.Position - start;
							Chunks.Add(new FileChunk(complength, start));

							//each chunk is padded
							long remainder = complength % 0x80;
							msOutput.Seek(0x80 - remainder, SeekOrigin.Current);
						}

						uint tableOffset = newFormat ? (uint)msOutput.Position : (uint)headerSize;
						int tableSize = chunkcount * 8;

						msOutput.Position = tableOffset;

						for (int i = 0; i < chunkcount; i++)
						{
							ewOutput.WriteInt32(Chunks[i].Size);
							ewOutput.WriteInt32(Chunks[i].Offset);
						}

						if (newFormat)
						{
							//table is padded in the new format
							long remainder = tableSize % 0x80;
							msOutput.Seek(0x80 - remainder - 1, SeekOrigin.Current);
							ewOutput.WriteByte(0);

							headerValues.SetInteger("compression data chunk size", (ulong)_chunkSize);
							headerValues.SetInteger("compression data offset", (ulong)headerSize);
							headerValues.SetInteger("compression chunk table offset", tableOffset);
							headerValues.SetInteger("compression chunk table count", (ulong)chunkcount);

							short flags = (short)headerValues.GetInteger("flags");
							flags |= 1;
							headerValues.SetInteger("flags", (ulong)chunkcount);

							ewOutput.SeekTo(0);
							StructureWriter.WriteStructure(headerValues, headerLayout, ewOutput);
						}
					}
				}

				File.WriteAllBytes(cacheFile, msOutput.ToArray());
			}
		}

		public static void DecompressCache(string cacheFile, StructureValueCollection headerValues, StructureLayout headerLayout)
		{
			using (MemoryStream msOutput = new MemoryStream())
			{
				using (FileStream fsInput = new FileStream(cacheFile, FileMode.Open))
				{
					using (EndianReader erInput = new EndianReader(fsInput, Endian.LittleEndian))
					{
						int headerSize = (int)headerLayout.Size;
						int chunkSize = headerValues.HasInteger("compression data chunk size") ?
							(int)headerValues.GetInteger("compression data chunk size") : _chunkSize;

						uint chunkTableOffset = headerValues.HasInteger("compression chunk table offset") ?
							(uint)headerValues.GetInteger("compression chunk table offset") : (uint)headerSize;

						int chunkCount = headerValues.HasInteger("compression chunk table count") ?
							(int)headerValues.GetInteger("compression chunk table count") : 0x400;

						//header is uncompressed
						msOutput.Write(erInput.ReadBlock(headerSize), 0, headerSize);

						List<FileChunk> Chunks = new List<FileChunk>();

						fsInput.Seek(chunkTableOffset, SeekOrigin.Begin);

						for (int i = 0; i < chunkCount; i++)
						{
							int csize = erInput.ReadInt32();
							int offset = erInput.ReadInt32();

							if (csize == 0)
								break;

							if (offset >= fsInput.Length)
								throw new ArgumentException("Chunk " + i + " has an offset past the end of the file.");

							Chunks.Add(new FileChunk(csize, offset));
						}

						for (int i = 0; i < Chunks.Count; i++)
						{
							//check for faux-compression some other tools use
							if (Chunks[i].Size < 0)
							{
								int invertedSize = -Chunks[i].Size;
								byte[] aaa = new byte[invertedSize];

								fsInput.Seek(Chunks[i].Offset, SeekOrigin.Begin);

								int readSize = fsInput.Read(aaa, 0, invertedSize);

								msOutput.Write(aaa, 0, readSize);
							}
							else
							{
								fsInput.Seek(Chunks[i].Offset + 2, SeekOrigin.Begin);

								int realsize = chunkSize;
								byte[] chunkData = new byte[realsize];

								using (DeflateStream ds = new DeflateStream(fsInput, CompressionMode.Decompress, true))
								{
									realsize = ds.Read(chunkData, 0, chunkData.Length);
								}

								msOutput.Write(chunkData, 0, realsize);
							}
						}

						//clear compressed bit so it can run ingame
						if (headerValues.HasInteger("flags"))
						{
							int flagOffset = (int)headerLayout.GetFieldOffset("flags");
							fsInput.Seek(flagOffset, SeekOrigin.Begin);
							int flags = erInput.ReadInt16();
							flags &= 0x7FFFFFFE;

							msOutput.Seek(flagOffset, SeekOrigin.Begin);
							msOutput.Write(BitConverter.GetBytes((short)flags), 0, 2);
						}
					}
				}

				File.WriteAllBytes(cacheFile, msOutput.ToArray());
			}
		}

		private class FileChunk
		{
			public int Size { get; set; }
			public int Offset { get; set; }

			public FileChunk(int size, int offset)
			{
				Size = size;
				Offset = offset;
			}
		}

	}


}
