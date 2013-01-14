using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Patching
{
    public static class MetaComparer
    {
        /// <summary>
        /// Compares the tag meta between two cache files and adds the results to a patch.
        /// </summary>
        /// <param name="originalFile">The unmodified cache file.</param>
        /// <param name="originalReader">A stream open on the unmodified cache file.</param>
        /// <param name="newFile">The modified cache file.</param>
        /// <param name="newReader">A stream open on the modified cache file.</param>
        /// <param name="output">The Patch to add results to.</param>
        public static void CompareMeta(ICacheFile originalFile, IReader originalReader, ICacheFile newFile, IReader newReader, Patch output)
        {
            // TODO: Handle files with expanded meta partitions
            uint address = originalFile.Info.MetaBase.AsAddress();
            uint offset = originalFile.Info.MetaBase.AsOffset();
            uint endOffset = offset + originalFile.Info.MetaSize;

            const int BufferSize = 0x1000;
            byte[] oldBuffer = new byte[BufferSize];
            byte[] newBuffer = new byte[BufferSize];

            int diffStart = 0;
            uint diffAddress = 0;
            int diffSize = 0;

            originalReader.SeekTo(offset);
            newReader.SeekTo(offset);
            while (offset < endOffset)
            {
                // Read the meta in large blocks and then compare the blocks
                originalReader.ReadBlock(oldBuffer, 0, BufferSize);
                newReader.ReadBlock(newBuffer, 0, BufferSize);
                for (int i = 0; i < oldBuffer.Length; i++)
                {
                    if (oldBuffer[i] != newBuffer[i])
                    {
                        if (diffSize == 0)
                        {
                            diffStart = i;
                            diffAddress = (uint)(address + i);
                        }
                        diffSize++;
                    }
                    else if (diffSize > 0)
                    {
                        // Copy the differing bytes to a buffer
                        byte[] diff = new byte[diffSize];
                        Array.Copy(newBuffer, diffStart, diff, 0, diffSize);

                        // Export the change data
                        MetaChange change = new MetaChange((uint)(address + i), diff);
                        output.MetaChanges.Add(change);

                        diffSize = 0;
                    }
                }

                offset += BufferSize;
                address += BufferSize;
            }
        }
    }
}
