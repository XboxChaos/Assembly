using Blamite.IO;
using Blamite.Serialization;

namespace Blamite.Blam.FirstGen.Structures
{
	public class FirstGenHeader
	{
		private FileSegment _eofSegment;
		public FirstGenHeader(StructureValueCollection values, EngineDescription info, FileSegmenter segmenter)
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

		// leaving set public so we can grab the scenario name
		// from the filenames on cache load
		public string ScenarioName { get; set; }

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

		private uint _bsp_size_hack = 0;

		public StructureValueCollection Serialize()
		{
			var result = new StructureValueCollection();
			result.SetInteger("file size", FileSize);
			result.SetInteger("meta offset", (uint)MetaArea.Offset);

			if (_bsp_size_hack > 0)
				result.SetInteger("meta size", MetaArea.Size + _bsp_size_hack);
			else
				result.SetInteger("meta size", MetaArea.VirtualSize);

			result.SetString("internal name", InternalName);
			result.SetString("build string", BuildString);
			result.SetInteger("type", (uint)Type);
			result.SetInteger("checksum", Checksum);
			return result;
		}

		private void Load(StructureValueCollection values, FileSegmenter segmenter)
		{
			//some opensauce maps were found to have the size set to 0.
			uint filesize = (uint)values.GetInteger("file size");
			if (filesize == 0)
				filesize = (uint)values.GetInteger("true filesize");

			_eofSegment = segmenter.WrapEOF(filesize);

			uint metaOffset = (uint)values.GetInteger("meta offset");

			uint metaSize;
			if (values.HasInteger("tag data offset"))
			{
				metaSize = (uint)values.GetInteger("tag data offset") + (uint)values.GetInteger("tag data size");
			}
			else
				metaSize = (uint)values.GetInteger("meta size");

			var metaSegment = new FileSegment(
				segmenter.DefineSegment(metaOffset, metaSize, 0x4, SegmentResizeOrigin.Beginning), segmenter);

			uint metaOffsetMask;
			if (values.HasInteger("xbox meta offset mask"))
				metaOffsetMask = (uint)values.GetInteger("xbox meta offset mask");
			else
				metaOffsetMask = (uint)(values.GetInteger("tag table offset") - values.GetInteger("meta header size"));

			MetaArea = new FileSegmentGroup(new MetaOffsetConverter(metaSegment, metaOffsetMask));

			// Until proper BSP support is merged in, we have to math the BSP size.
			if (values.HasInteger("xbox bsp mask"))
				_bsp_size_hack = (uint)MetaArea.PointerMask - (uint)values.GetInteger("xbox bsp mask");

			IndexHeaderLocation = MetaArea.AddSegment(metaSegment);

			Type = (CacheFileType)values.GetInteger("type");
			var headerGroup = new FileSegmentGroup();
			headerGroup.AddSegment(segmenter.WrapSegment(0, (uint)HeaderSize, 1, SegmentResizeOrigin.None));

			//h2 alpha forcing this to be shoved in
			if (values.HasInteger("string table count"))
			{
				StringIDCount = (int)values.GetInteger("string table count");
				var sidDataSize = (uint)values.GetInteger("string table size");
				StringIDData = segmenter.WrapSegment((uint)values.GetInteger("string table offset"), sidDataSize, 1,
					SegmentResizeOrigin.End);
				StringIDIndexTable = segmenter.WrapSegment((uint)values.GetInteger("string index table offset"), (uint)StringIDCount * 4, 4,
					SegmentResizeOrigin.End);

				StringArea = new FileSegmentGroup();
				if (values.HasInteger("string block offset"))
					StringArea.AddSegment(segmenter.WrapSegment((uint)values.GetInteger("string block offset"), (uint)StringIDCount * 0x80, 0x80,
						SegmentResizeOrigin.End));
				StringArea.AddSegment(StringIDIndexTable);
				StringArea.AddSegment(StringIDData);

				StringIDIndexTableLocation = SegmentPointer.FromOffset(StringIDIndexTable.Offset, StringArea);
				StringIDDataLocation = SegmentPointer.FromOffset(StringIDData.Offset, StringArea);
			}

			InternalName = values.GetString("internal name");

			Checksum = (uint)values.GetIntegerOrDefault("checksum", 0);

			// dummy partition
			Partitions = new Partition[1];
			Partitions[0] = new Partition(SegmentPointer.FromOffset(MetaArea.Offset, MetaArea), (uint)MetaArea.Size);

		}
	}
}
