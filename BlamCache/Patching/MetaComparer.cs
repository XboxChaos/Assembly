using System;
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
            var bufferAddress = originalFile.Info.VirtualBaseAddress;
			var bufferOffset = originalFile.Info.MetaOffset;
			var endOffset = bufferOffset + originalFile.Info.MetaSize;

            const int BufferSize = 0x1000;
            var oldBuffer = new byte[BufferSize];
			var newBuffer = new byte[BufferSize];

            originalReader.SeekTo(bufferOffset);
            newReader.SeekTo(bufferOffset);
            while (bufferOffset < endOffset)
            {
				var diffStart = 0;
                uint diffAddress = 0;
				var diffSize = 0;

                // Read the meta in large blocks and then compare the blocks
                originalReader.ReadBlock(oldBuffer, 0, BufferSize);
                newReader.ReadBlock(newBuffer, 0, BufferSize);
				for (var i = 0; i < oldBuffer.Length; i++)
                {
                    if (oldBuffer[i] != newBuffer[i])
                    {
                        if (diffSize == 0)
                        {
                            diffStart = i;
                            diffAddress = (uint)(bufferAddress + i);
                        }
                        diffSize++;
                    }
                    else if (diffSize > 0)
                    {
                        // Found a complete difference region - build data for the change and add it
                        output.MetaChanges.Add(BuildChange(newBuffer, diffStart, diffAddress, diffSize));
                        diffSize = 0;
                    }
                }

                // Handle differences at the end of the buffer
                if (diffSize > 0)
                    output.MetaChanges.Add(BuildChange(newBuffer, diffStart, diffAddress, diffSize));

                // Advance to the next block
                bufferOffset += BufferSize;
                bufferAddress += BufferSize;
            }
        }

        private static MetaChange BuildChange(byte[] diffBuffer, int diffOffset, uint diffAddress, int diffSize)
        {
            // Copy the differing bytes to a buffer
            var diff = new byte[diffSize];
            Array.Copy(diffBuffer, diffOffset, diff, 0, diffSize);

            return new MetaChange(diffAddress, diff);
        }
    }
}