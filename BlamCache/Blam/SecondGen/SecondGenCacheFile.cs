using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.SecondGen.Structures;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam.SecondGen
{
    public class SecondGenCacheFile : ICacheFile
    {
        private SecondGenHeader _header;
        private MetaOffsetConverter _offsetConverter;
        private SecondGenTagTable _tags;
        private IndexedFileNameSource _fileNames;
        private IndexedStringIDSource _stringIDs;

        public SecondGenCacheFile(IReader reader, BuildInformation buildInfo, string buildString)
        {
            Load(reader, buildInfo, buildString);
        }

        public void SaveChanges(IWriter writer)
        {
            // TODO: Implement this
        }

        public ICacheFileInfo Info
        {
            get { return _header; }
        }

        public PointerConverter MetaPointerConverter
        {
            get { return _offsetConverter; }
        }

        public PointerConverter LocalePointerConverter
        {
            get { throw new NotImplementedException(); }
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

        private void Load(IReader reader, BuildInformation buildInfo, string buildString)
        {
            _header = LoadHeader(reader, buildInfo, buildString);
            _offsetConverter = new MetaOffsetConverter(_header);
            _tags = LoadTagTable(reader, buildInfo);
            _fileNames = LoadFileNames(reader, buildInfo);
            _stringIDs = LoadStringIDs(reader, buildInfo);
        }

        private SecondGenHeader LoadHeader(IReader reader, BuildInformation buildInfo, string buildString)
        {
            reader.SeekTo(0);
            StructureValueCollection values = StructureReader.ReadStructure(reader, buildInfo.GetLayout("header"));
            return new SecondGenHeader(values, buildInfo, buildString);
        }

        private SecondGenTagTable LoadTagTable(IReader reader, BuildInformation buildInfo)
        {
            reader.SeekTo(_header.MetaOffset);
            StructureValueCollection values = StructureReader.ReadStructure(reader, buildInfo.GetLayout("meta header"));
            return new SecondGenTagTable(reader, _header.MetaOffset, values, _offsetConverter, buildInfo);
        }

        private IndexedFileNameSource LoadFileNames(IReader reader, BuildInformation buildInfo)
        {
            IndexedStringTable strings = new IndexedStringTable(reader, _header.FileNameCount, _header.FileNameTableSize, _header.FileNameIndexTableLocation, _header.FileNameDataLocation, buildInfo.FileNameKey);
            return new IndexedFileNameSource(strings);
        }

        private IndexedStringIDSource LoadStringIDs(IReader reader, BuildInformation buildInfo)
        {
            IndexedStringTable strings = new IndexedStringTable(reader, _header.StringIDCount, _header.StringIDTableSize, _header.StringIDIndexTableLocation, _header.StringIDDataLocation, buildInfo.StringIDKey);
            return new IndexedStringIDSource(strings, buildInfo.StringIDResolver);
        }
    }
}
