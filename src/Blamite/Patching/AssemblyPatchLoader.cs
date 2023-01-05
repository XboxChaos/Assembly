using System;
using System.Collections.Generic;
using System.IO;
using Blamite.IO;

namespace Blamite.Patching
{
	public static class AssemblyPatchLoader
	{
		public static Patch LoadPatch(IReader reader)
		{
			var container = new ContainerReader(reader);
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
						ReadSegmentChanges(reader, container.BlockVersion, result);
						break;

					case "blfc":
						ReadBlfInfo(reader, container.BlockVersion, result);
						break;

						#region Deprecated

					case "meta":
						ReadMetaChanges(reader, container.BlockVersion, result);
						break;

					case "locl":
						ReadLocaleChanges(reader, container.BlockVersion, result);
						break;

						#endregion Deprecated
				}
			}
			return result;
		}

		private static void ReadPatchInfo(IReader reader, byte version, Patch output)
		{
			if (version > 3)
				throw new NotSupportedException("Unrecognized \"titl\" block version");

			// Version 0 (all versions)
			output.MapID = reader.ReadInt32();
			output.MapInternalName = reader.ReadAscii();
			output.Name = reader.ReadUTF16();
			output.Description = reader.ReadUTF16();
			output.Author = reader.ReadUTF16();

			int screenshotLength = reader.ReadInt32();
			if (screenshotLength > 0)
				output.Screenshot = reader.ReadBlock(screenshotLength);

			// Version 1
			if (version >= 1)
			{
				if (version >= 3)
					output.MetaPokeBase = reader.ReadInt64();
				else
					output.MetaPokeBase = reader.ReadUInt32();
				output.MetaChangesIndex = reader.ReadSByte();
			}

			// Version 2
			if (version >= 2)
				output.OutputName = reader.ReadAscii();
			else
				output.OutputName = "";

			// Version 3
			if (version == 3)
			{
				output.PC = reader.ReadByte() == 1;
				output.BuildString = reader.ReadAscii();
			}
			else
				output.BuildString = "";
		}

		private static void ReadSegmentChanges(IReader reader, byte version, Patch output)
		{
			if (version > 0)
				throw new NotSupportedException("Unrecognized \"segm\" block version");

			// Version 0 (all versions)
			byte numChanges = reader.ReadByte();
			for (int i = 0; i < numChanges; i++)
			{
				uint oldOffset = reader.ReadUInt32();
				uint oldSize = reader.ReadUInt32();
				uint newOffset = reader.ReadUInt32();
				uint newSize = reader.ReadUInt32();
				bool resizeAtEnd = Convert.ToBoolean(reader.ReadByte());
				var segmentChange = new SegmentChange(oldOffset, oldSize, newOffset, newSize, resizeAtEnd);
				segmentChange.DataChanges.AddRange(ReadDataChanges(reader));

				output.SegmentChanges.Add(segmentChange);
			}
		}

		private static List<DataChange> ReadDataChanges(IReader reader)
		{
			var result = new List<DataChange>();

			uint numFourByteChanges = reader.ReadUInt32();
			for (int j = 0; j < numFourByteChanges; j++)
			{
				uint offset = reader.ReadUInt32();
				byte[] data = reader.ReadBlock(4);
				result.Add(new DataChange(offset, data));
			}

			uint numOtherChanges = reader.ReadUInt32();
			for (int j = 0; j < numOtherChanges; j++)
			{
				uint offset = reader.ReadUInt32();
				int dataSize = reader.ReadInt32();
				byte[] data = reader.ReadBlock(dataSize);
				result.Add(new DataChange(offset, data));
			}

			return result;
		}

		private static void ReadBlfInfo(IReader reader, byte version, Patch output)
		{
			if (version > 0)
				throw new NotSupportedException("Unrecognized \"blfc\" block version");

			// Version 0 (all versions)
			var targetGame = (TargetGame) reader.ReadByte();
			string mapInfoFileName = reader.ReadAscii();
			uint mapInfoLength = reader.ReadUInt32();
			byte[] mapInfo = reader.ReadBlock((int) mapInfoLength);
			short blfContainerCount = reader.ReadInt16();
			output.CustomBlfContent = new BlfContent(mapInfoFileName, mapInfo, targetGame);
			for (int i = 0; i < blfContainerCount; i++)
			{
				string fileName = Path.GetFileName(reader.ReadAscii());
				uint blfContainerLength = reader.ReadUInt32();
				byte[] blfContainer = reader.ReadBlock((int) blfContainerLength);

				output.CustomBlfContent.BlfContainerEntries.Add(new BlfContainerEntry(fileName, blfContainer));
			}
		}

		#region Deprecated

		private static void ReadMetaChanges(IReader reader, byte version, Patch output)
		{
			if (version > 0)
				throw new NotSupportedException("Unrecognized \"meta\" block version");

			output.MetaChanges.AddRange(ReadDataChanges(reader));
		}

		private static void ReadLocaleChanges(IReader reader, byte version, Patch output)
		{
			if (version > 0)
				throw new NotSupportedException("Unrecognized \"locl\" block version");

			// Read language changes
			byte numLanguageChanges = reader.ReadByte();
			for (byte i = 0; i < numLanguageChanges; i++)
			{
				byte languageIndex = reader.ReadByte();
				var languageChange = new LanguageChange(languageIndex);

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

		#endregion Deprecated
	}
}