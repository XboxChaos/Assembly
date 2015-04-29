using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using Blamite.Blam.Util;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.FourthGen.Strings
{
    class FourthGenIndexedStringTable
    {
    }
}





namespace Blamite.Blam
{
	/// <summary>
	///     A table of strings associated with a table of string offsets.
	/// </summary>
	public class FourthGenIndexedStringTable
	{
        private readonly List<string> _strings = new List<string>();

        // These values were figured out through trial-and-error
        private static readonly int[] _setOffsets = { 0x90F, 0x1, 0x685, 0x720, 0x7C4, 0x778, 0x7D0, 0x8EA, 0x902 };
        private const int SetMin = 0x1;   // Mininum index that goes in a set
        private const int SetMax = 0xF1E; // Maximum index that goes in a set

        public FourthGenIndexedStringTable(IReader reader)
		{
            /*
            reader.SeekTo(0x0);
            int count = reader.ReadInt32();
            int data_size = reader.ReadInt32();

            // Retrieve data offsets
            List<int> data_offsets = new List<int>();

            for (int i = 0; i < count; i++) data_offsets.Add(reader.ReadInt32());

            var offset = reader.BaseStream.Position;

            // Retrieve strings
            _strings = new List<string>();
            for (int i = 0; i < count; i++)
            {
                reader.SeekTo(data_offsets[i] + offset);
                _strings.Add(reader.ReadAscii());
            }
             * */
            reader.SeekTo(0);
            // Read the header
            var stringCount = reader.ReadInt32();  // int32 string count
            var dataSize = reader.ReadInt32();     // int32 string data size

            // Read string offsets
            var stringOffsets = new int[stringCount];
            for (var i = 0; i < stringCount; i++)
                stringOffsets[i] = reader.ReadInt32();

            // Seek to each offset and read each string
            var dataOffset = reader.BaseStream.Position;
            foreach (var offset in stringOffsets)
            {
                if (offset == -1 || offset >= dataSize)
                {
                    _strings.Add("");
                    continue;
                }
                reader.BaseStream.Position = dataOffset + offset;
                _strings.Add(reader.ReadAscii());
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
        /*
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _strings.GetEnumerator();
		}
        */

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
			SaveData(stream);
		}

		private void SaveOffsets(IStream stream)
		{
            int count = _strings.Count();
            int start_offset = 0x8 + count * 4; // Start data offset of the first string

            int currentOffset = start_offset;
			foreach (string str in _strings)
			{
				if (str != null)
				{
					stream.WriteInt32(currentOffset);
					currentOffset += str.Length + 1; // + 1 is for the null terminator
				}
				else
				{
					stream.WriteInt32(0);
				}
			}
		}

		private void SaveData(IStream stream)
		{
			// Create a memory buffer and write the strings there
			var buffer = new MemoryStream();
			var bufferWriter = new EndianWriter(buffer, stream.Endianness);
			try
			{
                int count = _strings.Count();
                int start_offset = 0x8 + count * 4; // Start data offset of the first string
                int string_data_size = 0;
                foreach (string str in _strings) string_data_size += str.Length + 1;
                int data_size = start_offset + string_data_size;

                // Headder
                bufferWriter.WriteInt32(count);
                bufferWriter.WriteInt32(data_size);

                // Offsets
                SaveOffsets(stream);

				// Write the strings to the buffer
				foreach (string str in _strings)
				{
					if (str != null)
						bufferWriter.WriteAscii(str);
				}
			}
			finally
			{
				bufferWriter.Close();
			}
		}


	}
}