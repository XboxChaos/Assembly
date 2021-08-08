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
			get { return (uint)_eofSegment.Offset; }
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

		public StructureValueCollection Serialize()
		{
			var result = new StructureValueCollection();
			result.SetInteger("file size", FileSize);
			result.SetInteger("meta offset", (uint)MetaArea.Offset);
			result.SetInteger("meta size", (uint)MetaArea.Size);
			result.SetString("internal name", InternalName);
			result.SetString("build string", BuildString);
			result.SetInteger("type", (uint)Type);
			result.SetInteger("checksum", Checksum);
			return result;
		}

		private void Load(StructureValueCollection values, FileSegmenter segmenter)
		{
			_eofSegment = segmenter.WrapEOF((int)values.GetInteger("file size"));

			var metaOffset = (int)values.GetInteger("meta offset");

			int metaSize;

			// TODO (Dragon): hack for h2 alpha
			if (BuildString == "02.01.07.4998")
			{
				metaSize = (int)values.GetInteger("tag data offset") + (int)values.GetInteger("tag data size");
				// hack to rewrite the "meta size" value even though its not actually the meta size
				//_saved_meta_size_hack = (uint)values.GetInteger("meta size");
			}
			else
			{
				metaSize = (int)values.GetInteger("meta size");
			}
			
			var metaSegment = new FileSegment(
				segmenter.DefineSegment(metaOffset, metaSize, 0x4, SegmentResizeOrigin.Beginning), segmenter);

			// we hacked in a meta header size into the values earlier in the cache load
			uint metaOffsetMask = (uint)(values.GetInteger("tag table offset") - values.GetInteger("meta header size"));
			MetaArea = new FileSegmentGroup(new MetaOffsetConverter(metaSegment, metaOffsetMask));

			IndexHeaderLocation = MetaArea.AddSegment(metaSegment);

			Type = (CacheFileType)values.GetInteger("type");
			var headerGroup = new FileSegmentGroup();
			headerGroup.AddSegment(segmenter.WrapSegment(0, HeaderSize, 1, SegmentResizeOrigin.None));

			InternalName = values.GetString("internal name");

			Checksum = (uint)values.GetInteger("checksum");

			// dummy partition
			Partitions = new Partition[1];
			Partitions[0] = new Partition(SegmentPointer.FromOffset(MetaArea.Offset, MetaArea), (uint)MetaArea.Size);

			// dummy stringids
			StringIDCount = 0;
			StringIDData = _eofSegment;
			StringIDIndexTable = _eofSegment;
		}
	}
}
