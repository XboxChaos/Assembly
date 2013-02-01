using System;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Patching
{
    public static class AssemblyPatchLoader
    {
        public static Patch LoadPatch(IReader reader)
        {
            // Verify header magic
			var magic = reader.ReadInt32();
            if (magic != AssemblyPatchMagic)
                throw new InvalidOperationException("Invalid Assembly patch magic");

            // Read the file size
			var size = reader.ReadUInt32();

            // Read the compression type
			var compression = reader.ReadByte();
            if (compression > 0)
                throw new InvalidOperationException("Unrecognized patch compression type");

            return ReadBlocks(reader, 9, size);
        }

        private static Patch ReadBlocks(IReader reader, uint startOffset, uint endOffset)
        {
			var result = new Patch();
			var offset = startOffset;
            while (offset < endOffset)
            {
                reader.SeekTo(offset);
				var blockId = reader.ReadInt32();
				var size = reader.ReadUInt32();

                switch (blockId)
				{
					case AssemblyPatchBlockID.Blfc:
						ReadBlfInfo(reader, result);
						break;

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

		private static void ReadBlfInfo(IReader reader, Patch output)
		{
			// ReSharper disable UnusedVariable
			var version = reader.ReadByte();
			// ReSharper restore UnusedVariable
			
			// Version 0 (all versions)
			var targetGame = (TargetGame)reader.ReadByte();
			var mapInfoFileName = reader.ReadAscii();
			var mapInfoLength = reader.ReadUInt32();
			var mapInfo = reader.ReadBlock((int)mapInfoLength);
			var blfContainerCount = reader.ReadInt16();
			output.CustomBlfContent = new BlfContent(mapInfoFileName, mapInfo, targetGame);
			for(var i = 0; i < blfContainerCount; i++)
			{
				var fileName = reader.ReadAscii();
				var blfContainerLength = reader.ReadUInt32();
				var blfContainer = reader.ReadBlock((int)blfContainerLength);

				output.CustomBlfContent.BlfContainerEntries.Add(new BlfContainerEntry(fileName, blfContainer));
			}
		}
        private static void ReadPatchInfo(IReader reader, Patch output)
        {
// ReSharper disable UnusedVariable
			var version = reader.ReadByte();
// ReSharper restore UnusedVariable

            // Version 0 (all versions)
            output.MapID = reader.ReadInt32();
            output.MapInternalName = reader.ReadAscii();
            output.Name = reader.ReadUTF16();
            output.Description = reader.ReadUTF16();
            output.Author = reader.ReadUTF16();

			var screenshotLength = reader.ReadInt32();
            if (screenshotLength > 0)
                output.Screenshot = reader.ReadBlock(screenshotLength);
        }
        private static void ReadMetaChanges(IReader reader, Patch output)
        {
// ReSharper disable UnusedVariable
			var version = reader.ReadByte();
// ReSharper restore UnusedVariable

            // Read four-byte changes
			var numFourByteChanges = reader.ReadUInt32();
            for (uint i = 0; i < numFourByteChanges; i++)
            {
				var address = reader.ReadUInt32();
				var data = reader.ReadBlock(4);
                output.MetaChanges.Add(new MetaChange(address, data));
            }

            // Read variable-length changes
			var numChanges = reader.ReadUInt32();
            for (uint i = 0; i < numChanges; i++)
            {
				var address = reader.ReadUInt32();
				var dataSize = reader.ReadInt32();
				var data = reader.ReadBlock(dataSize);
                output.MetaChanges.Add(new MetaChange(address, data));
            }
        }
        private static void ReadLocaleChanges(IReader reader, Patch output)
        {
// ReSharper disable UnusedVariable
			var version = reader.ReadByte();
// ReSharper restore UnusedVariable

            // Read language changes
			var numLanguageChanges = reader.ReadByte();
            for (byte i = 0; i < numLanguageChanges; i++)
            {
				var languageIndex = reader.ReadByte();
				var languageChange = new LanguageChange(languageIndex);

                // Read string changes
				var numStringChanges = reader.ReadInt32();
				for (var j = 0; j < numStringChanges; j++)
                {
					var index = reader.ReadInt32();
					var newValue = reader.ReadUTF8();
                    languageChange.LocaleChanges.Add(new LocaleChange(index, newValue));
                }

                output.LanguageChanges.Add(languageChange);
            }
        }

        private const int AssemblyPatchMagic = 0x61736D70;
    }
}