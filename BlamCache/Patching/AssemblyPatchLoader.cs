using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.IO;
using ExtryzeDLL.Util;

namespace ExtryzeDLL.Patching
{
    public static class AssemblyPatchLoader
    {
        public static Patch LoadPatch(IReader reader)
        {
            int magic = reader.ReadInt32();
            if (magic != AssemblyPatchMagic)
                throw new InvalidOperationException("Invalid Assembly patch magic");
            uint size = reader.ReadUInt32();
            byte compression = reader.ReadByte();
            if (compression > 0)
                throw new InvalidOperationException("Unrecognized patch compression type");

            return ReadBlocks(reader, 9, size + 8);
        }

        private static Patch ReadBlocks(IReader reader, uint offset, uint endOffset)
        {
            Patch result = new Patch();
            while (offset < endOffset)
            {
                reader.SeekTo(offset);
                int blockId = reader.ReadInt32();
                uint size = reader.ReadUInt32();

                switch (blockId)
                {
                    case AssemblyPatchBlockID.Titl:
                        ReadPatchInfo(reader, result);
                        break;
                    case AssemblyPatchBlockID.Meta:
                        ReadMetaChanges(reader, result);
                        break;
                }

                // Skip to the next block
                offset += size;
            }
            return result;
        }

        private static void ReadPatchInfo(IReader reader, Patch output)
        {
            byte version = reader.ReadByte();

            // Version 0 (all versions)
            output.MapID = reader.ReadInt32();
            output.MapInternalName = reader.ReadAscii();
            output.Name = reader.ReadUTF16();
            output.Description = reader.ReadUTF16();
            output.Author = reader.ReadUTF16();

            int screenshotLength = reader.ReadInt32();
            if (screenshotLength > 0)
                output.Screenshot = reader.ReadBlock(screenshotLength);
        }

        private static void ReadMetaChanges(IReader reader, Patch output)
        {
            byte version = reader.ReadByte();

            // Version 0 (all versions)
            uint num4ByteChanges = 0;
            for (uint i = 0; i < num4ByteChanges; i++)
            {
                uint address = reader.ReadUInt32();
                byte[] data = reader.ReadBlock(4);
                output.MetaChanges.Add(new MetaChange(address, data));
            }

            uint numChanges = 0;
            for (uint i = 0; i < numChanges; i++)
            {
                uint address = reader.ReadUInt32();
                uint dataSize = reader.ReadUInt32();
                byte[] data = reader.ReadBlock((int)dataSize);
                output.MetaChanges.Add(new MetaChange(address, data));
            }
        }

        private const int AssemblyPatchMagic = 0x61736D70;
    }
}
