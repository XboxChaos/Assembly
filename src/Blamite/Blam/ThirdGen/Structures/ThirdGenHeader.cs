using System.Collections.Generic;
using System.Linq;
using Blamite.Serialization;
using Blamite.IO;
using System;

namespace Blamite.Blam.ThirdGen.Structures
{
	/// <summary>
	///     A cache file header whose layout can be changed.
	/// </summary>
	public class ThirdGenHeader
	{
		private FileSegment _eofSegment;
		public int cacheSegmentAlignment;
		public int tagSegmentAlignment;
		private IPointerExpander _expander;

		public ThirdGenHeader(StructureValueCollection values, EngineDescription info, FileSegmenter segmenter,
			IPointerExpander expander)
		{
			BuildString = info.BuildVersion;
			HeaderSize = info.HeaderSize;
			cacheSegmentAlignment = info.SegmentAlignment;
			tagSegmentAlignment = info.TagSegmentAlignment;
			_expander = expander;
			Load(values, segmenter);
		}

		public int HeaderSize { get; private set; }

		public long FileSize
		{
			get { return _eofSegment.Offset; }
		}

		public CacheFileType Type { get; private set; }
		public string InternalName { get; private set; }
		public string ScenarioName { get; private set; }
		public string BuildString { get; private set; }
		public int XDKVersion { get; set; }

		public FileSegmentGroup MetaArea { get; private set; }
		public SegmentPointer IndexHeaderLocation { get; set; }
		public Partition[] Partitions { get; private set; }

		public FileSegment RawTable { get; private set; }

		public BasedPointerConverter DebugPointerConverter { get; private set; }
		public BasedPointerConverter ResourcePointerConverter { get; private set; }
		public BasedPointerConverter TagBufferPointerConverter { get; private set; }
		public BasedPointerConverter LocalePointerConverter { get; private set; }

		public FileSegmentGroup StringArea { get; private set; }

		public int StringIDCount { get; set; }
		public FileSegment StringIDIndexTable { get; private set; }
		public SegmentPointer StringIDIndexTableLocation { get; set; }
		public FileSegment StringIDData { get; private set; }
		public SegmentPointer StringIDDataLocation { get; set; }

		public int StringIDNamespaceCount { get; set; }
		public FileSegment StringIDNamespaceTable { get; private set; }
		public SegmentPointer StringIDNamespaceTableLocation { get; set; }

		public FileSegment StringBlock { get; private set; }
		public SegmentPointer StringBlockLocation { get; set; }

		public int FileNameCount { get; set; }
		public FileSegment FileNameIndexTable { get; private set; }
		public SegmentPointer FileNameIndexTableLocation { get; set; }
		public FileSegment FileNameData { get; private set; }
		public SegmentPointer FileNameDataLocation { get; set; }

		public int UnknownCount { get; set; }
		public FileSegment UnknownTable { get; private set; }
		public SegmentPointer UnknownTableLocation { get; set; }

		public uint[] SectionOffsetMasks { get; private set; }
		public ThirdGenInteropSection[] Sections { get; private set; }

		public uint Checksum { get; set; }

