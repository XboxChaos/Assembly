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
            // Verify header magic
            int magic = reader.ReadInt32();
            if (magic != AssemblyPatchMagic)
                throw new InvalidOperationException("Invalid Assembly patch magic");

            // Read the file size
            uint size = reader.ReadUInt32();

            // Read the compression type
            byte compression = reader.ReadByte();
            if (compression > 0)
                throw new InvalidOperationException("Unrecognized patch compression type");

            return ReadBlocks(reader, 9, size);
        }

        private static Patch ReadBlocks(IReader reader, uint startOffset, uint endOffset)
        {
            Patch result = new Patch();
            uint offset = startOffset;
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

                    case AssemblyPatchBlockID.Locl:
                        ReadLocaleChanges(reader, result);
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

            // Read four-byte changes
            uint numFourByteChanges = reader.ReadUInt32();
            for (uint i = 0; i < numFourByteChanges; i++)
            {
                uint address = reader.ReadUInt32();
                byte[] data = reader.ReadBlock(4);
                output.MetaChanges.Add(new MetaChange(address, data));
            }

            // Read variable-length changes
            uint numChanges = reader.ReadUInt32();
            for (uint i = 0; i < numChanges; i++)
            {
                uint address = reader.ReadUInt32();
                int dataSize = reader.ReadInt32();
                byte[] data = reader.ReadBlock(dataSize);
                output.MetaChanges.Add(new MetaChange(address, data));
            }
        }

        private static void ReadLocaleChanges(IReader reader, Patch output)
        {
            byte version = reader.ReadByte();

            // Read language changes
            byte numLanguageChanges = reader.ReadByte();
            for (byte i = 0; i < numLanguageChanges; i++)
            {
                byte languageIndex = reader.ReadByte();
                LanguageChange languageChange = new LanguageChange(languageIndex);

                // Read string changes
                int numStringChanges = reader.ReadInt32();
                for (int j = 0; j < numStringChanges; j++)
                {
                    int index = reader.ReadInt32();
                    string newValue = reader.ReadUTF8();
                    languageChange.LocaleChanges.Add(new LocaleChange(index, newValue));
                }

                output.LanguageChanges.Add(languageChange);
            }
        }

        private const int AssemblyPatchMagic = 0x61736D70;
    }
}
