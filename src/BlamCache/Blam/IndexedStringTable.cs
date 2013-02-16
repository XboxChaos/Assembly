using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.IO;
using ExtryzeDLL.Util;

namespace ExtryzeDLL.Blam
{
    /// <summary>
    /// A table of strings associated with a table of string offsets.
    /// </summary>
    public class IndexedStringTable
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

        public IList<string> Strings
        {
            get { return _strings; }
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
