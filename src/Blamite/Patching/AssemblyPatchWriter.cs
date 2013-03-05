using System;
using System.Linq;
using System.Collections.Generic;
using Blamite.IO;

namespace Blamite.Patching
{
    public static class AssemblyPatchWriter
    {
        public static void WritePatch(Patch patch, IWriter writer)
        {
			var startPos = WriteBlockHeader(writer, AssemblyPatchMagic);
            writer.WriteByte(0); // No compression

            WriteBlocks(patch, writer);

            EndBlock(writer, startPos);
        }

        private static void WriteBlocks(Patch patch, IWriter writer)
        {
            WritePatchInfo(patch, writer);
            WriteSegmentChanges(patch, writer);
            WriteBlfInfo(patch, writer);

            #region Deprecated
            WriteMetaChanges(patch, writer);
            WriteLocaleChanges(patch, writer);
            #endregion Deprecated
        }

        private static void WritePatchInfo(Patch patch, IWriter writer)
        {
			var startPos = WriteBlockHeader(writer, AssemblyPatchBlockID.Titl);
            writer.WriteByte(1); // Version 1

            // Write target map info
            writer.WriteInt32(patch.MapID);
            if (patch.MapInternalName != null)
                writer.WriteAscii(patch.MapInternalName);
            else
                writer.WriteByte(0);

            // Write patch info
            writer.WriteUTF16(patch.Name);
            writer.WriteUTF16(patch.Description);
            writer.WriteUTF16(patch.Author);

            // Write screenshot
            if (patch.Screenshot != null)
            {
                writer.WriteInt32(patch.Screenshot.Length);
                writer.WriteBlock(patch.Screenshot);
            }
            else
            {
                writer.WriteInt32(0);
            }

            // Write meta info
            writer.WriteUInt32(patch.MetaPokeBase);
            writer.WriteSByte((sbyte)patch.MetaChangesIndex);

            EndBlock(writer, startPos);
        }

        private static void WriteSegmentChanges(Patch patch, IWriter writer)
        {
            if (patch.SegmentChanges.Count == 0)
                return;

            var startPos = WriteBlockHeader(writer, AssemblyPatchBlockID.Segm);
            writer.WriteByte(0); // Version 0

            writer.WriteByte((byte)patch.SegmentChanges.Count);
            foreach (var segment in patch.SegmentChanges)
            {
                writer.WriteUInt32(segment.OldOffset);
                writer.WriteInt32(segment.OldSize);
                writer.WriteUInt32(segment.NewOffset);
                writer.WriteInt32(segment.NewSize);
                writer.WriteByte(Convert.ToByte(segment.ResizeAtEnd));

                WriteDataChanges(segment.DataChanges, writer);
            }

            EndBlock(writer, startPos);
        }

        private static void WriteDataChanges(IList<DataChange> changes, IWriter writer)
        {
            var fourByteChanges = changes.Where((c) => c.Data.Length == 4).ToList();
            var otherChanges = changes.Where((c) => c.Data.Length != 4).ToList();

            // Write 4-byte changes
            writer.WriteUInt32((uint)fourByteChanges.Count);
            foreach (var change in fourByteChanges)
            {
                writer.WriteUInt32(change.Offset);
                writer.WriteBlock(change.Data);
            }

            // Write other changes
            writer.WriteUInt32((uint)otherChanges.Count);
            foreach (var change in otherChanges)
            {
                writer.WriteUInt32(change.Offset);
                writer.WriteInt32(change.Data.Length);
                writer.WriteBlock(change.Data);
            }
        }

        private static void WriteBlfInfo(Patch patch, IWriter writer)
        {
            if (patch.CustomBlfContent == null)
                return;

            var startPos = WriteBlockHeader(writer, AssemblyPatchBlockID.Blfc);
            writer.WriteByte(0); // Version 0

            writer.WriteByte((byte)patch.CustomBlfContent.TargetGame);
            
            // Write mapinfo filename
            if (patch.CustomBlfContent.MapInfoFileName != null)
                writer.WriteAscii(patch.CustomBlfContent.MapInfoFileName);
            else
                writer.WriteByte(0);

            // Write mapinfo data
            if (patch.CustomBlfContent.MapInfo != null)
            {
                writer.WriteUInt32((uint)patch.CustomBlfContent.MapInfo.Length);
                writer.WriteBlock(patch.CustomBlfContent.MapInfo);
            }
            else
            {
                writer.WriteUInt32(0);
            }

            // Write BLF containers
            writer.WriteInt16((short)patch.CustomBlfContent.BlfContainerEntries.Count);
            foreach (var blfContainerEntry in patch.CustomBlfContent.BlfContainerEntries)
            {
                writer.WriteAscii(blfContainerEntry.FileName);
                writer.WriteUInt32((uint)blfContainerEntry.BlfContainer.Length);
                writer.WriteBlock(blfContainerEntry.BlfContainer);
            }

            EndBlock(writer, startPos);
        }

        #region Deprecated
        private static void WriteMetaChanges(Patch patch, IWriter writer)
        {
            if (patch.MetaChanges.Count == 0)
                return;

			var startPos = WriteBlockHeader(writer, AssemblyPatchBlockID.Meta);
            writer.WriteByte(0); // Version 0

            var fourByteChanges = patch.MetaChanges.Where((c) => c.Data.Length == 4).ToList();
            var otherChanges = patch.MetaChanges.Where((c) => c.Data.Length != 4).ToList();

            // Write 4-byte changes
            writer.WriteUInt32((uint)fourByteChanges.Count);
			foreach (var change in fourByteChanges)
            {
                writer.WriteUInt32(change.Offset);
                writer.WriteBlock(change.Data);
            }

            // Write other changes
            writer.WriteUInt32((uint)otherChanges.Count);
			foreach (var change in otherChanges)
            {
                writer.WriteUInt32(change.Offset);
                writer.WriteInt32(change.Data.Length);
                writer.WriteBlock(change.Data);
            }

            EndBlock(writer, startPos);
        }

        private static void WriteLocaleChanges(Patch patch, IWriter writer)
        {
            if (patch.LanguageChanges.Count == 0)
                return;

			var startPos = WriteBlockHeader(writer, AssemblyPatchBlockID.Locl);
            writer.WriteByte(0); // Version 0

            // Write change data for each language
            writer.WriteByte((byte)patch.LanguageChanges.Count);
			foreach (var language in patch.LanguageChanges)
            {
                writer.WriteByte(language.LanguageIndex);

                // Write the change data for each string in the language
                writer.WriteInt32(language.LocaleChanges.Count);
				foreach (var change in language.LocaleChanges)
                {
                    writer.WriteInt32(change.Index);
                    writer.WriteUTF8(change.NewValue);
                }
            }

            EndBlock(writer, startPos);
        }
        #endregion Deprecated

        private static long WriteBlockHeader(IWriter writer, int magic)
        {
			var startPos = writer.Position;
            writer.WriteInt32(magic);
            writer.WriteUInt32(0); // Size filled in later
            return startPos;
        }

        private static void EndBlock(IWriter writer, long headerPos)
        {
			var endPos = writer.Position;
            writer.SeekTo(headerPos + 4);
            writer.WriteUInt32((uint)(endPos - headerPos));
            writer.SeekTo(endPos);
        }

        private const int AssemblyPatchMagic = 0x61736D70;
    }
}