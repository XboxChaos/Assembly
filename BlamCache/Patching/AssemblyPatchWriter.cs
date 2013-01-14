using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Patching
{
    public static class AssemblyPatchWriter
    {
        public static void WritePatch(Patch patch, IWriter writer)
        {
            writer.WriteInt32(AssemblyPatchMagic);
            writer.WriteUInt32(0); // File size filled in later
            writer.WriteByte(0); // No compression

            WriteBlocks(patch, writer);

            // Fill in the file size
            long fileSize = writer.Position;
            writer.SeekTo(4);
            writer.WriteUInt32((uint)fileSize - 8);
            writer.SeekTo(fileSize);
        }

        private static void WriteBlocks(Patch patch, IWriter writer)
        {
            WritePatchInfo(patch, writer);
            WriteMetaChanges(patch, writer);
        }

        private static void WritePatchInfo(Patch patch, IWriter writer)
        {
            long startPos = writer.Position;
            writer.WriteInt32(AssemblyPatchBlockID.Titl);
            writer.WriteUInt32(0); // Size filled in later
            writer.WriteByte(0); // Version 0

            writer.WriteInt32(patch.MapID);
            if (patch.MapInternalName != null)
                writer.WriteAscii(patch.MapInternalName);
            else
                writer.WriteByte(0);
            writer.WriteUTF16(patch.Name);
            writer.WriteUTF16(patch.Description);
            writer.WriteUTF16(patch.Author);
            if (patch.Screenshot != null)
            {
                writer.WriteInt32(patch.Screenshot.Length);
                writer.WriteBlock(patch.Screenshot);
            }
            else
            {
                writer.WriteInt32(0);
            }

            // Fill in the block size
            long endPos = writer.Position;
            writer.SeekTo(startPos + 4);
            writer.WriteUInt32((uint)(endPos - startPos));
            writer.SeekTo(endPos);
        }

        private static void WriteMetaChanges(Patch patch, IWriter writer)
        {
            long startPos = writer.Position;
            writer.WriteInt32(AssemblyPatchBlockID.Meta);
            writer.WriteUInt32(0); // Size filled in later
            writer.WriteByte(0); // Version 0

            // Filter meta changes by size (as a file size optimization)
            List<MetaChange> fourByteChanges = new List<MetaChange>();
            List<MetaChange> otherChanges = new List<MetaChange>();
            foreach (MetaChange change in patch.MetaChanges)
            {
                if (change.Data.Length == 4)
                    fourByteChanges.Add(change);
                else
                    otherChanges.Add(change);
            }

            // Write 4-byte changes
            writer.WriteUInt32((uint)fourByteChanges.Count);
            foreach (MetaChange change in fourByteChanges)
            {
                writer.WriteUInt32(change.Address);
                writer.WriteBlock(change.Data);
            }

            // Write other changes
            writer.WriteUInt32((uint)otherChanges.Count);
            foreach (MetaChange change in otherChanges)
            {
                writer.WriteUInt32(change.Address);
                writer.WriteUInt32((uint)change.Data.Length);
                writer.WriteBlock(change.Data);
            }

            // Fill in the block size
            long endPos = writer.Position;
            writer.SeekTo(startPos + 4);
            writer.WriteUInt32((uint)(endPos - startPos));
            writer.SeekTo(endPos);
        }

        private const int AssemblyPatchMagic = 0x61736D70;
    }
}
