using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.SecondGen.Structures;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam.SecondGen
{
    public class SecondGenCacheFile : ICacheFile
    {
        private FileSegmenter _segmenter;
        private List<FileSegment> _segments = new List<FileSegment>();
        private SecondGenHeader _header;
        private BuildInformation _buildInfo;
        private SecondGenTagTable _tags;
        private IndexedFileNameSource _fileNames;
        private IndexedStringIDSource _stringIDs;

        public SecondGenCacheFile(IReader reader, BuildInformation buildInfo, string buildString)
        {
            _buildInfo = buildInfo;
            _segmenter = new FileSegmenter(buildInfo.SegmentAlignment);
            Load(reader, buildInfo, buildString);
        }

        public void SaveChanges(IStream stream)
        {
            CalculateChecksum(stream);
            WriteHeader(stream);
            // TODO: Write the tag table
        }

        public int HeaderSize
        {
            get { return _header.HeaderSize; }
        }

        public uint FileSize
        {
            get { return _header.FileSize; }
        }

        public CacheFileType Type
        {
            get { return _header.Type; }
        }

        public string InternalName
        {
            get { return _header.InternalName; }
        }

        public string ScenarioName
        {
            get { return _header.ScenarioName; }
        }

        public string BuildString
        {
            get { return _header.BuildString; }
        }

        public int XDKVersion
        {
            get { return _header.XDKVersion; }
            set { _header.XDKVersion = value; }
        }

        public SegmentPointer IndexHeaderLocation
        {
            get { return _header.IndexHeaderLocation; }
            set { _header.IndexHeaderLocation = value; }
        }

        public Partition[] Partitions
        {
            get { return _header.Partitions; }
        }

        public FileSegment RawTable
        {
            get { return _header.RawTable; }
        }

        public FileSegmentGroup StringArea
        {
            get { return _header.StringArea; }
        }

        public IFileNameSource FileNames
        {
            get { return _fileNames; }
        }

        public IStringIDSource StringIDs
        {
            get { return _stringIDs; }
        }

        public IList<ILanguage> Languages
        {
            get { return new List<ILanguage>(); }
        }

        public IList<ILocaleGroup> LocaleGroups
        {
            get { return new List<ILocaleGroup>(); }
        }

        public IList<ITagClass> TagClasses
        {
            get { return _tags.Classes; }
        }

        public IList<ITag> Tags
        {
            get { return _tags.Tags; }
        }

        public IScenario Scenario
        {
            get { return null; }
        }

        public IList<FileSegment> Segments
        {
            get { return _segments; }
        }

        public FileSegmentGroup MetaArea
        {
            get { return _header.MetaArea; }
        }

        public FileSegmentGroup LocaleArea
        {
            get { return _header.LocaleArea; }
        }

        private void Load(IReader reader, BuildInformation buildInfo, string buildString)
        {
            _header = LoadHeader(reader, buildInfo, buildString);
            _tags = LoadTagTable(reader, buildInfo);
            _fileNames = LoadFileNames(reader, buildInfo);
            _stringIDs = LoadStringIDs(reader, buildInfo);
        }

        private SecondGenHeader LoadHeader(IReader reader, BuildInformation buildInfo, string buildString)
        {
            reader.SeekTo(0);
            StructureValueCollection values = StructureReader.ReadStructure(reader, buildInfo.GetLayout("header"));
            return new SecondGenHeader(values, buildInfo, buildString, _segmenter);
        }

        private SecondGenTagTable LoadTagTable(IReader reader, BuildInformation buildInfo)
        {
            reader.SeekTo(MetaArea.Offset);
            StructureValueCollection values = StructureReader.ReadStructure(reader, buildInfo.GetLayout("meta header"));
            return new SecondGenTagTable(reader, values, MetaArea, buildInfo);
        }

        private IndexedFileNameSource LoadFileNames(IReader reader, BuildInformation buildInfo)
        {
            IndexedStringTable strings = new IndexedStringTable(reader, _header.FileNameCount, _header.FileNameIndexTable, _header.FileNameData, buildInfo.FileNameKey);
            return new IndexedFileNameSource(strings);
        }

        private IndexedStringIDSource LoadStringIDs(IReader reader, BuildInformation buildInfo)
        {
            IndexedStringTable strings = new IndexedStringTable(reader, _header.StringIDCount, _header.StringIDIndexTable, _header.StringIDData, buildInfo.StringIDKey);
            return new IndexedStringIDSource(strings, new LengthBasedStringIDResolver(strings));
        }

        private void CalculateChecksum(IReader reader)
        {
            // XOR all of the uint32s in the file after the header
            uint checksum = 0;
            reader.SeekTo(_header.HeaderSize);
            for (var offset = _header.HeaderSize; offset < _header.FileSize; offset += 4)
                checksum ^= reader.ReadUInt32();

            _header.Checksum = checksum;
        }

        private void WriteHeader(IWriter writer)
        {
            writer.SeekTo(0);
            StructureWriter.WriteStructure(_header.Serialize(), _buildInfo.GetLayout("header"), writer);
        }
    }
}
