using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.IO;

namespace Blamite.Patching
{
	public static class AssemblyPatchWriter
	{
		public static void WritePatch(Patch patch, IWriter writer)
		{
			var container = new ContainerWriter(writer);
			container.StartBlock("asmp", 0);
			WriteBlocks(patch, container, writer);
			container.EndBlock();
		}

		private static void WriteBlocks(Patch patch, ContainerWriter container, IWriter writer)
		{
			WritePatchInfo(patch, container, writer);
			WriteSegmentChanges(patch, container, writer);
			WriteBlfInfo(patch, container, writer);

			#region Deprecated

			WriteMetaChanges(patch, container, writer);
			WriteLocaleChanges(patch, container, writer);

			#endregion Deprecated
		}

		private static void WritePatchInfo(Patch patch, ContainerWriter container, IWriter writer)
		{
			container.StartBlock("titl", 3); // Version 2

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
			writer.WriteInt64(patch.MetaPokeBase);
			writer.WriteSByte((sbyte) patch.MetaChangesIndex);

			// Write output name
			if (patch.OutputName != null)
				writer.WriteAscii(patch.OutputName);
			else
				writer.WriteByte(0);

			// PC?
			if (patch.PC)
				writer.WriteByte(1);
			else
				writer.WriteByte(0);

			// Write the build string
			if (patch.BuildString != null)
				writer.WriteAscii(patch.BuildString);
			else
				writer.WriteByte(0);

			container.EndBlock();
		}

		private static void WriteSegmentChanges(Patch patch, ContainerWriter container, IWriter writer)
		{
			if (patch.SegmentChanges.Count == 0)
				return;

			container.StartBlock("segm", 0); // Version 0

			writer.WriteByte((byte) patch.SegmentChanges.Count);
			foreach (SegmentChange segment in patch.SegmentChanges)
			{
				writer.WriteUInt32(segment.OldOffset);
				writer.WriteUInt32(segment.OldSize);
				writer.WriteUInt32(segment.NewOffset);
				writer.WriteUInt32(segment.NewSize);
				writer.WriteByte(Convert.ToByte(segment.ResizeAtEnd));

				WriteDataChanges(segment.DataChanges, writer);
			}

			container.EndBlock();
		}

		private static void WriteDataChanges(IList<DataChange> changes, IWriter writer)
		{
			List<DataChange> fourByteChanges = changes.Where(c => c.Data.Length == 4).ToList();
			List<DataChange> otherChanges = changes.Where(c => c.Data.Length != 4).ToList();

			// Write 4-byte changes
			writer.WriteUInt32((uint) fourByteChanges.Count);
			foreach (DataChange change in fourByteChanges)
			{
				writer.WriteUInt32(change.Offset);
				writer.WriteBlock(change.Data);
			}

			// Write other changes
			writer.WriteUInt32((uint) otherChanges.Count);
			foreach (DataChange change in otherChanges)
			{
				writer.WriteUInt32(change.Offset);
				writer.WriteInt32(change.Data.Length);
				writer.WriteBlock(change.Data);
			}
		}

		private static void WriteBlfInfo(Patch patch, ContainerWriter container, IWriter writer)
		{
			if (patch.CustomBlfContent == null)
				return;

			container.StartBlock("blfc", 0); // Version 0

			writer.WriteByte((byte) patch.CustomBlfContent.TargetGame);

			// Write mapinfo filename
			if (patch.CustomBlfContent.MapInfoFileName != null)
				writer.WriteAscii(patch.CustomBlfContent.MapInfoFileName);
			else
				writer.WriteByte(0);

			// Write mapinfo data
			if (patch.CustomBlfContent.MapInfo != null)
			{
				writer.WriteUInt32((uint) patch.CustomBlfContent.MapInfo.Length);
				writer.WriteBlock(patch.CustomBlfContent.MapInfo);
			}
			else
			{
				writer.WriteUInt32(0);
			}

			// Write BLF containers
			writer.WriteInt16((short) patch.CustomBlfContent.BlfContainerEntries.Count);
			foreach (BlfContainerEntry blfContainerEntry in patch.CustomBlfContent.BlfContainerEntries)
			{
				writer.WriteAscii(blfContainerEntry.FileName);
				writer.WriteUInt32((uint) blfContainerEntry.BlfContainer.Length);
				writer.WriteBlock(blfContainerEntry.BlfContainer);
			}

			container.EndBlock();
		}

		#region Deprecated

		private static void WriteMetaChanges(Patch patch, ContainerWriter container, IWriter writer)
		{
			if (patch.MetaChanges.Count == 0)
				return;

			container.StartBlock("meta", 0); // Version 0

			List<DataChange> fourByteChanges = patch.MetaChanges.Where(c => c.Data.Length == 4).ToList();
			List<DataChange> otherChanges = patch.MetaChanges.Where(c => c.Data.Length != 4).ToList();

			// Write 4-byte changes
			writer.WriteUInt32((uint) fourByteChanges.Count);
			foreach (DataChange change in fourByteChanges)
			{
				writer.WriteUInt32(change.Offset);
				writer.WriteBlock(change.Data);
			}

			// Write other changes
			writer.WriteUInt32((uint) otherChanges.Count);
			foreach (DataChange change in otherChanges)
			{
				writer.WriteUInt32(change.Offset);
				writer.WriteInt32(change.Data.Length);
				writer.WriteBlock(change.Data);
			}

			container.EndBlock();
		}

		private static void WriteLocaleChanges(Patch patch, ContainerWriter container, IWriter writer)
		{
			if (patch.LanguageChanges.Count == 0)
				return;

			container.StartBlock("locl", 0); // Version 0

			// Write change data for each language
			writer.WriteByte((byte) patch.LanguageChanges.Count);
			foreach (LanguageChange language in patch.LanguageChanges)
			{
				writer.WriteByte(language.LanguageIndex);

				// Write the change data for each string in the language
				writer.WriteInt32(language.LocaleChanges.Count);
				foreach (LocaleChange change in language.LocaleChanges)
				{
					writer.WriteInt32(change.Index);
					writer.WriteUTF8(change.NewValue);
				}
			}

			container.EndBlock();
		}

		#endregion Deprecated
	}
}