using Blamite.IO;
using Blamite.Serialization;
using SevenZip.Compression.LZMA;
using System;
using System.IO;

namespace Blamite.Compression
{
	public static class ThirdGenDurangoLZMA
	{
		public static CompressionState AnalyzeCache(IReader reader, EngineDescription engineInfo, out StructureValueCollection headerValues)
		{
			var segmenter = new FileSegmenter(engineInfo.SegmentAlignment);
			reader.SeekTo(0);
			var headerLayout = engineInfo.Layouts.GetLayout("header");
			headerValues = StructureReader.ReadStructure(reader, headerLayout);

			if (headerValues.HasArray("compressed section sizes"))
			{
				foreach (var size in headerValues.GetArray("compressed section sizes"))
				{
					int sizeval = (int)size.GetInteger("compressed size");
					if (sizeval > 0)
						return CompressionState.Compressed;
				}
			}

			return CompressionState.Decompressed;
		}

		public static void CompressCache(string cacheFile, StructureValueCollection headerValues, StructureLayout headerLayout)
		{
			//could probably support this but ehhhh. @ me when we can actually run maps and the decompressed maps dont "just work".
		}

		public static void DecompressCache(string cacheFile, StructureValueCollection headerValues, StructureLayout headerLayout, int align)
		{
			int headerSize = (int)headerLayout.Size;
			var masks = headerValues.GetArray("offset masks");
			var sections = headerValues.GetArray("sections");
			var offsets = headerValues.GetArray("compressed section offsets");
			var sizes = headerValues.GetArray("compressed section sizes");
			var types = headerValues.GetArray("compressed section types");

			uint[] newMasks = new uint[4];

			using (MemoryStream msOutput = new MemoryStream())
			{
				using (EndianWriter ewOutput = new EndianWriter(msOutput, Endian.LittleEndian))
				{
					using (FileStream fsInput = new FileStream(cacheFile, FileMode.Open))
					{
						using (EndianReader erInput = new EndianReader(fsInput, Endian.LittleEndian))
						{
							//header is uncompressed
							msOutput.Write(erInput.ReadBlock(headerSize), 0, headerSize);
							int[] order = new int[4] { 0, 1, 3, 2 };

							for (int i = 0; i < 4; i++)
							{
								int ind = order[i];

								uint offset = (uint)offsets[ind].GetInteger("offset");
								int compressedSize = (int)sizes[ind].GetInteger("compressed size");
								byte type = (byte)types[ind].GetInteger("type");
								int decompressedSize = (int)sections[ind].GetInteger("size");
								uint addr = (uint)sections[ind].GetInteger("virtual address");
								uint mask = (uint)masks[ind].GetInteger("mask");
								uint finalOffset = addr + mask;

								uint pad = Align((uint)msOutput.Position, (uint)align);
								ewOutput.SeekTo(pad);

								newMasks[ind] = pad - addr;

								if (type == 0)
								{
									erInput.SeekTo(offset);
									msOutput.Write(erInput.ReadBlock(decompressedSize), 0, decompressedSize);
								}
								else if (type == 3)
								{
									Decoder lzmaDecoder = new Decoder();
									erInput.SeekTo(offset + 1);

									int unpackedHeader = UnpackHeader(erInput.ReadByte());
									byte[] lzmaHeadBytes = BitConverter.GetBytes(unpackedHeader);

									using (MemoryStream compms = new MemoryStream())
									{
										compms.Write(new byte[1] { 0x5D }, 0, 1);
										compms.Write(lzmaHeadBytes, 0, lzmaHeadBytes.Length);
										compms.Write(erInput.ReadBlock(compressedSize - 2), 0, compressedSize - 2);
										compms.Seek(0, SeekOrigin.Begin);

										byte[] props = new byte[5];
										compms.Read(props, 0, 5);

										lzmaDecoder.SetDecoderProperties(props);

										lzmaDecoder.Code(compms, msOutput, compressedSize, decompressedSize, null);
									}
								}
								else
									throw new ArgumentException($"Unknown MCC compression type {type} found. Please report this with the build version/map name.");
							}

							//fix up some header values
							ewOutput.SeekTo(headerLayout.GetFieldOffset("offset masks"));
							for (int i = 0; i < 4; i++)
								ewOutput.WriteUInt32(newMasks[i]);

							ewOutput.SeekTo(headerLayout.GetFieldOffset("compressed section sizes"));
							ewOutput.WriteBlock(new byte[0x10]);
							ewOutput.SeekTo(headerLayout.GetFieldOffset("compressed section types"));
							ewOutput.WriteBlock(new byte[0x4]);
						}
					}
				}

				File.WriteAllBytes(cacheFile, msOutput.ToArray());
			}
		}

		private static uint Align(uint val, uint alignment)
		{
			return (val + alignment - 1) & ~(alignment - 1);
		}

		private static int UnpackHeader(byte value)
		{
			int shift = ((value >> 1) & 0x1F) + 10;
			int baseVal = (value & 1 << 1) + 1;
			return baseVal << shift;
		}

	}
}
