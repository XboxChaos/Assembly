using System.Collections;
using System.Collections.Generic;
using System.IO;
using Blamite.Blam.Util;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam
{
	/// <summary>
	///     A table of strings associated with a table of string offsets.
	/// </summary>
	public class IndexedStringTable : IEnumerable<string>
	{
		private readonly FileSegment _data;
		private readonly FileSegment _indexTable;
		private readonly AESKey _key;
		private readonly List<string> _strings = new List<string>();

		public IndexedStringTable(IReader reader, int count, FileSegment indexTable, FileSegment data, AESKey key)
		{
			_key = key;
			_indexTable = indexTable;
			_data = data;

			if (reader == null || indexTable == null)
				return;

			int[] offsets = ReadOffsets(reader, indexTable, count);
			IReader stringReader = DecryptData(reader, data, key);

			// Read each string
			stringReader.SeekTo(0);
			for (int i = 0; i < offsets.Length; i++)
			{
				if (offsets[i] == -1)
					_strings.Add(null);
				else
				{
					stringReader.SeekTo(offsets[i]);
					_strings.Add(stringReader.ReadAscii());
				}
			}
		}

		/// <summary>
		///     Gets the number of strings in the table.
		/// </summary>
		public int Count
		{
			get { return _strings.Count; }
		}

		/// <summary>
		///     Gets or sets the string at an index.
		/// </summary>
		/// <param name="index">The index of the string to get or set.</param>
		/// <returns>The string at the given index.</returns>
		public string this[int index]
		{
			get { return _strings[index]; }
			set { _strings[index] = value; }
		}

		public IEnumerator<string> GetEnumerator()
		{
			return _strings.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _strings.GetEnumerator();
		}

		/// <summary>
		///     Adds a string to the table.
		/// </summary>
		/// <param name="str">The string to add.</param>
		public void Add(string str)
		{
			_strings.Add(str);
		}

		/// <summary>
		///     Expands the table to be at least a certain length by adding null strings to the end.
		/// </summary>
		/// <param name="length">The minimum length that the table must have.</param>
		public void Expand(int length)
		{
			while (_strings.Count < length)
				_strings.Add(null);
		}

		/// <summary>
		///     Searches for a given string and returns the zero-based index of its first occurrence in the table. O(n).
		/// </summary>
		/// <param name="str">The string to search for. Case-sensitive.</param>
		/// <returns>The zero-based index of the first occurrence of the string in the table, or -1 if not found.</returns>
		public int IndexOf(string str)
		{
			// TODO: Change this to use a Dictionary or something if the O(n) runtime complexity is too inefficient
			return _strings.IndexOf(str);
		}

		/// <summary>
		///     Saves changes made to the string table.
		/// </summary>
		/// <param name="stream">The stream to manipulate.</param>
		public void SaveChanges(IStream stream)
		{
			SaveOffsets(stream);
			SaveData(stream);
		}

		private int[] ReadOffsets(IReader reader, FileSegment indexTable, int count)
		{
			reader.SeekTo(indexTable.Offset);
			var offsets = new int[count];
			for (int i = 0; i < count; i++)
				offsets[i] = reader.ReadInt32();
			return offsets;
		}

		private void SaveOffsets(IStream stream)
		{
			// I'm assuming here that the official cache files don't intern strings
			// Doing that might be a possibility even if they don't, but, meh
			_indexTable.Resize((uint)_strings.Count*4, stream);
			stream.SeekTo(_indexTable.Offset);
			int currentOffset = 0;
			foreach (string str in _strings)
			{
				if (str != null)
				{
					stream.WriteInt32(currentOffset);
					currentOffset += str.Length + 1; // + 1 is for the null terminator
				}
				else
				{
					stream.WriteInt32(-1);
				}
			}
		}

		private void SaveData(IStream stream)
		{
			// Create a memory buffer and write the strings there
			using (var buffer = new MemoryStream())
			using (var bufferWriter = new EndianWriter(buffer, stream.Endianness))
			{
				// Write the strings to the buffer
				foreach (string str in _strings)
				{
					if (str != null)
						bufferWriter.WriteAscii(str);
				}

				// Align the buffer's length if encryption is necessary
				if (_key != null)
					buffer.SetLength(AES.AlignSize((int)buffer.Length));

				byte[] data = buffer.ToArray();

				// Encrypt the buffer if necessary
				if (_key != null)
					data = AES.Encrypt(data, 0, (int)buffer.Length, _key.Key, _key.IV);

				// Resize the data area and write it in
				_data.Resize((uint)buffer.Length, stream);
				stream.SeekTo(_data.Offset);
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
