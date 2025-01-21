using System.IO;
using Blamite.Blam.Util;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.Eldorado
{
	public class ExternalIndexedStringTable : IndexedStringTable
	{
		private readonly AESKey _key;
		private readonly string _fileName;
		private readonly Endian _endianness;

		public ExternalIndexedStringTable(string fileName, Endian endianness, AESKey key) : base(null, 0, null, null, key)
		{
			_key = key;
			_fileName = fileName;
			_endianness = endianness;

			using (FileStream stringStream = File.OpenRead(fileName))
			using (EndianReader stringFileReader = new EndianReader(stringStream, endianness))
			{
				var stringSegmenter = new FileSegmenter(1);

				int count = stringFileReader.ReadInt32();
				uint dataSize = stringFileReader.ReadUInt32();
				uint tablesize = (uint)count * 4;

				stringSegmenter.WrapEOF(8 + tablesize + dataSize);
				var indexTable = stringSegmenter.WrapSegment(8, tablesize, 4, SegmentResizeOrigin.End);
				var data = stringSegmenter.WrapSegment(8 + tablesize, dataSize, 1, SegmentResizeOrigin.End);

				int[] offsets = ReadOffsets(stringFileReader, indexTable, count);
				var stringDataReader = DecryptData(stringFileReader, data, key);

				// Read each string
				stringDataReader.SeekTo(0);
				for (int i = 0; i < offsets.Length; i++)
				{
					if (offsets[i] == -1)
						_strings.Add("");
					else
					{
						stringDataReader.SeekTo(offsets[i]);
						_strings.Add(stringDataReader.ReadAscii());
					}
				}
			}
		}

		/// <summary>
		///     Saves changes made to the string table.
		/// </summary>
		/// <param name="stream">The stream to manipulate.</param>
		public override void SaveChanges(IStream stream)
		{
			using (MemoryStream ms = new MemoryStream())
			using (EndianStream stringWriter = new EndianStream(ms, _endianness))
			{
				int dataOffset = SaveOffsets(stringWriter);
				SaveData(stringWriter, dataOffset);
				File.WriteAllBytes(_fileName, ms.ToArray());
			}
		}

		private int[] ReadOffsets(IReader reader, FileSegment indexTable, int count)
		{
			reader.SeekTo(indexTable.Offset);
			var offsets = new int[count];
			for (int i = 0; i < count; i++)
				offsets[i] = reader.ReadInt32();
			return offsets;
		}

		private int SaveOffsets(IStream stream)
		{
			stream.WriteInt32(Count);
			stream.SeekTo(8);

			int currentOffset = 0;
			for (int i = 0; i < _strings.Count; i++)
			{
				if (string.IsNullOrEmpty(_strings[i]))
				{
					//theres a section where a bunch of nulls are present, so past the initial empty string write nulls for empty strings.
					if (i == 0)
					{
						stream.WriteInt32(currentOffset);
						currentOffset++;
					}
					else
						stream.WriteInt32(-1);
				}
				else
				{
					stream.WriteInt32(currentOffset);
					currentOffset += _strings[i].Length + 1; // + 1 is for the null terminator
				}
			}

			int dataOffset = (int)stream.Position;
			stream.SeekTo(4);
			stream.WriteInt32(currentOffset);

			return dataOffset;
		}

		private void SaveData(IStream stream, int dataOffset)
		{
			// Create a memory buffer and write the strings there
			using (var buffer = new MemoryStream())
			using (var bufferWriter = new EndianWriter(buffer, stream.Endianness))
			{
				// Write the strings to the buffer
				for (int i = 0; i < _strings.Count; i++)
				{
					if (string.IsNullOrEmpty(_strings[i]))
					{
						//theres a section where a bunch of nulls are present, so past the initial empty string write nulls for empty strings.
						if (i == 0)
							bufferWriter.WriteByte(0);
					}
					else
						bufferWriter.WriteAscii(_strings[i]);
				}

				// Align the buffer's length if encryption is necessary
				if (_key != null)
					buffer.SetLength(AES.AlignSize((int)buffer.Length));

				byte[] data = buffer.ToArray();

				// Encrypt the buffer if necessary
				if (_key != null)
					data = AES.Encrypt(data, 0, (int)buffer.Length, _key.Key, _key.IV);

				stream.SeekTo(dataOffset);
				stream.WriteBlock(data, 0, (int)buffer.Length);
			}
		}

		private IReader DecryptData(IReader reader, FileSegment dataLocation, AESKey key)
		{
			reader.SeekTo(dataLocation.Offset);
			byte[] data = reader.ReadBlock(AES.AlignSize((int)dataLocation.Size));
			if (key != null)
				data = AES.Decrypt(data, key.Key, key.IV);
			return new EndianReader(new MemoryStream(data), Endian.BigEndian);
		}
	}
}