		/// <summary>
		///     Serializes the header's values, storing them into a StructureValueCollection.
		/// </summary>
		/// <param name="localeArea">The locale area of the cache file. Can be null.</param>
		/// <param name="localePointerMask">The value to add to locale pointers to translate them to file offsets.</param>
		/// <returns>The resulting StructureValueCollection.</returns>
		public StructureValueCollection Serialize(FileSegmentGroup localeArea)
		{
			var values = new StructureValueCollection();

			values.SetInteger("file size", (ulong)FileSize);
			values.SetInteger("type", (uint) Type);
			values.SetString("internal name", InternalName);
			values.SetString("scenario name", ScenarioName);
			values.SetInteger("xdk version", (uint) XDKVersion);

			AdjustPartitions();
			values.SetArray("partitions", SerializePartitions());

			RebuildInteropData(localeArea);

			values.SetArray("offset masks", SectionOffsetMasks.Select(m =>
			{
				var result = new StructureValueCollection();
				result.SetInteger("mask", m);
				return result;
			}).ToArray());

			values.SetArray("sections", Sections.Select(s => s.Serialize()).ToArray());
			values.SetInteger("tag buffer offset", Sections[(int) ThirdGenInteropSectionType.Tag].VirtualAddress);

			if (MetaArea != null)
			{
				values.SetInteger("virtual base address", (ulong)MetaArea.BasePointer);
				values.SetInteger("index header address", (ulong)IndexHeaderLocation.AsPointer());
				values.SetInteger("virtual size", (uint) MetaArea.Size);
			}

			if (StringBlockLocation != null)
				values.SetInteger("string block offset", (ulong)StringBlockLocation.AsPointer());

			values.SetInteger("string table count", (uint) StringIDCount);
			if (StringIDData != null)
			{
				values.SetInteger("string table size", (uint) StringIDData.Size);
				values.SetInteger("string table offset", (ulong)StringIDDataLocation.AsPointer());
			}

			if (StringIDNamespaceTable != null)
			{
				values.SetInteger("string namespace table count", (uint)StringIDNamespaceCount);
				values.SetInteger("string namespace table offset", (uint)StringIDNamespaceTableLocation.AsPointer());
			}

			if (StringIDIndexTableLocation != null)
				values.SetInteger("string index table offset", (ulong)StringIDIndexTableLocation.AsPointer());

			if (StringArea != null)
				values.SetInteger("string data size", (uint) StringArea.Size);

			values.SetInteger("file table count", (uint) FileNameCount);
			if (FileNameData != null)
			{
				values.SetInteger("file table offset", (ulong)FileNameDataLocation.AsPointer());
				values.SetInteger("file table size", (uint) FileNameData.Size);
			}

			if (FileNameIndexTableLocation != null)
				values.SetInteger("file index table offset", (ulong)FileNameIndexTableLocation.AsPointer());

			if (localeArea != null)
			{
				values.SetInteger("locale data index offset", (uint)localeArea.BasePointer);
				values.SetInteger("locale data size", (uint) localeArea.Size);
			}

			if (UnknownTableLocation != null)
			{
				values.SetInteger("unknown table count", (uint) UnknownCount);
				values.SetInteger("unknown table offset", (ulong)UnknownTableLocation.AsPointer());
			}

			values.SetInteger("checksum", Checksum);
			return values;
		}

		private void AdjustPartitions()
		{
			if (MetaArea == null)
				return;

			// Find the first partition and change it to the meta area's base address
			Partition partition = Partitions.First();
			if (partition != null)
				partition.BasePointer = SegmentPointer.FromPointer(MetaArea.BasePointer, MetaArea);

			// Recalculate the size of each partition
			uint partitionEnd = MetaArea.Offset + MetaArea.Size;
			for (int i = Partitions.Length - 1; i >= 0; i--)
			{
				if (Partitions[i].BasePointer == null)
					continue;

				uint offset = Partitions[i].BasePointer.AsOffset();
				Partitions[i].Size = (uint) (partitionEnd - offset);
				partitionEnd = offset;
			}
		}

		private StructureValueCollection[] SerializePartitions()
		{
			if (Partitions == null)
				return new StructureValueCollection[0];

			var results = new StructureValueCollection[Partitions.Length];
			for (int i = 0; i < Partitions.Length; i++)
			{
				var values = new StructureValueCollection();
				values.SetInteger("load address", Partitions[i].BasePointer != null ? (ulong)Partitions[i].BasePointer.AsPointer() : 0);
				values.SetInteger("size", Partitions[i].Size);
				results[i] = values;
			}
			return results;
		}

