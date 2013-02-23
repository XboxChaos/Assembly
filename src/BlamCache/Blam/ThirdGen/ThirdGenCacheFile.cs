/* Copyright 2012 Aaron Dierking, TJ Tunnell, Jordan Mueller, Alex Reed
 * 
 * This file is part of ExtryzeDLL.
 * 
 * Extryze is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Extryze is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ExtryzeDLL.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExtryzeDLL.Blam.Resources;
using ExtryzeDLL.Blam.ThirdGen.Resources;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;
using ExtryzeDLL.Util;

namespace ExtryzeDLL.Blam.ThirdGen
{
    /// <summary>
    /// A third-generation Blam (map) cache file.
    /// </summary>
    public class ThirdGenCacheFile : ICacheFile
    {
        private FileSegmenter _segmenter;
        private ThirdGenHeader _header;
        private ThirdGenTagTable _tags;
        private IndexedStringIDSource _stringIds;
        private IndexedFileNameSource _fileNames;
        private ThirdGenLanguageGlobals _languageInfo;
        private ThirdGenScenarioMeta _scenario;
        private List<ILanguage> _languages = new List<ILanguage>();
        private List<ILocaleGroup> _localeGroups = new List<ILocaleGroup>();
        private List<FileSegment> _segments = new List<FileSegment>();
        private BuildInformation _buildInfo;
        private ThirdGenResourceLayoutTable _resourceLayout;
        private ThirdGenResourceGestalt _resources;
        private ThirdGenResourceMetaLoader _resourceMetaLoader;

        public ThirdGenCacheFile(IReader reader, BuildInformation buildInfo, string buildString)
        {
            _buildInfo = buildInfo;
            _segmenter = new FileSegmenter(buildInfo.SegmentAlignment);
            Load(reader, buildInfo, buildString);
        }

        public void SaveChanges(IStream stream)
        {
            WriteHeader(stream);
            WriteLanguageInfo(stream);
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

        public ThirdGenHeader FullHeader
        {
            get { return _header; }
        }

        public IFileNameSource FileNames
        {
            get { return _fileNames; }
        }

        public IStringIDSource StringIDs
        {
            get { return _stringIds; }
        }

        public IList<ILanguage> Languages
        {
            get { return _languages.AsReadOnly(); }
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
            get { return _scenario; }
        }

        public IList<ILocaleGroup> LocaleGroups
        {
            get { return _localeGroups.AsReadOnly(); }
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
            get { return (_languageInfo != null ? _languageInfo.LocaleArea : null); }
        }

        public IResourceTable Resources
        {
            get { return _resources; }
        }

        public IResourceMetaLoader ResourceMetaLoader
        {
            get { return _resourceMetaLoader; }
        }

        private void Load(IReader reader, BuildInformation buildInfo, string buildString)
        {
            LoadHeader(reader, buildInfo, buildString);
            LoadFileNames(reader, buildInfo);
            LoadStringIDs(reader, buildInfo);
            LoadTags(reader, buildInfo);
            LoadLanguageGlobals(reader, buildInfo);
            LoadScenario(reader, buildInfo);
            LoadLocaleGroups(reader, buildInfo);
            LoadResourceLayoutTable(reader, buildInfo);
            LoadResourceGestalt(reader, buildInfo);

            BuildLanguageList();
            BuildSegmentList();
        }

        private void LoadHeader(IReader reader, BuildInformation buildInfo, string buildString)
        {
            reader.SeekTo(0);
            StructureValueCollection values = StructureReader.ReadStructure(reader, buildInfo.GetLayout("header"));
            _header = new ThirdGenHeader(values, buildInfo, buildString, _segmenter);
        }

        private void LoadTags(IReader reader, BuildInformation buildInfo)
        {
            if (_header.IndexHeaderLocation == null)
            {
                _tags = new ThirdGenTagTable();
                return;
            }

            reader.SeekTo(_header.IndexHeaderLocation.AsOffset());
            StructureValueCollection values = StructureReader.ReadStructure(reader, buildInfo.GetLayout("index header"));
            _tags = new ThirdGenTagTable(reader, values, _header.MetaArea, buildInfo);

            _resourceMetaLoader = new ThirdGenResourceMetaLoader(buildInfo, _header.MetaArea);
        }

        private void LoadFileNames(IReader reader, BuildInformation buildInfo)
        {
            if (_header.FileNameCount > 0)
                _fileNames = new IndexedFileNameSource(new IndexedStringTable(reader, _header.FileNameCount, _header.FileNameIndexTable, _header.FileNameData, buildInfo.FileNameKey));
        }

        private void LoadStringIDs(IReader reader, BuildInformation buildInfo)
        {
            if (_header.StringIDCount > 0)
                _stringIds = new IndexedStringIDSource(new IndexedStringTable(reader, _header.StringIDCount, _header.StringIDIndexTable, _header.StringIDData, buildInfo.StringIDKey), buildInfo.StringIDResolver);
        }

        private void LoadLanguageGlobals(IReader reader, BuildInformation buildInfo)
        {
            // Find the language data
            ITag languageTag;
            StructureLayout tagLayout;
            if (!FindLanguageTable(buildInfo, out languageTag, out tagLayout))
                return;

            // Read it
            reader.SeekTo(languageTag.MetaLocation.AsOffset());
            StructureValueCollection values = StructureReader.ReadStructure(reader, tagLayout);
            _languageInfo = new ThirdGenLanguageGlobals(values, _segmenter, _header.LocalePointerConverter, buildInfo);
        }

        private bool FindLanguageTable(BuildInformation buildInfo, out ITag tag, out StructureLayout layout)
        {
            // Check for a PATG tag, and if one isn't found, then use MATG
            tag = null;
            layout = null;
            if (buildInfo.HasLayout("patg"))
            {
                tag = FindTagByClass(PatgMagic);
                layout = buildInfo.GetLayout("patg");
            }
            if (tag == null)
            {
                tag = FindTagByClass(MatgMagic);
                layout = buildInfo.GetLayout("matg");
            }
            return (tag != null && layout != null);
        }

        private void BuildLanguageList()
        {
            // hax hax hax
            if (_languageInfo != null)
            {
                foreach (ThirdGenLanguage language in _languageInfo.Languages)
                    _languages.Add(language);
            }
        }

        private void LoadScenario(IReader reader, BuildInformation buildInfo)
        {
            if (!buildInfo.HasLayout("scnr"))
                return;
            ITag scnr = FindTagByClass(ScnrMagic);
            if (scnr == null)
                return;

            reader.SeekTo(scnr.MetaLocation.AsOffset());
            StructureValueCollection values = StructureReader.ReadStructure(reader, buildInfo.GetLayout("scnr"));
            _scenario = new ThirdGenScenarioMeta(values, reader, MetaArea, _stringIds, buildInfo);
        }

        private void LoadLocaleGroups(IReader reader, BuildInformation buildInfo)
        {
            if (_tags == null)
                return;

            // Locale groups are stored in unic tags
            StructureLayout layout = buildInfo.GetLayout("unic");
            foreach (ITag tag in _tags.Tags)
            {
                if (tag != null && tag.Class != null && tag.Class.Magic == UnicMagic && tag.MetaLocation != null)
                {
                    reader.SeekTo(tag.MetaLocation.AsOffset());
                    StructureValueCollection values = StructureReader.ReadStructure(reader, layout);
                    _localeGroups.Add(new ThirdGenLocaleGroup(values, tag.Index));
                }
            }
        }

        private void LoadResourceLayoutTable(IReader reader, BuildInformation buildInfo)
        {
            if (!buildInfo.HasLayout("resource layout table"))
                return;

            StructureLayout layout = buildInfo.GetLayout("resource layout table");

            ITag play = FindTagByClass(PlayMagic);
            if (play == null)
                return;

            reader.SeekTo(play.MetaLocation.AsOffset());
            StructureValueCollection values = StructureReader.ReadStructure(reader, layout);
            _resourceLayout = new ThirdGenResourceLayoutTable(values, reader, MetaArea, buildInfo);
        }

        private void LoadResourceGestalt(IReader reader, BuildInformation buildInfo)
        {
            if (_resourceLayout == null || !_buildInfo.HasLayout("resource gestalt"))
                return;

            StructureLayout layout = buildInfo.GetLayout("resource gestalt");

            ITag zone = FindTagByClass(ZoneMagic);
            if (zone == null)
                return;

            reader.SeekTo(zone.MetaLocation.AsOffset());
            StructureValueCollection values = StructureReader.ReadStructure(reader, layout);
            _resources = new ThirdGenResourceGestalt(values, reader, MetaArea, buildInfo, _tags, _resourceLayout);
        }

        private void WriteHeader(IWriter writer)
        {
            // Serialize and write the header
            StructureValueCollection values = _header.Serialize(_languageInfo.LocaleArea);
            writer.SeekTo(0);
            StructureWriter.WriteStructure(values, _buildInfo.GetLayout("header"), writer);
        }

        private void WriteLanguageInfo(IWriter writer)
        {
            // Find the language data
            ITag languageTag;
            StructureLayout tagLayout;
            if (!FindLanguageTable(_buildInfo, out languageTag, out tagLayout))
                return;

            // Write it
            StructureValueCollection values = _languageInfo.Serialize();
            writer.SeekTo(languageTag.MetaLocation.AsOffset());
            StructureWriter.WriteStructure(values, tagLayout, writer);
        }

        private ITag FindTagByClass(int classMagic)
        {
            if (_tags == null)
                return null;

            foreach (ITag tag in _tags.Tags)
            {
                if (tag != null && tag.Class != null && tag.Class.Magic == classMagic && tag.MetaLocation != null)
                    return tag;
            }
            return null;
        }

        private void BuildSegmentList()
        {
            _segments.AddRange(_header.Segments);
        }

        private static int MatgMagic = CharConstant.FromString("matg");
        private static int PatgMagic = CharConstant.FromString("patg");
        private static int ScnrMagic = CharConstant.FromString("scnr");
        private static int UnicMagic = CharConstant.FromString("unic");
        private static int PlayMagic = CharConstant.FromString("play");
        private static int ZoneMagic = CharConstant.FromString("zone");
    }
}
