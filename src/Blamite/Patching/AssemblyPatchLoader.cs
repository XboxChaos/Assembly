using System;
using System.Linq;
using System.Collections.Generic;
using Blamite.IO;
using System.IO;

namespace Blamite.Patching
{
    public static class AssemblyPatchLoader
    {
        public static Patch LoadPatch(IReader reader)
        {
            ContainerReader container = new ContainerReader(reader);
            if (!container.NextBlock() || container.BlockName != "asmp")
                throw new InvalidOperationException("Invalid assembly patch");
            if (container.BlockVersion > 0)
                throw new InvalidOperationException("Unrecognized patch version");

            container.EnterBlock();
            Patch patch = ReadBlocks(reader, container);
            container.LeaveBlock();
            
            return patch;
        }

        private static Patch ReadBlocks(IReader reader, ContainerReader container)
        {
			var result = new Patch();
            while (container.NextBlock())
            {
                switch (container.BlockName)
				{
                    case "titl":
                        ReadPatchInfo(reader, container.BlockVersion, result);
                        break;

                    case "segm":
                        ReadSegmentChanges(reader, result);
                        break;

                    case "blfc":
                        ReadBlfInfo(reader, result);
                        break;

                    #region Deprecated
                    case "meta":
                        ReadMetaChanges(reader, result);
                        break;

                    case "locl":
                        ReadLocaleChanges(reader, result);
                        break;
                    #endregion Deprecated
                }
            }
            return result;
        }

        private static void ReadPatchInfo(IReader reader, byte version, Patch output)
        {
            // Version 0 (all versions)
            output.MapID = reader.ReadInt32();
            output.MapInternalName = reader.ReadAscii();
            output.Name = reader.ReadUTF16();
            output.Description = reader.ReadUTF16();
            output.Author = reader.ReadUTF16();

			var screenshotLength = reader.ReadInt32();
            if (screenshotLength > 0)
                output.Screenshot = reader.ReadBlock(screenshotLength);

            // Version 1
            if (version == 1)
            {
                output.MetaPokeBase = reader.ReadUInt32();
                output.MetaChangesIndex = reader.ReadSByte();
            }
        }

        private static void ReadSegmentChanges(IReader reader, Patch output)
        {
            // Version 0 (all versions)
            var numChanges = reader.ReadByte();
            for (var i = 0; i < numChanges; i++)
            {
                var oldOffset = reader.ReadUInt32();
                var oldSize = reader.ReadInt32();
                var newOffset = reader.ReadUInt32();
                var newSize = reader.ReadInt32();
                var resizeAtEnd = Convert.ToBoolean(reader.ReadByte());
                var segmentChange = new SegmentChange(oldOffset, oldSize, newOffset, newSize, resizeAtEnd);
                segmentChange.DataChanges.AddRange(ReadDataChanges(reader));

                output.SegmentChanges.Add(segmentChange);
            }
        }

        private static List<DataChange> ReadDataChanges(IReader reader)
        {
            List<DataChange> result = new List<DataChange>();

            var numFourByteChanges = reader.ReadUInt32();
            for (var j = 0; j < numFourByteChanges; j++)
            {
                var offset = reader.ReadUInt32();
                var data = reader.ReadBlock(4);
                result.Add(new DataChange(offset, data));
            }

            var numOtherChanges = reader.ReadUInt32();
            for (var j = 0; j < numOtherChanges; j++)
            {
                var offset = reader.ReadUInt32();
                var dataSize = reader.ReadInt32();
                var data = reader.ReadBlock(dataSize);
                result.Add(new DataChange(offset, data));
            }

            return result;
        }

        private static void ReadBlfInfo(IReader reader, Patch output)
        {
            // Version 0 (all versions)
            var targetGame = (TargetGame)reader.ReadByte();
            var mapInfoFileName = reader.ReadAscii();
            var mapInfoLength = reader.ReadUInt32();
            var mapInfo = reader.ReadBlock((int)mapInfoLength);
            var blfContainerCount = reader.ReadInt16();
            output.CustomBlfContent = new BlfContent(mapInfoFileName, mapInfo, targetGame);
            for (var i = 0; i < blfContainerCount; i++)
            {
                var fileName = Path.GetFileName(reader.ReadAscii());
                var blfContainerLength = reader.ReadUInt32();
                var blfContainer = reader.ReadBlock((int)blfContainerLength);

                output.CustomBlfContent.BlfContainerEntries.Add(new BlfContainerEntry(fileName, blfContainer));
            }
        }

        #region Deprecated
        private static void ReadMetaChanges(IReader reader, Patch output)
        {
            output.MetaChanges.AddRange(ReadDataChanges(reader));
        }

        private static void ReadLocaleChanges(IReader reader, Patch output)
        {
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
        #endregion Deprecated
    }
}