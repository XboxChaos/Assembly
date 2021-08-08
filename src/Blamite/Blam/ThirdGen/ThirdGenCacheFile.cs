using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Localization;
using Blamite.Blam.Resources;
using Blamite.Blam.Resources.Sounds;
using Blamite.Blam.Scripting;
using Blamite.Blam.Shaders;
using Blamite.Blam.ThirdGen.Localization;
using Blamite.Blam.ThirdGen.Resources;
using Blamite.Blam.ThirdGen.Shaders;
using Blamite.Blam.ThirdGen.Structures;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.ThirdGen
{
	/// <summary>
	///     A third-generation Blam (map) cache file.
	/// </summary>
	public class ThirdGenCacheFile : ICacheFile
	{
		private readonly EngineDescription _buildInfo;
		private readonly FileSegmenter _segmenter;
		private IndexedFileNameSource _fileNames;
		private ThirdGenHeader _header;
		private ThirdGenLanguageGlobals _languageInfo;
		private ThirdGenLanguagePackLoader _languageLoader;
		private ThirdGenResourceMetaLoader _resourceMetaLoader;
		private IResourceManager _resources;
		private IndexedStringIDSource _stringIds;
		private ThirdGenTagTable _tags;
		private ThirdGenSimulationDefinitionTable _simulationDefinitions;
		private ThirdGenPointerExpander _expander;
		private Endian _endianness;
		private EffectInterop _effects;
		private SoundResourceManager _soundGestalt;

		private bool _zoneOnly = false;

		public ThirdGenCacheFile(IReader reader, EngineDescription buildInfo, string filePath)
		{
			FilePath = filePath;
			_endianness = reader.Endianness;
			_buildInfo = buildInfo;
			_segmenter = new FileSegmenter(buildInfo.SegmentAlignment);
			_expander = new ThirdGenPointerExpander(buildInfo.ExpandMagic);
			Allocator = new MetaAllocator(this, 0x10000);
			Load(reader);
		}

		public ThirdGenHeader FullHeader
		{
			get { return _header; }
		}

		public void SaveChanges(IStream stream)
		{
			_tags.SaveChanges(stream);
			_fileNames.SaveChanges(stream);
			_stringIds.SaveChanges(stream);
			if (_simulationDefinitions != null)
				_simulationDefinitions.SaveChanges(stream);
			if (_effects != null)
				_effects.SaveChanges(stream);
			WriteLanguageInfo(stream);
			_header.Checksum = ICacheFileExtensions.GenerateChecksum(this, stream);
			WriteHeader(stream);
		}

		public string FilePath { get; private set; }

		public int HeaderSize
		{
			get { return _header.HeaderSize; }
		}

		public long FileSize
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

		public bool ZoneOnly
		{
			get { return _zoneOnly; }
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
			get { return _stringIds; }
		}

		public IList<ITagGroup> TagGroups
		{
			get { return _tags.Groups; }
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
			get { return _header.FileNameIndexTable; }
		}

		public FileSegment FileNameDataTable
		{
			get { return _header.FileNameData; }
		}

		public MetaAllocator Allocator { get; private set; }

		public IScriptFile[] ScriptFiles { get; private set; }

		public IShaderStreamer ShaderStreamer { get; private set; }

		public ISimulationDefinitionTable SimulationDefinitions
		{
			get { return _simulationDefinitions; }
		}

		public IList<ITagInterop> TagInteropTable
		{
			get { return _tags.Interops; }
		}

		public IPointerExpander PointerExpander
		{
			get { return _expander; }
		}

		public Endian Endianness
		{
			get { return _endianness; }
		}

		public EffectInterop EffectInterops
		{
			get { return _effects; }
		}

		public SoundResourceManager SoundGestalt
		{
			get { return _soundGestalt; }
		}

		private void Load(IReader reader)
		{
			LoadHeader(reader);
			LoadFileNames(reader);
			var resolver = LoadStringIDNamespaces(reader);
			LoadStringIDs(reader, resolver);
			LoadTags(reader);
			LoadLanguageGlobals(reader);
			LoadScriptFiles();
			LoadResourceManager(reader);
			LoadSoundResourceManager(reader);
			LoadSimulationDefinitions(reader);
			LoadEffects(reader);
			ShaderStreamer = new ThirdGenShaderStreamer(this, _buildInfo);
		}

		private void LoadHeader(IReader reader)
		{
			reader.SeekTo(0);
			StructureValueCollection values = StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("header"));
			_header = new ThirdGenHeader(values, _buildInfo, _segmenter, _expander);
		}

		private void LoadTags(IReader reader)
		{
			if (_header.IndexHeaderLocation == null)
			{
				_tags = new ThirdGenTagTable();
				return;
			}

			_tags = new ThirdGenTagTable(reader, _header.IndexHeaderLocation, _header.MetaArea, Allocator, _buildInfo, _expander);
			_resourceMetaLoader = new ThirdGenResourceMetaLoader(_buildInfo, _header.MetaArea);
		}

		private void LoadFileNames(IReader reader)
		{
			if (_header.FileNameCount > 0)
			{
				var stringTable = new IndexedStringTable(reader, _header.FileNameCount, _header.FileNameIndexTable,
					_header.FileNameData, _buildInfo.TagNameKey);
				_fileNames = new IndexedFileNameSource(stringTable);
			}
		}

		private StringIDNamespaceResolver LoadStringIDNamespaces(IReader reader)
		{
			// making some assumptions here based on all current stringid xmls
			if (_header.StringIDNamespaceCount > 1)
			{
				int[] namespaces = new int[_header.StringIDNamespaceCount];
				reader.SeekTo(_header.StringIDNamespaceTable.Offset);
				for (int i = 0; i < namespaces.Length; i++)
					namespaces[i] = reader.ReadInt32();

				// shift our way to the namespace bits, assuming index is always at least 16 bits
				int firstNamespaceBit = -1;
				for (int i = 16; i < 32; i++)
					if (namespaces[1] >> i == 1)
					{
						firstNamespaceBit = i;
						break;
					}

				if (firstNamespaceBit == -1)
					return null;

				// assuming here that the namespace is always 8 bits
				var resolver = new StringIDNamespaceResolver(new StringIDLayout(firstNamespaceBit, 8, 32 - 8 - firstNamespaceBit));
				int indexMask = (1 << firstNamespaceBit) - 1;

				// register all but the first namespace as that needs the final count
				int counter = namespaces[0] & indexMask;
				for (int i = 1; i < namespaces.Length; i++)
				{
					resolver.RegisterSet(i, 0, counter);
					counter += namespaces[i] & indexMask;
				}
				resolver.RegisterSet(0, namespaces[0] & indexMask, counter);
				return resolver;
			}
			else
				return null;
		}

		private void LoadStringIDs(IReader reader, StringIDNamespaceResolver resolver = null)
		{
			if (_header.StringIDCount > 0)
			{
				var stringTable = new IndexedStringTable(reader, _header.StringIDCount, _header.StringIDIndexTable,
					_header.StringIDData, _buildInfo.StringIDKey);
				_stringIds = new IndexedStringIDSource(stringTable, resolver != null ? resolver : _buildInfo.StringIDs);
			}
		}

		private void LoadLanguageGlobals(IReader reader)
		{
			// Find the language data
			ITag languageTag;
			StructureLayout tagLayout;
			if (!FindLanguageTable(out languageTag, out tagLayout))
			{
				// No language data
				_languageLoader = new ThirdGenLanguagePackLoader();
				return;
			}

			// Read it
			reader.SeekTo(languageTag.MetaLocation.AsOffset());
			StructureValueCollection values = StructureReader.ReadStructure(reader, tagLayout);
			_languageInfo = new ThirdGenLanguageGlobals(values, _segmenter, _header.LocalePointerConverter, _buildInfo);
			_languageLoader = new ThirdGenLanguagePackLoader(this, _languageInfo, _buildInfo, reader);
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
				tag = _tags.FindTagByGroup("patg");
				layout = _buildInfo.Layouts.GetLayout("patg");
			}
			if (tag == null)
			{
				tag = _tags.FindTagByGroup("matg");
				layout = _buildInfo.Layouts.GetLayout("matg");
			}
			return (tag != null && layout != null);
		}

		private void LoadResourceManager(IReader reader)
		{
			ITag zoneTag = _tags.FindTagByGroup("zone");
			ITag playTag = _tags.FindTagByGroup("play");
			bool haveZoneLayout = _buildInfo.Layouts.HasLayout("resource gestalt");
			bool havePlayLayout = _buildInfo.Layouts.HasLayout("resource layout table");
			bool haveAltPlayLayout = _buildInfo.Layouts.HasLayout("resource layout table alt");
			bool canLoadZone = (zoneTag != null && zoneTag.MetaLocation != null && haveZoneLayout);
			bool canLoadPlay = (playTag != null && playTag.MetaLocation != null && havePlayLayout);
			if (canLoadZone || canLoadPlay)
			{
				ThirdGenResourceGestalt gestalt = null;
				ThirdGenResourceLayoutTable layoutTable = null;
				if (canLoadZone)
					gestalt = new ThirdGenResourceGestalt(reader, zoneTag, MetaArea, Allocator, StringIDs, _buildInfo, _expander);

				if (canLoadPlay)
					layoutTable = new ThirdGenResourceLayoutTable(playTag, MetaArea, Allocator, _buildInfo, _expander);
				else if (canLoadZone && haveAltPlayLayout)
				{
					layoutTable = new ThirdGenResourceLayoutTable(zoneTag, MetaArea, Allocator, _buildInfo, _expander);
					_zoneOnly = true;
				}
					

				_resources = new ThirdGenResourceManager(gestalt, layoutTable, _tags, MetaArea, Allocator, _buildInfo, _expander);
			}
		}

		private void LoadSoundResourceManager(IReader reader)
		{
			ITag ughTag = _tags.FindTagByGroup("ugh!");
			bool haveUghLayout = _buildInfo.Layouts.HasLayout("sound resource gestalt");
			bool canLoadUgh = (ughTag != null && ughTag.MetaLocation != null && haveUghLayout);

			if (ughTag != null && ughTag.MetaLocation != null && haveUghLayout)
			{
				SoundResourceGestalt gestalt = null;
				if (canLoadUgh)
					gestalt = new SoundResourceGestalt(reader, ughTag, MetaArea, Allocator, _buildInfo, _expander);

				_soundGestalt = new SoundResourceManager(gestalt, _tags, MetaArea, Allocator, _buildInfo, _expander);
			}
		}

		private void LoadScriptFiles()
		{
			if (_tags != null)
			{
				if (_buildInfo.Layouts.HasLayout("hsdt"))
				{
					ScriptFiles = _tags.FindTagsByGroup("hsdt").Select(t => new HsdtScriptFile(t, _fileNames.GetTagName(t.Index), MetaArea, _buildInfo, StringIDs, _expander)).ToArray();
				}
				else if (_buildInfo.Layouts.HasLayout("scnr"))
				{
					ScriptFiles = _tags.FindTagsByGroup("scnr").Select(t => new ScnrScriptFile(t, _fileNames.GetTagName(t.Index), MetaArea, _buildInfo, StringIDs, _expander, Allocator)).ToArray();
				}
				else
                {
					ScriptFiles = new IScriptFile[0];
				}
			}
		}

		private void LoadSimulationDefinitions(IReader reader)
		{
			if (_tags != null && _buildInfo.Layouts.HasLayout("scnr") && _buildInfo.Layouts.HasLayout("simulation definition table element"))
			{
				ITag scnr = _tags.FindTagByGroup("scnr");
				if (scnr != null)
					_simulationDefinitions = new ThirdGenSimulationDefinitionTable(scnr, _tags, reader, MetaArea, Allocator, _buildInfo, _expander);
			}
		}

		private void LoadEffects(IReader reader)
		{
			if (_tags != null && _buildInfo.Layouts.HasLayout("scnr") && _buildInfo.Layouts.HasLayout("structured effect interop element"))
			{
				ITag scnr = _tags.GetGlobalTag(CharConstant.FromString("scnr"));
				if (scnr != null)
					_effects = new EffectInterop(scnr, reader, MetaArea, Allocator, _buildInfo, _expander);
			}
		}

		private void WriteHeader(IWriter writer)
		{
			// Update tagname and stringid info (so. ugly.)
			_header.FileNameCount = _fileNames.Count;
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