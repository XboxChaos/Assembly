using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.SecondGen.Structures
{
	public class SecondGenHeader
	{
		private FileSegment _eofSegment;

		public SecondGenHeader(StructureValueCollection values, EngineDescription info, string buildString,
			FileSegmenter segmenter)
		{
			BuildString = buildString;
			HeaderSize = info.HeaderSize;
			Load(values, segmenter);
		}

		public int HeaderSize { get; private set; }

		public uint FileSize
		{
			get { return (uint) _eofSegment.Offset; }
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

		public FileSegmentGroup LocaleArea { get; private set; }
		public FileSegmentGroup StringArea { get; private set; }

		public int StringIDCount { get; set; }
		public FileSegment StringIDIndexTable { get; set; }
		public SegmentPointer StringIDIndexTableLocation { get; set; }
		public FileSegment StringIDData { get; set; }
		public SegmentPointer StringIDDataLocation { get; set; }

		public int FileNameCount { get; set; }
		public FileSegment FileNameIndexTable { get; set; }
		public SegmentPointer FileNameIndexTableLocation { get; set; }
		public FileSegment FileNameData { get; set; }
		public SegmentPointer FileNameDataLocation { get; set; }

		public uint Checksum { get; set; }

		public StructureValueCollection Serialize()
		{
			var result = new StructureValueCollection();
			result.SetInteger("file size", FileSize);
			result.SetInteger("meta offset", (uint) MetaArea.Offset);
			result.SetInteger("meta size", (uint) MetaArea.Size);
			result.SetInteger("meta offset mask", MetaArea.BasePointer);
			result.SetInteger("type", (uint) Type);
			result.SetInteger("string table count", (uint) StringIDCount);
			result.SetInteger("string table size", (uint) StringIDData.Size);
			result.SetInteger("string index table offset", (uint) StringIDIndexTable.Offset);
			result.SetInteger("string table offset", (uint) StringIDData.Offset);
			result.SetString("internal name", InternalName);
			result.SetString("scenario name", ScenarioName);
			result.SetInteger("file table count", (uint) FileNameCount);
			result.SetInteger("file table offset", (uint) FileNameData.Offset);
			result.SetInteger("file table size", (uint) FileNameData.Size);
			result.SetInteger("file index table offset", (uint) FileNameIndexTable.Offset);
			result.SetInteger("raw table offset", (uint) RawTable.Offset);
			result.SetInteger("raw table size", (uint) RawTable.Size);
			result.SetInteger("checksum", Checksum);
			return result;
		}

		private void Load(StructureValueCollection values, FileSegmenter segmenter)
		{
			_eofSegment = segmenter.WrapEOF((int) values.GetInteger("file size"));

			var metaOffset = (int) values.GetInteger("meta offset");
			var metaSize = (int) values.GetInteger("meta size");
			uint metaOffsetMask = values.GetInteger("meta offset mask");

			var metaSegment = new FileSegment(
				segmenter.DefineSegment(metaOffset, metaSize, 0x200, SegmentResizeOrigin.Beginning), segmenter);
			MetaArea = new FileSegmentGroup(new MetaOffsetConverter(metaSegment, metaOffsetMask));
			IndexHeaderLocation = MetaArea.AddSegment(metaSegment);

			Type = (CacheFileType) values.GetInteger("type");

			var headerGroup = new FileSegmentGroup();
			headerGroup.AddSegment(segmenter.WrapSegment(0, HeaderSize, 1, SegmentResizeOrigin.None));

			StringIDCount = (int) values.GetInteger("string table count");
			var sidDataSize = (int) values.GetInteger("string table size");
			StringIDData = segmenter.WrapSegment((int) values.GetInteger("string table offset"), sidDataSize, 1,
				SegmentResizeOrigin.End);
			StringIDIndexTable = segmenter.WrapSegment((int) values.GetInteger("string index table offset"), StringIDCount*4, 4,
				SegmentResizeOrigin.End);

			FileNameCount = (int) values.GetInteger("file table count");
			var fileDataSize = (int) values.GetInteger("file table size");
			FileNameData = segmenter.WrapSegment((int) values.GetInteger("file table offset"), fileDataSize, 1,
				SegmentResizeOrigin.End);
			FileNameIndexTable = segmenter.WrapSegment((int) values.GetInteger("file index table offset"), FileNameCount*4, 4,
				SegmentResizeOrigin.End);

			InternalName = values.GetString("internal name");
			ScenarioName = values.GetString("scenario name");

			StringArea = new FileSegmentGroup();
			StringArea.AddSegment(segmenter.WrapSegment((int) values.GetInteger("string block offset"), StringIDCount*0x80, 0x80,
				SegmentResizeOrigin.End));
			StringArea.AddSegment(StringIDIndexTable);
			StringArea.AddSegment(StringIDData);
			StringArea.AddSegment(FileNameIndexTable);
			StringArea.AddSegment(FileNameData);

			StringIDIndexTableLocation = SegmentPointer.FromOffset(StringIDIndexTable.Offset, StringArea);
			StringIDDataLocation = SegmentPointer.FromOffset(StringIDData.Offset, StringArea);
			FileNameIndexTableLocation = SegmentPointer.FromOffset(FileNameIndexTable.Offset, StringArea);
			FileNameDataLocation = SegmentPointer.FromOffset(FileNameData.Offset, StringArea);

			LocaleArea = new FileSegmentGroup();

			var rawTableOffset = (int) values.GetInteger("raw table offset");
			var rawTableSize = (int) values.GetInteger("raw table size");
			RawTable = segmenter.WrapSegment(rawTableOffset, rawTableSize, 1, SegmentResizeOrigin.End);

			Checksum = values.GetInteger("checksum");

			// Set up a bogus partition table
			Partitions = new Partition[1];
			Partitions[0] = new Partition(SegmentPointer.FromOffset(MetaArea.Offset, MetaArea), (uint) MetaArea.Size);
		}
	}
}