		/// <summary>
		///     Rebuilds the interop data table in a cache file.
		/// </summary>
		/// <param name="localeArea">The localization area of the file.</param>
		private void RebuildInteropData(FileSegmentGroup localeArea)
		{
			ThirdGenInteropSection debugSection = Sections[(int) ThirdGenInteropSectionType.Debug];
			ThirdGenInteropSection rsrcSection = Sections[(int) ThirdGenInteropSectionType.Resource];
			ThirdGenInteropSection tagSection = Sections[(int) ThirdGenInteropSectionType.Tag];
			ThirdGenInteropSection localeSection = Sections[(int) ThirdGenInteropSectionType.Localization];

			// Recompute base addresses
			// Section addresses are usually in the following order: resource, locale, tag, debug.
			// Each address can immediately follow after the previous non-null section,
			// even though this isn't the case in some of the official files (because of removed debug data).
			//
			// TODO: This could possibly be made into a for loop and cleaned up if the pointer converters are stored in an array.
			// I just want to get this working for now.
			//rsrcSection.VirtualAddress = 0; // This is (not) always zero

			if (cacheSegmentAlignment == 0x800 || cacheSegmentAlignment == 0x200)
				rsrcSection.VirtualAddress = 0x800; // hax for h3b saving, sections aren't in the same order as final
			rsrcSection.Size = (ResourcePointerConverter != null) ? (uint) RawTable.Size : 0;
			localeSection.VirtualAddress = (LocalePointerConverter != null) ? rsrcSection.VirtualAddress + rsrcSection.Size : 0;
			localeSection.Size = (LocalePointerConverter != null) ? (uint) localeArea.Size : 0;
			tagSection.VirtualAddress = (TagBufferPointerConverter != null)
				? rsrcSection.VirtualAddress + rsrcSection.Size + localeSection.Size
				: 0;
			tagSection.Size = (TagBufferPointerConverter != null) ? (uint) MetaArea.Size : 0;
			debugSection.VirtualAddress = (DebugPointerConverter != null)
				? rsrcSection.VirtualAddress + rsrcSection.Size + localeSection.Size + tagSection.Size
				: 0;
			debugSection.Size = (DebugPointerConverter != null) ? (uint) StringArea.Size : 0;

			// If the offset mask for the debug section wasn't originally zero, then we have to subtract the first partition size from the debug base address
			// Not entirely sure why this is the case, but that's what the official files do
			if (debugSection.VirtualAddress != 0 && SectionOffsetMasks[(int) ThirdGenInteropSectionType.Debug] != 0)
				debugSection.VirtualAddress -= Partitions[0].Size;

			// Recompute offset masks
			SectionOffsetMasks[(int) ThirdGenInteropSectionType.Debug] = (debugSection.Size > 0)
				? (uint) (StringArea.Offset - debugSection.VirtualAddress)
				: 0;
			SectionOffsetMasks[(int) ThirdGenInteropSectionType.Resource] = (rsrcSection.Size > 0)
				? (uint) (RawTable.Offset - rsrcSection.VirtualAddress)
				: 0;
			SectionOffsetMasks[(int) ThirdGenInteropSectionType.Tag] = (tagSection.Size > 0)
				? (uint) (MetaArea.Offset - tagSection.VirtualAddress)
				: 0;
			SectionOffsetMasks[(int) ThirdGenInteropSectionType.Localization] = (localeSection.Size > 0)
				? (uint) (localeArea.Offset - localeSection.VirtualAddress)
				: 0;

			// Update pointer converters
			if (DebugPointerConverter != null)
				DebugPointerConverter.BasePointer = debugSection.VirtualAddress;
			if (ResourcePointerConverter != null)
				ResourcePointerConverter.BasePointer = rsrcSection.VirtualAddress;
			if (TagBufferPointerConverter != null)
				TagBufferPointerConverter.BasePointer = tagSection.VirtualAddress;
			if (LocalePointerConverter != null)
				LocalePointerConverter.BasePointer = localeSection.VirtualAddress;
		}

		private void Load(StructureValueCollection values, FileSegmenter segmenter)
		{
			if (values.HasArray("compressed section types"))
			{
				foreach (var type in values.GetArray("compressed section types"))
				{
					if ((int)type.GetInteger("type") > 0)
						throw new ArgumentException("Map claims to be compressed. Please decompress it using the Tools menu before trying to load it again.");
				}	
			}

			segmenter.DefineSegment(0, (uint)HeaderSize, 1, SegmentResizeOrigin.Beginning); // Define a segment for the header
			_eofSegment = segmenter.WrapEOF((uint) values.GetInteger("file size"));

			Type = (CacheFileType)values.GetInteger("type");

			LoadInteropData(values);
			RawTable = CalculateRawTableSegment(segmenter);

			InternalName = values.GetString("internal name");
			ScenarioName = values.GetString("scenario name");
			XDKVersion = (int) values.GetInteger("xdk version");

			FileSegment metaSegment = CalculateTagDataSegment(values, segmenter);
			if (metaSegment != null)
			{
				ulong virtualBase = values.GetInteger("virtual base address");
				MetaArea = new FileSegmentGroup(new MetaAddressConverter(metaSegment, (long)virtualBase));
				MetaArea.AddSegment(metaSegment);

				IndexHeaderLocation = SegmentPointer.FromPointer((long)values.GetInteger("index header address"), MetaArea);
				Partitions = LoadPartitions(values.GetArray("partitions"));
			}
			else
			{
				Partitions = new Partition[0];
			}

			CalculateStringGroup(values, segmenter);

			Checksum = (uint)values.GetIntegerOrDefault("checksum", 0);
		}

