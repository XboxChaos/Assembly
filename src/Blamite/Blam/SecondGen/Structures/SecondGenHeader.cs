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
			get { return _eofSegment.Offset; }
		}

		public CacheFileType Type { get; private set; }
		public string InternalName { get; private set; }
		public string ScenarioName { get; set; }
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

		private uint _bsp_size_hack = 0;

		public StructureValueCollection Serialize()
		{
			var result = new StructureValueCollection();
			result.SetInteger("file size", FileSize);
			result.SetInteger("meta offset", (uint) MetaArea.Offset);
			result.SetInteger("tag data offset", (uint)MetaArea.Segments[0].Size);
			result.SetInteger("tag data size", (uint)MetaArea.Segments[1].Size);

			//handle xbox silliness
			if (_bsp_size_hack > 0)
				result.SetInteger("meta size", MetaArea.Size + _bsp_size_hack);
			else
			{
				result.SetInteger("meta size", (uint)MetaArea.Size);
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

			//beta didnt have names in the header yet
			if (FileNameData != null)
			{
				result.SetInteger("file table count", (uint)FileNameCount);
				result.SetInteger("file table offset", (uint)FileNameData.Offset);
				result.SetInteger("file table size", (uint)FileNameData.Size);
				result.SetInteger("file index table offset", (uint)FileNameIndexTable.Offset);
			}
			
			result.SetInteger("raw table offset", RawTable != null ? (uint)RawTable.Offset : 0xFFFFFFFF);
			result.SetInteger("raw table size", RawTable != null ? (uint)RawTable.Size : 0);
			result.SetInteger("checksum", Checksum);
			return result;
		}

		
		private void Load(StructureValueCollection values, FileSegmenter segmenter)
		{
			uint filesize = (uint)values.GetInteger("file size");

			uint metaOffset = (uint)values.GetInteger("meta offset");

			uint tagTableSize = (uint)values.GetInteger("tag data offset");
			uint tagDataSize = (uint)values.GetInteger("tag data size");

			var headSegment = new FileSegment(
				segmenter.DefineSegment(metaOffset, tagTableSize, 0x1000, SegmentResizeOrigin.Beginning), segmenter);

			//xbox haxx, we can assume thru the existance of the code-set xbox mask
			uint metaOffsetMask;
			FileSegment metaSegment = null;
			if (values.HasInteger("xbox meta offset mask"))
			{
				metaOffsetMask = (uint)values.GetInteger("xbox meta offset mask");

				// old modded h2 maps have unexpected size values, if this is the case, fix it. This will get applied to the file when saving.
				uint segmentsize = metaOffset + tagTableSize + tagDataSize;
				if (segmentsize > filesize)
					filesize = segmentsize;

				metaSegment = new FileSegment(
					segmenter.DefineSegment(metaOffset + (uint)tagTableSize, tagDataSize, 0x4, SegmentResizeOrigin.End), segmenter);
			}
			else
			{
				metaOffsetMask = (uint)values.GetInteger("meta offset mask");

				metaSegment = new FileSegment(
					segmenter.DefineSegment(metaOffset + (uint)tagTableSize, tagDataSize, 0x1000, SegmentResizeOrigin.End), segmenter);
			}

			_eofSegment = segmenter.WrapEOF(filesize);

			MetaArea = new FileSegmentGroup(new MetaOffsetConverter(headSegment, metaOffsetMask));

			// Until proper BSP support is merged in, we have to math the BSP size.
			if (values.HasInteger("xbox bsp mask"))
				_bsp_size_hack = (uint)MetaArea.PointerMask - (uint)values.GetInteger("xbox bsp mask");

			IndexHeaderLocation = MetaArea.AddSegment(headSegment);
			MetaArea.AddSegment(metaSegment);

			Type = (CacheFileType) values.GetInteger("type");

			var headerGroup = new FileSegmentGroup();
			headerGroup.AddSegment(segmenter.WrapSegment(0, (uint)HeaderSize, 1, SegmentResizeOrigin.None));

			StringIDCount = (int) values.GetInteger("string table count");
			var sidDataSize = (uint) values.GetInteger("string table size");
			StringIDData = segmenter.WrapSegment((uint) values.GetInteger("string table offset"), sidDataSize, 1,
				SegmentResizeOrigin.End);
			StringIDIndexTable = segmenter.WrapSegment((uint) values.GetInteger("string index table offset"), (uint)StringIDCount*4, 4,
				SegmentResizeOrigin.End);

			if (values.HasInteger("file table count"))
			{
				FileNameCount = (int)values.GetInteger("file table count");
				if (FileNameCount > 0)//obfuscate me cap'n!
				{
					var fileDataSize = (uint)values.GetInteger("file table size");
					FileNameData = segmenter.WrapSegment((uint)values.GetInteger("file table offset"), fileDataSize, 1,
						SegmentResizeOrigin.End);
					FileNameIndexTable = segmenter.WrapSegment((uint)values.GetInteger("file index table offset"), (uint)FileNameCount * 4, 4,
						SegmentResizeOrigin.End);
				}
				
			}

			InternalName = values.GetString("internal name");
			ScenarioName = values.GetString("scenario name");

			StringArea = new FileSegmentGroup();
			if (values.HasInteger("string block offset"))
				StringArea.AddSegment(segmenter.WrapSegment((uint) values.GetInteger("string block offset"), (uint)StringIDCount*0x80, 0x80,
					SegmentResizeOrigin.End));
			StringArea.AddSegment(StringIDIndexTable);
			StringArea.AddSegment(StringIDData);

			StringIDIndexTableLocation = SegmentPointer.FromOffset(StringIDIndexTable.Offset, StringArea);
			StringIDDataLocation = SegmentPointer.FromOffset(StringIDData.Offset, StringArea);

			if (FileNameIndexTable != null)
			{
				StringArea.AddSegment(FileNameIndexTable);
				StringArea.AddSegment(FileNameData);

				FileNameIndexTableLocation = SegmentPointer.FromOffset(FileNameIndexTable.Offset, StringArea);
				FileNameDataLocation = SegmentPointer.FromOffset(FileNameData.Offset, StringArea);
			}

			uint rawTableOffset;
			uint rawTableSize;
			if (values.HasInteger("raw table offset"))
			{
				rawTableOffset = (uint)values.GetInteger("raw table offset");
				rawTableSize = (uint)values.GetInteger("raw table size");
				// It is apparently possible to create a cache without a raw table, but -1 gets written as the offset
				if (rawTableOffset != 0xFFFFFFFF)
					RawTable = segmenter.WrapSegment(rawTableOffset, rawTableSize, 0x80, SegmentResizeOrigin.End);
			}

			Checksum = (uint)values.GetIntegerOrDefault("checksum", 0);

			// Set up a bogus partition table
			Partitions = new Partition[1];
			Partitions[0] = new Partition(SegmentPointer.FromOffset(MetaArea.Offset, MetaArea), (uint) MetaArea.Size);
		}
	}
}