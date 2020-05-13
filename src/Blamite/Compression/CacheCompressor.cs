using Blamite.Blam;
using Blamite.Blam.SecondGen;
using Blamite.IO;
using Blamite.Serialization;
using Blamite.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Compression
{
	public enum CompressionState
	{
		Null,
		Compressed,
		Decompressed
	}

	/// <summary>
	/// A class for handling MCC's cache compression
	/// </summary>
	public static class CacheCompressor
	{
		/// <summary>
		/// Reads in the input file, determines its current compression state, and reverses it by default.
		/// </summary>
		/// <param name="input">Cache file to compress</param>
		/// <param name="engineDb">The engine database to use to process the cache file.</param>
		/// <param name="desiredState">Optional. When set to not null, the default behavior is overridden and skips action if the cache file is already that state.</param>
		/// <returns></returns>
		public static CompressionState HandleCompression(string input, EngineDatabase engineDb, CompressionState desiredState = CompressionState.Null)
		{
			CompressionState state;
			EngineType type;
			using (FileStream fileStream = File.OpenRead(input))
			{
				var reader = new EndianReader(fileStream, Endian.BigEndian);

				state = DetermineState(reader, engineDb, out type);
			}

			if (state == desiredState)
				return state;

			switch (state)
			{
				default:
				case CompressionState.Null:
					return state;
				case CompressionState.Compressed:
					{
						if (type == EngineType.SecondGeneration)
						{
							DecompressSecondGen(input);
							return CompressionState.Decompressed;
						}
						else
							return state;
					}
				case CompressionState.Decompressed:
					{
						if (type == EngineType.SecondGeneration)
						{
							CompressSecondGen(input);
							return CompressionState.Compressed;
						}
						else
							return state;
					}
			}
		}

		private static CompressionState DetermineState(IReader reader, EngineDatabase engineDb, out EngineType type)
		{
			// Set the reader's endianness based upon the file's header magic
			reader.SeekTo(0);
			byte[] headerMagic = reader.ReadBlock(4);
			reader.Endianness = CacheFileLoader.DetermineCacheFileEndianness(headerMagic);

			// Load engine version info
			var version = new CacheFileVersionInfo(reader);
			type = version.Engine;

			if (version.Engine == EngineType.SecondGeneration)
			{
				// Load build info
				var engineInfo = engineDb.FindEngineByVersion(version.BuildString);
				if (engineInfo == null)
					return CompressionState.Null;

				if (!engineInfo.UsesCompression)
					return CompressionState.Null;

				return AnalyzeSecondGen(reader, engineInfo);
			}
			else
				return CompressionState.Null;
		}

		#region Second Generation
		private static CompressionState AnalyzeSecondGen(IReader reader, EngineDescription engineInfo)
		{
			// H2 header is uncompressed, so the cache file needs to be loaded enough to check if the tag table is readable
			var segmenter = new FileSegmenter(engineInfo.SegmentAlignment);

			reader.SeekTo(0);
			StructureValueCollection headerValues = StructureReader.ReadStructure(reader, engineInfo.Layouts.GetLayout("header"));

			var metaOffset = (int)headerValues.GetInteger("meta offset");
			var metaSize = (int)headerValues.GetInteger("meta size");
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

		private static void CompressSecondGen(string file)
		{
			using (MemoryStream msOutput = new MemoryStream())
			{
				using (BinaryWriter bwOutput = new BinaryWriter(msOutput))
				{
					using (FileStream fsInput = new FileStream(file, FileMode.Open))
					{
						List<Tuple<int, int>> Chunks = new List<Tuple<int, int>>();

						//header is uncompressed
						byte[] header = new byte[0x1000];
						fsInput.Read(header, 0, 0x1000);
						msOutput.Write(header, 0, 0x1000);

						int datalength = (int)fsInput.Length - 0x1000;
						int chunkcount = ((datalength + 0x3FFFF) & ~0x3FFFF) / 0x40000;

						int datastart = 0x3000;
						msOutput.Position = datastart;

						while (fsInput.Position < fsInput.Length)
						{
							int size = 0x40000;
							if (fsInput.Length - fsInput.Position < size)
								size = datalength % 0x40000;

							int start = (int)msOutput.Position;

							// 1) deflatestream doesnt write a header.
							// 2) this specific header (x2815) is whats used internally, and h2 mcc wont load without it, regardless of the actual data
							bwOutput.Write((short)5416);

							using (DeflateStream ds = new DeflateStream(msOutput, CompressionMode.Compress, true))
							{
								byte[] chunkData = new byte[size];
								fsInput.Read(chunkData, 0, size);
								ds.Write(chunkData, 0, chunkData.Length);
							}

							int complength = (int)msOutput.Position - start;
							Chunks.Add(new Tuple<int, int>(complength, start));

							//each chunk is padded
							long remainder = complength % 0x80;
							msOutput.Seek(0x80 - remainder, SeekOrigin.Current);
						}

						msOutput.Position = 0x1000;
						for (int i = 0; i < chunkcount; i++)
						{
							bwOutput.Write(Chunks[i].Item1);
							bwOutput.Write(Chunks[i].Item2);
						}
					}
					File.WriteAllBytes(file, msOutput.ToArray());
				}
			}
		}

		private static void DecompressSecondGen(string file)
		{
			using (MemoryStream msOutput = new MemoryStream())
			{
				using (FileStream fsInput = new FileStream(file, FileMode.Open))
				{
					using (BinaryReader brInput = new BinaryReader(fsInput))
					{
						//header is uncompressed
						msOutput.Write(brInput.ReadBytes(0x1000), 0, 0x1000);

						List<Tuple<int, int>> Chunks = new List<Tuple<int, int>>();

						for (int i = 0; i < 0x400; i++)
						{
							int csize = brInput.ReadInt32();
							int offset = brInput.ReadInt32();

							if (csize == 0)
								break;

							if (offset >= fsInput.Length)
								throw new ArgumentException("Chunk " + i + " has an offset past the end of the file.");

							Chunks.Add(new Tuple<int, int>(csize, offset));
						}

						//Decompress and write each chunk
						for (int i = 0; i < Chunks.Count; i++)
						{
							//check for faux-compression some other tools use
							if (Chunks[i].Item1 < 0)
							{
								int invertedSize = -Chunks[i].Item1;
								byte[] aaa = new byte[invertedSize];

								fsInput.Seek(Chunks[i].Item2, SeekOrigin.Begin);

								int readSize = fsInput.Read(aaa, 0, invertedSize);

								msOutput.Write(aaa, 0, readSize);
							}
							else
							{
								fsInput.Seek(Chunks[i].Item2 + 2, SeekOrigin.Begin);

								int realsize = 0x40000;
								byte[] chunkData = new byte[realsize];

								using (DeflateStream ds = new DeflateStream(fsInput, CompressionMode.Decompress, true))
								{
									realsize = ds.Read(chunkData, 0, chunkData.Length);
								}

								msOutput.Write(chunkData, 0, realsize);
							}
						}
					}
				}
				File.WriteAllBytes(file, msOutput.ToArray());
			}
		}
		#endregion
	}
}
