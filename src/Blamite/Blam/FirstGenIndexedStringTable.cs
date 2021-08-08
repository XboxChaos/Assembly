using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blamite.Blam.FirstGen.Structures;
using Blamite.Blam.Util;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam
{
    // TODO (Dragon): hacky string table wrapper for first gen
    //public class FirstGenIndexedStringTable : IEnumerable<string>
    public class FirstGenIndexedStringTable : IndexedStringTable
    {
        //private readonly List<string> _strings = new List<string>();

        public FirstGenIndexedStringTable(IReader reader, int count, FileSegment indexTable, FileSegment data, AESKey key) : base(reader, count, indexTable, data, key)
        {

        }

        public FirstGenIndexedStringTable(IReader reader,
                                          FirstGenTagTable tags) : base()
        {

            int[] offsets = ReadOffsets(tags, tags.Count);

            // TODO (Dragon): this is kind of hacky now and uses the original reader
            //                rather than a StringReader
            // Read each string
            reader.SeekTo(0);
            for (int i = 0; i < offsets.Length; i++)
            {
                if (offsets[i] == -1)
                    base.Add(null);
                else
                {
                    reader.SeekTo(offsets[i]);
                    string name = reader.ReadAscii();
                    base.Add(name);
                }
            }
        }

        /// <summary>
        ///     Saves changes made to the string table.
        /// </summary>
        /// <param name="stream">The stream to manipulate.</param>
        public new void SaveChanges(IStream stream)
        {
            //SaveOffsets(stream);
            //SaveData(stream);
            throw new NotImplementedException();
        }

        private int[] ReadOffsets(FirstGenTagTable tags, int count)
        {
            var offsets = new int[count];
            int i = 0;
            foreach (FirstGenTag tag in tags)
            {
                offsets[i] = tag.FileNameOffset.AsOffset();
                i++;
            }
            return offsets;
        }

        private void SaveOffsets(IStream stream)
        {
            /*
            // I'm assuming here that the official cache files don't intern strings
            // Doing that might be a possibility even if they don't, but, meh
            _indexTable.Resize(_strings.Count * 4, stream);
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
            */
            throw new NotImplementedException();
        }

        private void SaveData(IStream stream)
        {
            /*
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
                _data.Resize((int)buffer.Length, stream);
                stream.SeekTo(_data.Offset);
                stream.WriteBlock(data, 0, (int)buffer.Length);
            }*/
            throw new NotImplementedException();
        }

        // TODO (Dragon): hacky
        private IReader DecryptData(IReader reader, int dataLocation, AESKey key)
        {
            /*
            reader.SeekTo(dataLocation);

            // TODO (Dragon): hacky
            byte[] data = reader.ReadBlock(AES.AlignSize(0x1B4F8));
            if (key != null)
                data = AES.Decrypt(data, key.Key, key.IV);
            return new EndianReader(new MemoryStream(data), Endian.BigEndian);
            */
            throw new NotImplementedException();
        }
    }
}