		private void LoadInteropData(StructureValueCollection headerValues)
		{
			// TODO: fix this shit for the h3beta better
			if (headerValues.HasArray("offset masks") && headerValues.HasArray("sections"))
			{
				SectionOffsetMasks = headerValues.GetArray("offset masks").Select(v => (uint)v.GetInteger("mask")).ToArray();
				Sections = headerValues.GetArray("sections").Select(v => new ThirdGenInteropSection(v)).ToArray();
				
				// H3 MCC currently doesn't store section data for campaign/shared, so it must be hacked together
				if (_expander.IsValid && (Type == CacheFileType.Shared || Type == CacheFileType.SinglePlayerShared))
				{
					if (Sections[(int)ThirdGenInteropSectionType.Resource].Size == 0)
					{
						Sections[(int)ThirdGenInteropSectionType.Resource].VirtualAddress = (uint)HeaderSize;
						Sections[(int)ThirdGenInteropSectionType.Resource].Size = (uint)FileSize - (uint)HeaderSize;
					}
				}
			}
			else //else hack up our own sections
			{
				SectionOffsetMasks = new uint[] { 0, 0, 0, 0 };

				ThirdGenInteropSection debugSection = new ThirdGenInteropSection(
					(uint)headerValues.GetInteger("string index table offset"),
					(uint)(headerValues.GetInteger("file size") - headerValues.GetInteger("string index table offset")));
				ThirdGenInteropSection resourceSection = new ThirdGenInteropSection(0, 0); // this is between locales and tag, so if we had a locale size, this section could be calculated. Using 0's for now seems to work at least
				ThirdGenInteropSection tagSection = new ThirdGenInteropSection(
					(uint)headerValues.GetInteger("tag buffer offset"),
					(uint)headerValues.GetInteger("virtual size"));
				ThirdGenInteropSection localeSection = new ThirdGenInteropSection(
					(uint)HeaderSize,
					(uint)headerValues.GetInteger("tag buffer offset")); //bs the size for now

				Sections = new ThirdGenInteropSection[] { debugSection, resourceSection, tagSection, localeSection };
			}

			DebugPointerConverter = MakePointerConverter(ThirdGenInteropSectionType.Debug);
			ResourcePointerConverter = MakePointerConverter(ThirdGenInteropSectionType.Resource);
			TagBufferPointerConverter = MakePointerConverter(ThirdGenInteropSectionType.Tag);
			LocalePointerConverter = MakePointerConverter(ThirdGenInteropSectionType.Localization);
		}

		private BasedPointerConverter MakePointerConverter(ThirdGenInteropSectionType section)
		{
			if (Sections[(int) section].Size == 0)
				return null;

			uint baseAddress = Sections[(int) section].VirtualAddress;
			uint mask = SectionOffsetMasks[(int) section];
			return new BasedPointerConverter(baseAddress, (int) (baseAddress + mask));
		}

		private FileSegment CalculateRawTableSegment(FileSegmenter segmenter)
		{
			if (ResourcePointerConverter != null)
			{
				uint rawTableOffset = ResourcePointerConverter.PointerToOffset(ResourcePointerConverter.BasePointer);
				var rawTableSize = (uint) Sections[(int) ThirdGenInteropSectionType.Resource].Size;
				return segmenter.WrapSegment(rawTableOffset, rawTableSize, cacheSegmentAlignment, SegmentResizeOrigin.End);
			}
			return null;
		}

		private FileSegment CalculateTagDataSegment(StructureValueCollection values, FileSegmenter segmenter)
		{
			if (TagBufferPointerConverter != null)
			{
				uint tagDataOffset = TagBufferPointerConverter.PointerToOffset(TagBufferPointerConverter.BasePointer);
				var tagDataSize = (uint) values.GetInteger("virtual size");
				return segmenter.WrapSegment(tagDataOffset, tagDataSize, tagSegmentAlignment, SegmentResizeOrigin.Beginning);
			}
			return null;
		}

