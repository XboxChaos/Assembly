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
using Blamite.Blam.Resources;
using Blamite.Blam.Scripting;
using Blamite.Blam.ThirdGen.Resources;
using Blamite.Blam.ThirdGen.Structures;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.ThirdGen
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
        private List<ILanguage> _languages = new List<ILanguage>();
        private List<ILocaleGroup> _localeGroups = new List<ILocaleGroup>();
        private BuildInformation _buildInfo;
        private IResourceManager _resources;
        private ThirdGenResourceMetaLoader _resourceMetaLoader;

        public ThirdGenCacheFile(IReader reader, BuildInformation buildInfo, string buildString)
        {
            _buildInfo = buildInfo;
            _segmenter = new FileSegmenter(buildInfo.SegmentAlignment);
            Allocator = new MetaAllocator(this, 0x10000);
            Load(reader, buildString);
        }

        public void SaveChanges(IStream stream)
        {
            _tags.SaveChanges(stream);
            _fileNames.SaveChanges(stream);
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

        public EngineType Engine
        {
            get { return EngineType.ThirdGeneration; }
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

        public FileNameSource FileNames
        {
            get { return _fileNames; }
        }

        public StringIDSource StringIDs
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

        public IResourceManager Resources
        {
            get { return _resources; }
        }

        public TagTable Tags
        {
            get { return _tags; }
        }

        public IList<ILocaleGroup> LocaleGroups
        {
            get { return _localeGroups.AsReadOnly(); }
        }

        public FileSegmentGroup MetaArea
        {
            get { return _header.MetaArea; }
        }

        public FileSegmentGroup LocaleArea
        {
            get { return (_languageInfo != null ? _languageInfo.LocaleArea : null); }
        }

        public IResourceMetaLoader ResourceMetaLoader
        {
            get { return _resourceMetaLoader; }
        }

        public IEnumerable<FileSegment> Segments
        {
            get { return _segmenter.GetWrappers(); }
        }

        public FileSegment StringIDIndexTable
        {
            get { return _header.StringIDIndexTable; }
        }

        public FileSegment StringIDDataTable
        {
            get { return _header.StringIDData; }
        }

        public FileSegment FileNameIndexTable
        {
            get { return _header.FileNameIndexTable; }
        }

        public FileSegment FileNameDataTable
        {
            get { return _header.FileNameData; }
        }

        public MetaAllocator Allocator { get; private set; }

        public IScriptFile[] ScriptFiles { get; private set; }

        private void Load(IReader reader, string buildString)
        {
            LoadHeader(reader, buildString);
            LoadFileNames(reader);
            LoadStringIDs(reader);
            LoadTags(reader);
            LoadLanguageGlobals(reader);
            LoadScriptFiles(reader);
            LoadLocaleGroups(reader);
            LoadResourceManager(reader);
        }

        private void LoadHeader(IReader reader, string buildString)
        {
            reader.SeekTo(0);
            StructureValueCollection values = StructureReader.ReadStructure(reader, _buildInfo.GetLayout("header"));
            _header = new ThirdGenHeader(values, _buildInfo, buildString, _segmenter);
        }

        private void LoadTags(IReader reader)
        {
            if (_header.IndexHeaderLocation == null)
            {
                _tags = new ThirdGenTagTable();
                return;
            }

            _tags = new ThirdGenTagTable(reader, _header.IndexHeaderLocation, _header.MetaArea, Allocator, _buildInfo);
            _resourceMetaLoader = new ThirdGenResourceMetaLoader(_buildInfo, _header.MetaArea);
        }

        private void LoadFileNames(IReader reader)
        {
            if (_header.FileNameCount > 0)
                _fileNames = new IndexedFileNameSource(new IndexedStringTable(reader, _header.FileNameCount, _header.FileNameIndexTable, _header.FileNameData, _buildInfo.FileNameKey));
        }

        private void LoadStringIDs(IReader reader)
        {
            if (_header.StringIDCount > 0)
                _stringIds = new IndexedStringIDSource(new IndexedStringTable(reader, _header.StringIDCount, _header.StringIDIndexTable, _header.StringIDData, _buildInfo.StringIDKey), _buildInfo.StringIDResolver);
        }

        private void LoadLanguageGlobals(IReader reader)
        {
            if (_tags == null)
                return;

            // Find the language data
            ITag languageTag;
            StructureLayout tagLayout;
            if (!FindLanguageTable(out languageTag, out tagLayout))
                return;

            // Read it
            reader.SeekTo(languageTag.MetaLocation.AsOffset());
            StructureValueCollection values = StructureReader.ReadStructure(reader, tagLayout);
            _languageInfo = new ThirdGenLanguageGlobals(values, _segmenter, _header.LocalePointerConverter, _buildInfo);

            BuildLanguageList();
        }

        private bool FindLanguageTable(out ITag tag, out StructureLayout layout)
        {
            tag = null;
            layout = null;

            if (_tags == null)
                return false;

            // Check for a PATG tag, and if one isn't found, then use MATG
            if (_buildInfo.HasLayout("patg"))
            {
                tag = _tags.FindTagByClass("patg");
                layout = _buildInfo.GetLayout("patg");
            }
            if (tag == null)
            {
                tag = _tags.FindTagByClass("matg");
                layout = _buildInfo.GetLayout("matg");
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

        private void LoadLocaleGroups(IReader reader)
        {
            if (_tags == null)
                return;

            // Locale groups are stored in unic tags
            StructureLayout layout = _buildInfo.GetLayout("unic");
            foreach (ITag tag in _tags.FindTagsByClass("unic"))
            {
                if (tag.MetaLocation != null)
                {
                    reader.SeekTo(tag.MetaLocation.AsOffset());
                    StructureValueCollection values = StructureReader.ReadStructure(reader, layout);
                    _localeGroups.Add(new ThirdGenLocaleGroup(values, tag.Index));
                }
            }
        }

        private void LoadResourceManager(IReader reader)
        {
            ITag zoneTag = _tags.FindTagByClass("zone");
            ITag playTag = _tags.FindTagByClass("play");
            if (zoneTag != null && playTag != null && _buildInfo.HasLayout("resource gestalt") && _buildInfo.HasLayout("resource layout table"))
            {
                ThirdGenResourceGestalt gestalt = new ThirdGenResourceGestalt(reader, zoneTag, MetaArea, Allocator, StringIDs, _buildInfo);
                ThirdGenResourceLayoutTable layoutTable = new ThirdGenResourceLayoutTable(playTag, MetaArea, Allocator, _buildInfo);
                _resources = new ThirdGenResourceManager(gestalt, layoutTable, _tags, MetaArea, Allocator, _buildInfo);
            }
        }

        private void LoadScriptFiles(IReader reader)
        {
            // Scripts are just loaded from scnr for now...
            if (_tags != null && _buildInfo.HasLayout("scnr"))
            {
                ITag scnr = _tags.FindTagByClass("scnr");
                if (scnr != null)
                {
                    ScriptFiles = new IScriptFile[1];
                    ScriptFiles[0] = new ThirdGenScenarioScriptFile(scnr, ScenarioName, MetaArea, StringIDs, _buildInfo);
                    return;
                }
            }
            ScriptFiles = new IScriptFile[0];
        }

        private void WriteHeader(IWriter writer)
        {
            // Update tagname and stringid info (so. ugly.)
            _header.FileNameCount = _fileNames.Count;
            _header.StringIDCount = _stringIds.Count;

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
            if (!FindLanguageTable(out languageTag, out tagLayout))
                return;

            // Write it
            StructureValueCollection values = _languageInfo.Serialize();
            writer.SeekTo(languageTag.MetaLocation.AsOffset());
            StructureWriter.WriteStructure(values, tagLayout, writer);
        }
    }
}
