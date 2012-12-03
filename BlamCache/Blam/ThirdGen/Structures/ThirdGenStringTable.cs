using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.IO;
using ExtryzeDLL.Util;

namespace ExtryzeDLL.Blam.ThirdGen.Structures
{
    public class ThirdGenStringTable
    {
        private List<string> _strings = new List<string>();

        public ThirdGenStringTable(IReader reader, int count, int tableSize, Pointer indexTableLocation, Pointer dataLocation, AESKey key)
        {
            int[] offsets = ReadOffsets(reader, indexTableLocation, count);
            IReader stringReader = DecryptData(reader, dataLocation, tableSize, key);

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
            get { return _strings.AsReadOnly(); }
        }

        private int[] ReadOffsets(IReader reader, Pointer indexTableLocation, int count)
        {
            reader.SeekTo(indexTableLocation.AsOffset());
            int[] offsets = new int[count];
            for (int i = 0; i < count; i++)
                offsets[i] = reader.ReadInt32();
            return offsets;
        }

        private IReader DecryptData(IReader reader, Pointer dataLocation, int tableSize, AESKey key)
        {
            // Round the table size to an AES block size
            tableSize = (tableSize + 0xF) & ~0xF;

            reader.SeekTo(dataLocation.AsOffset());
            byte[] data = reader.ReadBlock(tableSize);
            if (key != null)
                data = AES.Decrypt(data, key.Key, key.IV);
            return new EndianReader(new MemoryStream(data), Endian.BigEndian);
        }
    }
}
