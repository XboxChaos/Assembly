using System.Collections.Generic;
using Blamite.Blam.Localization;
using Blamite.Blam.Resources;
using Blamite.Blam.Resources.Sounds;
using Blamite.Blam.Scripting;
using Blamite.Blam.Shaders;
using Blamite.Blam.FourthGen.Localization;
using Blamite.Blam.FourthGen.Resources;
using Blamite.Blam.FourthGen.Resources.Sounds;
using Blamite.Blam.FourthGen.Shaders;
using Blamite.Blam.FourthGen.Structures;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.FourthGen
{
	/// <summary>
	///     A third-generation Blam (map) cache file.
	/// </summary>
	public class FourthGenCacheFile : ICacheFile
	{
		private readonly EngineDescription _buildInfo;
		private readonly FileSegmenter _segmenter;
		private IndexedFileNameSource _fileNames;
		private FourthGenHeader _header;
		private FourthGenLanguageGlobals _languageInfo;
		private FourthGenLanguagePackLoader _languageLoader;
		private FourthGenResourceMetaLoader _resourceMetaLoader;
		private IResourceManager _resources;
        private StringIDSource _stringIds;
		private FourthGenTagTable _tags;
		private FourthGenSimulationDefinitionTable _simulationDefinitions;

        public FourthGenCacheFile(IReader map_reader, IReader tag_reader, IReader string_reader, EngineDescription buildInfo, string buildString)
		{
			_buildInfo = buildInfo;
			_segmenter = new FileSegmenter(buildInfo.SegmentAlignment);
			Allocator = new MetaAllocator(this, 0x10000);
            Load(map_reader, tag_reader, string_reader, buildString);
		}

		public FourthGenHeader FullHeader
		{
			get { return _header; }
		}

		public void SaveChanges(IStream stream)
		{
			_tags.SaveChanges(stream);
			//_fileNames.SaveChanges(stream);
			//_stringIds.SaveChanges(stream);
			//if (_simulationDefinitions != null)
			//	_simulationDefinitions.SaveChanges(stream);
			//WriteHeader(stream);
			//WriteLanguageInfo(stream);
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
			get { return EngineType.FourthGeneration; }
		}

		public string InternalName
		{
			get { return _header.InternalName; }
		}

		public string ScenarioName
		{
			get { return _header.ScenarioPath; }
		}

		public string BuildString
		{
			get { return _header.BuildString; }
		}
        
		public int XDKVersion
		{
            get { return 0; }
			set { }
		}

		public SegmentPointer IndexHeaderLocation
		{
			get { return _header.IndexHeaderLocation; }
			set { _header.IndexHeaderLocation = value; }
            //get { return null; }
			//set {  }
		}

		public Partition[] Partitions
		{
			get { return null; }
		}

		public FileSegment RawTable
		{
			get { return null; }
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
			get { return _stringIds; }
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

		public FileSegmentGroup MetaArea
		{
            get { return _header.MetaArea; }
            //get { return null; }
		}

		public FileSegmentGroup LocaleArea
		{
			get { return (_languageInfo != null ? _languageInfo.LocaleArea : null); }
		}

		public ILanguagePackLoader Languages
		{
			get { return _languageLoader; }
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
			get { return null; }
		}

		public FileSegment FileNameDataTable
		{
			get { return null; }
		}

		public MetaAllocator Allocator { get; private set; }

		public IScriptFile[] ScriptFiles { get; private set; }

		public IShaderStreamer ShaderStreamer { get; private set; }

		public ISimulationDefinitionTable SimulationDefinitions
		{
			get { return _simulationDefinitions; }
		}

        private void Load(IReader map_reader, IReader tag_reader, IReader string_reader, string buildString)
		{
            LoadHeader(map_reader, tag_reader, string_reader, buildString);
			//LoadFileNames(reader);
            LoadStringIDs(string_reader);
            LoadTags(tag_reader);
            //LoadLanguageGlobals(tag_reader);
			//LoadScriptFiles(reader);
            LoadResourceManager(tag_reader);
			//LoadSimulationDefinitions(reader);
			//ShaderStreamer = new FourthGenShaderStreamer(this, _buildInfo);
		}

        private void LoadHeader(IReader map_reader, IReader tag_reader, IReader string_reader, string buildString)
		{
            // Ensure Readers are at start
            map_reader.SeekTo(0);
            tag_reader.SeekTo(0);
            string_reader.SeekTo(0);

            // Parse XML Layouts and retrieve values
            StructureValueCollection map_values = StructureReader.ReadStructure(map_reader, _buildInfo.Layouts.GetLayout("map_header"));
            StructureValueCollection tag_values = StructureReader.ReadStructure(tag_reader, _buildInfo.Layouts.GetLayout("tags_header"));
            StructureValueCollection string_values = StructureReader.ReadStructure(string_reader, _buildInfo.Layouts.GetLayout("strings_header"));

            // Create the header
            _header = new FourthGenHeader(map_values, tag_values, string_values, _buildInfo, buildString, _segmenter);
		}

		private void LoadTags(IReader reader)
		{
            /*
			if (_header.IndexHeaderLocation == null)
			{
				_tags = new FourthGenTagTable();
				return;
			}
            */

            _tags = new FourthGenTagTable(reader, Allocator, _buildInfo);
            //_tags = new FourthGenTagTable(reader, Allocator, _buildInfo);
			//_resourceMetaLoader = new FourthGenResourceMetaLoader(_buildInfo, _header.MetaArea);
            _resourceMetaLoader = new FourthGenResourceMetaLoader(_buildInfo, null);
		}

        /*
        private void LoadFileNames(IReader reader)
        {
            if (_header.FileNameCount > 0)
            {
                var stringTable = new IndexedStringTable(reader, _header.FileNameCount, _header.FileNameIndexTable,
                    _header.FileNameData, _buildInfo.TagNameKey);
                _fileNames = new IndexedFileNameSource(stringTable);
            }
        }
        */

        private void LoadStringIDs(IReader reader)
		{
            var stringTable = new FourthGenIndexedStringTable(reader);
            _stringIds = new FourthGenIndexedStringIDSource(stringTable, _buildInfo.StringIDs);
		}

		private void LoadLanguageGlobals(IReader reader)
		{
			// Find the language data
			ITag tempTag;
			StructureLayout tagLayout;
            if (!FindLanguageTable(out tempTag, out tagLayout))
			{
				// No language data
				_languageLoader = new FourthGenLanguagePackLoader();
				return;
			}
            FourthGenTag languageTag = (FourthGenTag)tempTag;

			// Read it
			reader.SeekTo(languageTag.MetaLocation.AsOffset());
			StructureValueCollection values = StructureReader.ReadStructure(reader, tagLayout);
			_languageInfo = new FourthGenLanguageGlobals(values, _segmenter, _header.LocalePointerConverter, _buildInfo);
			_languageLoader = new FourthGenLanguagePackLoader(this, _languageInfo, _buildInfo, reader);
		}

		private bool FindLanguageTable(out ITag tag, out StructureLayout layout)
		{
			tag = null;
			layout = null;

			if (_tags == null)
				return false;

			// Check for a PATG tag, and if one isn't found, then use MATG
			if (_buildInfo.Layouts.HasLayout("patg"))
			{
				tag = _tags.FindTagByClass("patg");
				layout = _buildInfo.Layouts.GetLayout("patg");
			}
			if (tag == null)
			{
				tag = _tags.FindTagByClass("matg");
				layout = _buildInfo.Layouts.GetLayout("matg");
			}
			return (tag != null && layout != null);
		}

		private void LoadResourceManager(IReader reader)
		{
			ITag zoneTag = _tags.FindTagByClass("zone");
			ITag playTag = _tags.FindTagByClass("play");
			bool haveZoneLayout = _buildInfo.Layouts.HasLayout("resource gestalt");
			bool havePlayLayout = _buildInfo.Layouts.HasLayout("resource layout table");
			bool canLoadZone = (zoneTag != null && haveZoneLayout);
			bool canLoadPlay = (playTag != null && havePlayLayout);
			if (canLoadZone || canLoadPlay)
			{
				FourthGenResourceGestalt gestalt = null;
				FourthGenResourceLayoutTable layoutTable = null;
				if (canLoadZone)
					gestalt = new FourthGenResourceGestalt(reader, zoneTag, MetaArea, Allocator, StringIDs, _buildInfo);
				if (canLoadPlay)
					layoutTable = new FourthGenResourceLayoutTable(playTag, MetaArea, Allocator, _buildInfo);

				_resources = new FourthGenResourceManager(gestalt, layoutTable, _tags, MetaArea, Allocator, _buildInfo);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public ISoundResourceGestalt LoadSoundResourceGestaltData(IReader reader)
		{
			if (_tags == null || !_buildInfo.Layouts.HasLayout("sound resource gestalt"))
				return null;

			var layout = _buildInfo.Layouts.GetLayout("sound resource gestalt");

			var ugh = _tags.FindTagByClass("ugh!");
			if (ugh == null)
				return null;

			reader.SeekTo(ugh.MetaLocation.AsOffset());
			var values = StructureReader.ReadStructure(reader, layout);
			return new FourthGenSoundResourceGestalt(values, reader, MetaArea, _buildInfo);
		}

		private void LoadScriptFiles(IReader reader)
		{
			// Scripts are just loaded from scnr for now...
			if (_tags != null && _buildInfo.Layouts.HasLayout("scnr"))
			{
				ITag scnr = _tags.FindTagByClass("scnr");
				if (scnr != null)
				{
					ScriptFiles = new IScriptFile[1];
					ScriptFiles[0] = new FourthGenScenarioScriptFile(scnr, ScenarioName, MetaArea, StringIDs, _buildInfo);
					return;
				}
			}
			ScriptFiles = new IScriptFile[0];
		}

		private void LoadSimulationDefinitions(IReader reader)
		{
			if (_tags != null && _buildInfo.Layouts.HasLayout("scnr") && _buildInfo.Layouts.HasLayout("simulation definition table entry"))
			{
				ITag scnr = _tags.FindTagByClass("scnr");
				if (scnr != null)
					_simulationDefinitions = new FourthGenSimulationDefinitionTable(scnr, _tags, reader, MetaArea, Allocator, _buildInfo);
			}
		}

		private void WriteHeader(IWriter writer)
		{
			// Update tagname and stringid info (so. ugly.)
			//_header.FileNameCount = _fileNames.Count;
			_header.StringIDCount = _stringIds.Count;

			// Serialize and write the header            
			StructureValueCollection values = _header.Serialize(_languageInfo.LocaleArea);
			writer.SeekTo(0);
			StructureWriter.WriteStructure(values, _buildInfo.Layouts.GetLayout("header"), writer);
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