using Blamite.Blam;
using Blamite.IO;
using Blamite.Serialization;
using System;
using System.IO;
using System.IO.Compression;

namespace Blamite.Compression
{
	public enum CompressionState
	{
		Null,
		Compressed,
		Decompressed,
		ReadOnly
	}

	public enum CompressionType
	{
		None,
		CEXBox,
		CEA360,
		CEAMCC,
		H2MCC
	}

	/// <summary>
	/// A manager for interacting with the various supported compression types of a cache file.
	/// </summary>
	public static class CompressionManager
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
			FileInfo cache = new FileInfo(input);
			if (cache.IsReadOnly)
				return CompressionState.ReadOnly;

			CompressionState state;
			EngineDescription engineInfo;
			StructureValueCollection headerValues;

			using (FileStream fileStream = File.OpenRead(input))
			{
				var reader = new EndianReader(fileStream, Endian.LittleEndian);
				state = DetermineState(reader, engineDb, out engineInfo, out headerValues);
			}

			if (state == desiredState)
				return state;

			StructureLayout headerLayout = engineInfo.Layouts.GetLayout("header");

			switch (state)
			{
				default:
				case CompressionState.Null:
					return state;
				case CompressionState.Compressed:
					{
						if (engineInfo.Compression == CompressionType.CEXBox)
						{
							int headerSize = headerLayout.Size;
							FirstGenXboxZLib.DecompressCache(input, headerSize, (int)headerValues.GetInteger("file size"));
							return CompressionState.Decompressed;
						}
						else if (engineInfo.Compression == CompressionType.CEA360)
						{
							FirstGenSaberLZX.DecompressCache(input);
							return CompressionState.Decompressed;
						}
						else if (engineInfo.Compression == CompressionType.CEAMCC)
						{
							FirstGenSaberZLib.DecompressCache(input);
							return CompressionState.Decompressed;
						}
						else if (engineInfo.Compression == CompressionType.H2MCC)
						{
							SecondGenSaberZLib.DecompressCache(input, headerValues, headerLayout);
							return CompressionState.Decompressed;
						}
						else
							return state;
					}
				case CompressionState.Decompressed:
					{
						if (engineInfo.Compression == CompressionType.CEXBox)
						{
							int headerSize = headerLayout.Size;
							FirstGenXboxZLib.CompressCache(input, headerSize);
							return CompressionState.Compressed;
						}
						else if (engineInfo.Compression == CompressionType.CEA360)
						{
							FirstGenSaberLZX.CompressCache(input);
							return CompressionState.Compressed;
						}
						else if (engineInfo.Compression == CompressionType.CEAMCC)
						{
							FirstGenSaberZLib.CompressCache(input);
							return CompressionState.Compressed;
						}
						else if (engineInfo.Compression == CompressionType.H2MCC)
						{
							SecondGenSaberZLib.CompressCache(input, headerValues, headerLayout);
							return CompressionState.Compressed;
						}
						else
							return state;
					}
			}
		}

		private static CompressionState DetermineState(IReader reader, EngineDatabase engineDb, out EngineDescription engineInfo, out StructureValueCollection headerValues)
		{
			headerValues = null;
			engineInfo = null;
			try
			{
				engineInfo = CacheFileLoader.FindEngineDescription(reader, engineDb);
			}
			catch (Exception e) // map had no header, assume its CEA
			{
				using (MemoryStream ms_header_out = new MemoryStream())
				{
					// first chunk offset is at 0x4
					reader.SeekTo(0x4);
					int first_chunk_offset = reader.ReadInt32();

					if (first_chunk_offset > reader.Length)
						return CompressionState.Null;

					reader.SeekTo(first_chunk_offset);

					// CEA 360 stores an 0xFF, use it for ID
					if (reader.ReadByte() == 0xFF)
					{
						int first_chunk_size_decompressed = reader.ReadInt16();
						int first_chunk_size_compressed = reader.ReadInt16();

						if (first_chunk_offset + first_chunk_size_compressed > reader.Length)
							return CompressionState.Null;

						//byte[] first_chunk_bytes = reader.ReadBlock(first_chunk_size_compressed);
						throw new InvalidOperationException("lzx compression is not currently supported.");
					}
					else // assume CEA MCC
					{
						reader.SeekTo(first_chunk_offset);
						int first_chunk_size = reader.ReadInt32();
						reader.Skip(2);//zlib header

						if (first_chunk_offset + first_chunk_size > reader.Length)
							return CompressionState.Null;

						byte[] first_chunk_bytes = reader.ReadBlock(first_chunk_size);
						using (MemoryStream ms_header_comp = new MemoryStream(first_chunk_bytes))
						{
							using (DeflateStream ds = new DeflateStream(ms_header_comp, CompressionMode.Decompress))
							{
								ds.CopyTo(ms_header_out);
							}
						}
					}

					EndianReader header_reader = new EndianReader(ms_header_out, Endian.LittleEndian);
					engineInfo = CacheFileLoader.FindEngineDescription(header_reader, engineDb);
				}
			}

			if (engineInfo == null)
				return CompressionState.Null;

			if (engineInfo.Compression == CompressionType.CEXBox)
				return FirstGenXboxZLib.AnalyzeCache(reader, engineInfo, out headerValues);
			else if (engineInfo.Compression == CompressionType.CEA360)
				return FirstGenSaberLZX.AnalyzeCache(reader, engineInfo, out headerValues);
			else if (engineInfo.Compression == CompressionType.CEAMCC)
				return FirstGenSaberZLib.AnalyzeCache(reader, engineInfo, out headerValues);
			else if (engineInfo.Compression == CompressionType.H2MCC)
				return SecondGenSaberZLib.AnalyzeCache(reader, engineInfo, out headerValues);
			else
				return CompressionState.Null;
		}
	}
}