		private void CalculateStringGroup(StructureValueCollection values, FileSegmenter segmenter)
		{
			if (DebugPointerConverter == null)
				return;

			StringArea = new FileSegmentGroup(DebugPointerConverter);

			// StringIDs
			StringIDCount = (int) values.GetInteger("string table count");
			if (StringIDCount > 0)
			{
				uint sidIndexTableOff = DebugPointerConverter.PointerToOffset((uint)values.GetInteger("string index table offset"));
				uint sidDataOff = DebugPointerConverter.PointerToOffset((uint)values.GetInteger("string table offset"));

				uint sidTableSize = (uint) values.GetInteger("string table size");
				StringIDIndexTable = segmenter.WrapSegment(sidIndexTableOff, (uint)StringIDCount*4, 4, SegmentResizeOrigin.End);
				StringIDData = segmenter.WrapSegment(sidDataOff, sidTableSize, 1, SegmentResizeOrigin.End);

				StringIDIndexTableLocation = StringArea.AddSegment(StringIDIndexTable);
				StringIDDataLocation = StringArea.AddSegment(StringIDData);

				// idk what this is, but H3Beta has it
				if (values.HasInteger("string block offset"))
				{
					uint sidBlockOff = DebugPointerConverter.PointerToOffset((uint)values.GetInteger("string block offset"));
					StringBlock = segmenter.WrapSegment(sidBlockOff, (uint)StringIDCount*0x80, 0x80, SegmentResizeOrigin.End);
					StringBlockLocation = StringArea.AddSegment(StringBlock);
				}

				// newest reach mcc caches store namespace information, hopefully others follow because thats one less thing to worry about every update
				if (values.HasInteger("string namespace table count"))
				{
					StringIDNamespaceCount = (int)values.GetInteger("string namespace table count");
					if (StringIDNamespaceCount > 0)
					{
						uint namespaceTableOff = DebugPointerConverter.PointerToOffset((uint)values.GetInteger("string namespace table offset"));

						StringIDNamespaceTable = segmenter.WrapSegment(namespaceTableOff, (uint)StringIDNamespaceCount * 4, 4, SegmentResizeOrigin.End);

						StringIDNamespaceTableLocation = StringArea.AddSegment(StringIDNamespaceTable);
					}

				}
			}

			// Tag names
			FileNameCount = (int) values.GetInteger("file table count");
			if (FileNameCount > 0)
			{
				uint nameIndexTableOff = DebugPointerConverter.PointerToOffset((uint)values.GetInteger("file index table offset"));
				uint nameDataOff = DebugPointerConverter.PointerToOffset((uint)values.GetInteger("file table offset"));

				var fileTableSize = (uint) values.GetInteger("file table size");
				FileNameIndexTable = segmenter.WrapSegment(nameIndexTableOff, (uint)FileNameCount*4, 4, SegmentResizeOrigin.End);
				FileNameData = segmenter.WrapSegment(nameDataOff, fileTableSize, 1, SegmentResizeOrigin.End);

				FileNameIndexTableLocation = StringArea.AddSegment(FileNameIndexTable);
				FileNameDataLocation = StringArea.AddSegment(FileNameData);
			}

			// Some H4-only unknown table
			if (values.HasInteger("unknown table count") && values.HasInteger("unknown table offset"))
			{
				UnknownCount = (int) values.GetInteger("unknown table count");
				if (UnknownCount > 0)
				{
					uint unknownOff = DebugPointerConverter.PointerToOffset((uint)values.GetInteger("unknown table offset"));
					UnknownTable = segmenter.WrapSegment(unknownOff, (uint)UnknownCount*0x10, 0x10, SegmentResizeOrigin.End);
					UnknownTableLocation = StringArea.AddSegment(UnknownTable);
				}
			}
		}

		private Partition[] LoadPartitions(StructureValueCollection[] partitionValues)
		{
			IEnumerable<Partition> result = from partition in partitionValues
				select new Partition
					(
					partition.GetInteger("load address") != 0
						? SegmentPointer.FromPointer((long)partition.GetInteger("load address"), MetaArea)
						: null,
					(uint)partition.GetInteger("size")
					);
			return result.ToArray();
		}
	}
}