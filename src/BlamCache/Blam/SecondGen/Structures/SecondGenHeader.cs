using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam.SecondGen.Structures
{
    public class SecondGenHeader
    {
        private FileSegment _eofSegment;

        public SecondGenHeader(StructureValueCollection values, BuildInformation info, string buildString, FileSegmenter segmenter)
        {
            BuildString = buildString;
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

        public IList<FileSegment> Segments { get; private set; }

        public uint Checksum { get; set; }

        public StructureValueCollection Serialize()
        {
            StructureValueCollection result = new StructureValueCollection();
            result.SetNumber("file size", FileSize);
            result.SetNumber("meta offset", (uint)MetaArea.Offset);
            result.SetNumber("meta size", (uint)MetaArea.Size);
            result.SetNumber("meta offset mask", MetaArea.BasePointer);
            result.SetNumber("type", (uint)Type);
            result.SetNumber("string table count", (uint)StringIDCount);
            result.SetNumber("string table size", (uint)StringIDData.Size);
            result.SetNumber("string index table offset", (uint)StringIDIndexTable.Offset);
            result.SetNumber("string table offset", (uint)StringIDData.Offset);
            result.SetString("internal name", InternalName);
            result.SetString("scenario name", ScenarioName);
            result.SetNumber("file table count", (uint)FileNameCount);
            result.SetNumber("file table offset", (uint)FileNameData.Offset);
            result.SetNumber("file table size", (uint)FileNameData.Size);
            result.SetNumber("file index table offset", (uint)FileNameIndexTable.Offset);
            result.SetNumber("raw table offset", (uint)RawTable.Offset);
            result.SetNumber("raw table size", (uint)RawTable.Size);
            result.SetNumber("checksum", Checksum);
            return result;
        }

        private void Load(StructureValueCollection values, FileSegmenter segmenter)
        {
            _eofSegment = segmenter.WrapEOF((int)values.GetNumber("file size"));

            int metaOffset = (int)values.GetNumber("meta offset");
            int metaSize = (int)values.GetNumber("meta size");
            uint metaOffsetMask = values.GetNumber("meta offset mask");

            FileSegment metaSegment = new FileSegment(segmenter.DefineSegment(metaOffset, metaSize, 0x200, SegmentResizeOrigin.Beginning), segmenter);
            MetaArea = new FileSegmentGroup(new MetaOffsetConverter(metaSegment, metaOffsetMask));
            IndexHeaderLocation = MetaArea.AddSegment(metaSegment);

            Type = (CacheFileType)values.GetNumber("type");

            FileSegmentGroup headerGroup = new FileSegmentGroup();
            headerGroup.AddSegment(segmenter.WrapSegment(0, HeaderSize, 1, SegmentResizeOrigin.None));

            StringIDCount = (int)values.GetNumber("string table count");
            int sidDataSize = (int)values.GetNumber("string table size");
            StringIDData = segmenter.WrapSegment((int)values.GetNumber("string table offset"), sidDataSize, 1, SegmentResizeOrigin.End);
            StringIDIndexTable = segmenter.WrapSegment((int)values.GetNumber("string index table offset"), StringIDCount * 4, 4, SegmentResizeOrigin.End);

            FileNameCount = (int)values.GetNumber("file table count");
            int fileDataSize = (int)values.GetNumber("file table size");
            FileNameData = segmenter.WrapSegment((int)values.GetNumber("file table offset"), fileDataSize, 1, SegmentResizeOrigin.End);
            FileNameIndexTable = segmenter.WrapSegment((int)values.GetNumber("file index table offset"), FileNameCount * 4, 4, SegmentResizeOrigin.End);

            InternalName = values.GetString("internal name");
            ScenarioName = values.GetString("scenario name");

            StringArea = new FileSegmentGroup();
            StringArea.AddSegment(segmenter.WrapSegment((int)values.GetNumber("string block offset"), StringIDCount * 0x80, 0x80, SegmentResizeOrigin.End));
            StringArea.AddSegment(StringIDIndexTable);
            StringArea.AddSegment(StringIDData);
            StringArea.AddSegment(FileNameIndexTable);
            StringArea.AddSegment(FileNameData);

            StringIDIndexTableLocation = SegmentPointer.FromOffset(StringIDIndexTable.Offset, StringArea);
            StringIDDataLocation = SegmentPointer.FromOffset(StringIDData.Offset, StringArea);
            FileNameIndexTableLocation = SegmentPointer.FromOffset(FileNameIndexTable.Offset, StringArea);
            FileNameDataLocation = SegmentPointer.FromOffset(FileNameData.Offset, StringArea);

            LocaleArea = new FileSegmentGroup();

            int rawTableOffset = (int)values.GetNumber("raw table offset");
            int rawTableSize = (int)values.GetNumber("raw table size");
            RawTable = segmenter.WrapSegment(rawTableOffset, rawTableSize, 1, SegmentResizeOrigin.End);

            Checksum = values.GetNumber("checksum");

            // Set up a bogus partition table
            Partitions = new Partition[1];
            Partitions[0] = new Partition(SegmentPointer.FromOffset(MetaArea.Offset, MetaArea), (uint)MetaArea.Size);
        }
    }
}
