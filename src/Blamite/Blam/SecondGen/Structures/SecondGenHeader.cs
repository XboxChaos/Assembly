using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.SecondGen.Structures
{
	public class SecondGenHeader
	{
		private FileSegment _eofSegment;

		public SecondGenHeader(StructureValueCollection values, EngineDescription info, FileSegmenter segmenter)
		{
			BuildString = info.BuildVersion;
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

		private uint _saved_meta_size_hack = 0;

		public StructureValueCollection Serialize()
		{
			var result = new StructureValueCollection();
			result.SetInteger("file size", FileSize);
			result.SetInteger("meta offset", (uint) MetaArea.Offset);
			result.SetInteger("tag data offset", (uint)MetaArea.Segments[0].Size);
			result.SetInteger("tag data size", (uint)MetaArea.Segments[1].Size);


			// hack to set a saved "meta size" for ogbox
			// since this value isnt actually the meta size
			// but the console crashes otherwise. idk, needs more research
			if (BuildString == "02.09.27.09809" || BuildString == "02.06.28.07902")
			{
				result.SetInteger("meta size", _saved_meta_size_hack);
			}
			else
			{
				result.SetInteger("meta size", (uint)MetaArea.Size);
			}

			// hack to set meta offset mask to 0
			// technically we could get away without this but im stupid and use
			// a meta offset mask of 0 as an xbox identifier so uh yeah
			if (BuildString == "02.09.27.09809" || BuildString == "02.06.28.07902")
			{
				result.SetInteger("meta offset mask", 0);
			}
			else
			{
				result.SetInteger("meta offset mask", (uint)MetaArea.BasePointer);
			}


			result.SetInteger("type", (uint) Type);
			result.SetInteger("string block offset", (uint) StringArea.Offset);
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
			result.SetInteger("raw table offset", RawTable != null ? (uint)RawTable.Offset : 0xFFFFFFFF);
			result.SetInteger("raw table size", RawTable != null ? (uint)RawTable.Size : 0);
			result.SetInteger("checksum", Checksum);
			return result;
		}

		
		private void Load(StructureValueCollection values, FileSegmenter segmenter)
		{
			_eofSegment = segmenter.WrapEOF((int) values.GetInteger("file size"));

			var metaOffset = (int)values.GetInteger("meta offset");

			int tagTableSize = (int)values.GetInteger("tag data offset");
			int tagDataSize = (int)values.GetInteger("tag data size");

			var headSegment = new FileSegment(
				segmenter.DefineSegment(metaOffset, tagTableSize, 0x1000, SegmentResizeOrigin.Beginning), segmenter);

			int metaSize;
			
			// hack to set meta size on xbox
			if (BuildString == "02.09.27.09809" || BuildString == "02.06.28.07902")
			{
				metaSize = (int)values.GetInteger("tag data offset") + (int)values.GetInteger("tag data size");
				// hack to rewrite the "meta size" value even though its not actually the meta size
				_saved_meta_size_hack = (uint)values.GetInteger("meta size");
			}
			else
			{
				metaSize = (int)values.GetInteger("meta size");
			}


			uint metaOffsetMask;
			// hack to set meta offset mask on xbox
			if (BuildString == "02.09.27.09809")
			{
				// we hacked in a first tag address earlier
				metaOffsetMask = (uint)values.GetInteger("first tag address") - (uint)values.GetInteger("tag data offset");
			}
			else if (BuildString == "02.06.28.07902")
			{
				metaOffsetMask = (uint)values.GetInteger("first tag address") - (uint)values.GetInteger("tag data offset");
			}
			else
			{
				metaOffsetMask = (uint)values.GetInteger("meta offset mask");
			}


			FileSegment metaSegment = null;

			// hack to set segment alignment on xbox
			if (BuildString == "02.09.27.09809" || BuildString == "02.06.28.07902")
            {
				metaSegment = new FileSegment(
					segmenter.DefineSegment(metaOffset + tagTableSize, tagDataSize, 0x4, SegmentResizeOrigin.End), segmenter);
			}
			else
            {
				metaSegment = new FileSegment(
					segmenter.DefineSegment(metaOffset + tagTableSize, tagDataSize, 0x1000, SegmentResizeOrigin.End), segmenter);
			}

			MetaArea = new FileSegmentGroup(new MetaOffsetConverter(headSegment, metaOffsetMask));

			IndexHeaderLocation = MetaArea.AddSegment(headSegment);
			MetaArea.AddSegment(metaSegment);


			Type = (CacheFileType) values.GetInteger("type");

			var headerGroup = new FileSegmentGroup();
			headerGroup.AddSegment(segmenter.WrapSegment(0, HeaderSize, 1, SegmentResizeOrigin.None));

			StringIDCount = (int) values.GetInteger("string table count");
			var sidDataSize = (int) values.GetInteger("string table size");
			StringIDData = segmenter.WrapSegment((int) values.GetInteger("string table offset"), sidDataSize, 1,
				SegmentResizeOrigin.End);
			StringIDIndexTable = segmenter.WrapSegment((int) values.GetInteger("string index table offset"), StringIDCount*4, 4,
				SegmentResizeOrigin.End);


			// TODO (Dragon): beta has no file table fields in the header
			if (BuildString != "02.06.28.07902")
			{
				FileNameCount = (int)values.GetInteger("file table count");
				var fileDataSize = (int)values.GetInteger("file table size");
				FileNameData = segmenter.WrapSegment((int)values.GetInteger("file table offset"), fileDataSize, 1,
					SegmentResizeOrigin.End);
				FileNameIndexTable = segmenter.WrapSegment((int)values.GetInteger("file index table offset"), FileNameCount * 4, 4,
					SegmentResizeOrigin.End);
			}


			InternalName = values.GetString("internal name");
			ScenarioName = values.GetString("scenario name");

			StringArea = new FileSegmentGroup();
			if (values.HasInteger("string block offset"))
				StringArea.AddSegment(segmenter.WrapSegment((int) values.GetInteger("string block offset"), StringIDCount*0x80, 0x80,
					SegmentResizeOrigin.End));
			StringArea.AddSegment(StringIDIndexTable);
			StringArea.AddSegment(StringIDData);

			// TODO (Dragon): beta has no file table
			if (BuildString != "02.06.28.07902")
			{
				StringArea.AddSegment(FileNameIndexTable);
				StringArea.AddSegment(FileNameData);
			}

			StringIDIndexTableLocation = SegmentPointer.FromOffset(StringIDIndexTable.Offset, StringArea);
			StringIDDataLocation = SegmentPointer.FromOffset(StringIDData.Offset, StringArea);

			// TODO (Dragon): beta has no file table fields in the header
			if (BuildString != "02.06.28.07902")
			{
				FileNameIndexTableLocation = SegmentPointer.FromOffset(FileNameIndexTable.Offset, StringArea);
				FileNameDataLocation = SegmentPointer.FromOffset(FileNameData.Offset, StringArea);
			}

			int rawTableOffset;
			int rawTableSize;

			// TODO (Dragon): h2x has no raw table offset. if we spoof one itll be a gnarly linear search
			if (BuildString != "02.09.27.09809" && BuildString != "02.06.28.07902")
			{
				rawTableOffset = (int)values.GetInteger("raw table offset");
				rawTableSize = (int)values.GetInteger("raw table size");
				// It is apparently possible to create a cache without a raw table, but -1 gets written as the offset
				if (rawTableOffset != -1)
					RawTable = segmenter.WrapSegment(rawTableOffset, rawTableSize, 0x80, SegmentResizeOrigin.End);
			}

			Checksum = (uint)values.GetInteger("checksum");

			// Set up a bogus partition table
			Partitions = new Partition[1];
			Partitions[0] = new Partition(SegmentPointer.FromOffset(MetaArea.Offset, MetaArea), (uint) MetaArea.Size);
		}
	}
}