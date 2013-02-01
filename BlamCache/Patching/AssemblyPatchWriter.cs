using System;
using System.Collections.Generic;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Patching
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
			WriteBlfInfo(patch, writer);
            WritePatchInfo(patch, writer);
            WriteMetaChanges(patch, writer);
            WriteLocaleChanges(patch, writer);
        }

		private static void WriteBlfInfo(Patch patch, IWriter writer)
		{
			// Does this mod have custom mapinfo/blf files? 
			if (patch.CustomBlfContent == null) return;

			var startPos = WriteBlockHeader(writer, AssemblyPatchBlockID.Blfc);
			writer.WriteByte(0);

			// Write Targeted Game
			writer.WriteByte((byte) patch.CustomBlfContent.TargetGame);

			// Write MapInfo Length
			writer.WriteUInt32(Convert.ToUInt32(patch.CustomBlfContent.MapInfo.Length));

			// Write MapInfo
			writer.WriteBlock(patch.CustomBlfContent.MapInfo);

			// Write Number of Blf Containers
			writer.WriteInt16(Convert.ToInt16(patch.CustomBlfContent.BlfContainerEntries.Count));

			// Write Blf Containers
			foreach (var blfContainerEntry in patch.CustomBlfContent.BlfContainerEntries)
			{
				writer.WriteAscii(blfContainerEntry.FileName);
				writer.WriteUInt32(Convert.ToUInt32(blfContainerEntry.BlfContainer.Length));
				writer.WriteBlock(blfContainerEntry.BlfContainer);
			}

			EndBlock(writer, startPos);
		}
        private static void WritePatchInfo(Patch patch, IWriter writer)
        {
			var startPos = WriteBlockHeader(writer, AssemblyPatchBlockID.Titl);
            writer.WriteByte(0); // Version 0

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

            EndBlock(writer, startPos);
        }
        private static void WriteMetaChanges(Patch patch, IWriter writer)
        {
            if (patch.MetaChanges.Count == 0)
                return;

			var startPos = WriteBlockHeader(writer, AssemblyPatchBlockID.Meta);
            writer.WriteByte(0); // Version 0

            // Filter meta changes by size (as a file size optimization)
			var fourByteChanges = new List<MetaChange>();
			var otherChanges = new List<MetaChange>();
			foreach (var change in patch.MetaChanges)
            {
                if (change.Data.Length == 4)
                    fourByteChanges.Add(change);
                else
                    otherChanges.Add(change);
            }

            // Write 4-byte changes
            writer.WriteUInt32((uint)fourByteChanges.Count);
			foreach (var change in fourByteChanges)
            {
                writer.WriteUInt32(change.Address);
                writer.WriteBlock(change.Data);
            }

            // Write other changes
            writer.WriteUInt32((uint)otherChanges.Count);
			foreach (var change in otherChanges)
            {
                writer.WriteUInt32(change.Address);
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