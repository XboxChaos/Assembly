using System;
using Blamite.Blam.FirstGen.Structures;
using Blamite.IO;

namespace Blamite.Blam.FirstGen
{
    public class FirstGenIndexedStringTable : IndexedStringTable
    {
        public FirstGenIndexedStringTable(IReader reader, FirstGenTagTable tags) : base(null, 0, null, null, null)
        {
            uint[] offsets = ReadOffsets(tags, tags.Count);

            // Read each string
            reader.SeekTo(0);
            for (int i = 0; i < offsets.Length; i++)
            {
                if (offsets[i] == 0xFFFFFFFF)
                    Add(null);
                else
                {
                    reader.SeekTo(offsets[i]);
                    string name = reader.ReadAscii();
                    Add(name);
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

        private uint[] ReadOffsets(FirstGenTagTable tags, int count)
        {
            var offsets = new uint[count];
            int i = 0;
            foreach (FirstGenTag tag in tags)
            {
                offsets[i] = tag.FileNameOffset.AsOffset();
                i++;
            }
            return offsets;
        }
    }
}