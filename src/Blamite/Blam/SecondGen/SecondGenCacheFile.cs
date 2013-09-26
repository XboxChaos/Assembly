using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Resources;
using Blamite.Blam.Scripting;
using Blamite.Blam.SecondGen.Structures;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.SecondGen
{
    public class SecondGenCacheFile : ICacheFile
    {
        private FileSegmenter _segmenter;
        private SecondGenHeader _header;
        private EngineDescription _buildInfo;
        private SecondGenTagTable _tags;
        private IndexedFileNameSource _fileNames;
        private IndexedStringIDSource _stringIDs;

        public SecondGenCacheFile(IReader reader, EngineDescription buildInfo, string buildString)
        {
            _buildInfo = buildInfo;
            _segmenter = new FileSegmenter(buildInfo.SegmentAlignment);
            Allocator = new MetaAllocator(this, 0x10000);
            Load(reader, buildInfo, buildString);
        }

        public IResourceTable LoadResourceTable(IReader reader)
        {
            // Not supported yet
            return null;
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

        public EngineType Engine
        {
            get { return EngineType.SecondGeneration; }
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

        public FileNameSource FileNames
        {
            get { return _fileNames; }
        }

        public StringIDSource StringIDs
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

        public TagTable Tags
        {
            get { return _tags; }
        }

        public IEnumerable<FileSegment> Segments
        {
            get { return _segmenter.GetWrappers(); }
        }

        public FileSegmentGroup MetaArea
        {
            get { return _header.MetaArea; }
        }

        public FileSegmentGroup LocaleArea
        {
            get { return _header.LocaleArea; }
        }

        public IResourceMetaLoader ResourceMetaLoader
        {
            get { return new SecondGenResourceMetaLoader(); }
        }

        public MetaAllocator Allocator { get; private set; }

        public IScriptFile[] ScriptFiles
        {
            get { return new IScriptFile[0]; }
        }

        private void Load(IReader reader, EngineDescription buildInfo, string buildString)
        {
            _header = LoadHeader(reader, buildInfo, buildString);
            _tags = LoadTagTable(reader, buildInfo);
            _fileNames = LoadFileNames(reader, buildInfo);
            _stringIDs = LoadStringIDs(reader, buildInfo);
        }

        private SecondGenHeader LoadHeader(IReader reader, EngineDescription buildInfo, string buildString)
        {
            reader.SeekTo(0);
            StructureValueCollection values = StructureReader.ReadStructure(reader, buildInfo.Layouts.GetLayout("header"));
            return new SecondGenHeader(values, buildInfo, buildString, _segmenter);
        }

        private SecondGenTagTable LoadTagTable(IReader reader, EngineDescription buildInfo)
        {
            reader.SeekTo(MetaArea.Offset);
            StructureValueCollection values = StructureReader.ReadStructure(reader, buildInfo.Layouts.GetLayout("meta header"));
            return new SecondGenTagTable(reader, values, MetaArea, buildInfo);
        }

        private IndexedFileNameSource LoadFileNames(IReader reader, EngineDescription buildInfo)
        {
            IndexedStringTable strings = new IndexedStringTable(reader, _header.FileNameCount, _header.FileNameIndexTable, _header.FileNameData, buildInfo.TagNameKey);
            return new IndexedFileNameSource(strings);
        }

        private IndexedStringIDSource LoadStringIDs(IReader reader, EngineDescription buildInfo)
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
            StructureWriter.WriteStructure(_header.Serialize(), _buildInfo.Layouts.GetLayout("header"), writer);
        }
    }
}
