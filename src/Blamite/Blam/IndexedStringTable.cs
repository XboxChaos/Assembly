using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Blamite.Blam.Util;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam
{
    /// <summary>
    /// A table of strings associated with a table of string offsets.
    /// </summary>
    public class IndexedStringTable : IEnumerable<string>
    {
        private List<string> _strings = new List<string>();

        public IndexedStringTable(IReader reader, int count, FileSegment indexTable, FileSegment data, AESKey key)
        {
            int[] offsets = ReadOffsets(reader, indexTable, count);
            IReader stringReader = DecryptData(reader, data, key);

            // Read each string
            stringReader.SeekTo(0);
            for (int i = 0; i < offsets.Length; i++)
            {
                stringReader.SeekTo(offsets[i]);
                _strings.Add(stringReader.ReadAscii());
            }
        }

        /// <summary>
        /// Gets the number of strings in the table.
        /// </summary>
        public int Count
        {
            get { return _strings.Count; }
        }

        /// <summary>
        /// Gets the string at an index.
        /// </summary>
        /// <param name="index">The index of the string to retrieve.</param>
        /// <returns>The string at the given index.</returns>
        public string this[int index]
        {
            get { return _strings[index]; }
        }

        /// <summary>
        /// Searches for a given string and returns the zero-based index of its first occurrence in the table. O(n).
        /// </summary>
        /// <param name="str">The string to search for. Case-sensitive.</param>
        /// <returns>The zero-based index of the first occurrence of the string in the table, or -1 if not found.</returns>
        public int IndexOf(string str)
        {
            // TODO: Change this to use a Dictionary or something if the O(n) runtime complexity is too inefficient
            return _strings.IndexOf(str);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _strings.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _strings.GetEnumerator();
        }

        private int[] ReadOffsets(IReader reader, FileSegment indexTable, int count)
        {
            reader.SeekTo(indexTable.Offset);
            int[] offsets = new int[count];
            for (int i = 0; i < count; i++)
                offsets[i] = reader.ReadInt32();
            return offsets;
        }

        private IReader DecryptData(IReader reader, FileSegment dataLocation, AESKey key)
        {
            reader.SeekTo(dataLocation.Offset);
            byte[] data = reader.ReadBlock(AES.AlignSize(dataLocation.Size));
            if (key != null)
                data = AES.Decrypt(data, key.Key, key.IV);
            return new EndianReader(new MemoryStream(data), Endian.BigEndian);
        }
    }
}
