using System.Collections.Generic;
using Blamite.Blam.Resources;
using Blamite.Blam.Scripting;
using Blamite.Blam.Shaders;
using Blamite.Blam.Util;
using Blamite.Blam.Eldorado.Structures;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Blam.Localization;
using Blamite.Blam.Resources.Sounds;
using System.IO;
using System.Linq;

namespace Blamite.Blam.Eldorado
{
	/// <summary>
	///     A eldorado Blam (map) cache file.
	/// </summary>
	public class EldoradoCacheFile : ICacheFile
	{
		private readonly EngineDescription _buildInfo;
		private readonly FileSegmenter _segmenter;
		private readonly FileSegmenter _tagSegmenter;
		private CSVFilenameSource _fileNames;
		private EldoradoCacheHeader _header;
		private IndexedStringIDSource _stringIds;
		private EldoradoTagTable _tags;
		private DummyPointerExpander _expander;
		private Endian _endianness;

		public EldoradoCacheFile(IReader reader, EngineDescription buildInfo, string filePath)
		{
			FilePath = filePath;
			_endianness = reader.Endianness;
			_buildInfo = buildInfo;
			_segmenter = new FileSegmenter(buildInfo.SegmentAlignment);
			_tagSegmenter = new FileSegmenter(4);
			_expander = new DummyPointerExpander();
			Allocator = new MetaAllocator(this, 4);
			Load(reader);
		}

		public EldoradoCacheHeader FullHeader
		{
			get { return _header; }
		}

		public void SaveChanges(IStream stream)
		{
			_tags.SaveChanges(stream);
			_stringIds.SaveChanges(null);
		}

		public void SaveTagNames(IStream stream)
		{
			_fileNames.SaveChanges(TagListPath);
		}

		public string FilePath { get; private set; }
		public string TagFilePath { get; private set; }
		public string StringFilePath { get; private set; }
		public string TagListPath { get; private set; }

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
			get { return EngineType.Eldorado; }
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
			get { return 0; }
			set { }
		}

		public bool ZoneOnly
		{
			get { return false; }
		}

		public SegmentPointer IndexHeaderLocation
		{
			get { return _header.IndexHeaderLocation; }
			set { _header.IndexHeaderLocation = value; }
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

		public IList<ITagGroup> TagGroups
		{
			get { return _tags.Groups; }
		}

		public IResourceManager Resources
		{
			get { return null; }
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
			get { return null; }
		}

		public ILanguagePackLoader Languages
		{
			get { return null; }
		}

		public IResourceMetaLoader ResourceMetaLoader
		{
			get { return null; }
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
			get { return null; }
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
			get { return null; }
		}

		public SoundResourceManager SoundGestalt
		{
			get { return null; }
		}

		public FileSegmentGroup[] BSPAreas
		{
			get { return null; }
		}

		private void Load(IReader reader)
		{
			var fileDir = Path.GetDirectoryName(FilePath);

			string tagFile = Path.Combine(fileDir, "tags.dat");
			string stringFile = Path.Combine(fileDir, "string_ids.dat");
			string nameFile = Path.Combine(fileDir, "tag_list.csv");

			if (!File.Exists(tagFile))
				throw new FileNotFoundException("Cannot load eldorado cache file without a tags file. Please make sure tags.dat is in the same folder as the map file.");

			if (!File.Exists(stringFile))
				throw new FileNotFoundException("Cannot load eldorado cache file without a stringid file. Please make sure string_ids.dat is in the same folder as the map file.");

			TagFilePath = tagFile;
			StringFilePath = stringFile;
			TagListPath = nameFile;

			using (FileStream tagStream = File.OpenRead(TagFilePath))
			using (EndianReader tagReader = new EndianReader(tagStream, _endianness))
			{
				LoadHeader(reader, (ulong)tagReader.Length);
				LoadStringIDs(stringFile);
				LoadTags(tagReader);
				LoadFileNames(TagListPath);
				LoadScriptFiles();
			}
		}

		private void LoadHeader(IReader mapReader, ulong tagLength)
		{
			mapReader.SeekTo(0);

			StructureValueCollection values = StructureReader.ReadStructure(mapReader, _buildInfo.Layouts.GetLayout("header"));

			_header = new EldoradoCacheHeader(values, _buildInfo, _segmenter);
		}

		private void LoadTags(IReader tagReader)
		{
			_tags = new EldoradoTagTable(tagReader, _buildInfo, _tagSegmenter);
		}

		private void LoadFileNames(string nameFile)
		{
			string fallback = null;
			if (_buildInfo.FallbackTagNameFolder != null)
				fallback = Path.Combine(_buildInfo.FallbackTagNameFolder, _buildInfo.BuildVersion.Replace(':', '_') + ".csv");

			_fileNames = new CSVFilenameSource(nameFile, fallback);
		}

		private void LoadStringIDs(string stringFile)
		{
			var stringTable = new ExternalIndexedStringTable(stringFile, _endianness, _buildInfo.StringIDKey);
			_stringIds = new IndexedStringIDSource(stringTable, new StringIDNamespaceResolver(_buildInfo.StringIDInfo));
		}

		private void LoadScriptFiles()
		{
			if (_tags != null)
			{
				ScriptFiles = new IScriptFile[0];

				if (_buildInfo.Layouts.HasLayout("scnr"))
				{
					ScriptFiles = _tags.FindTagsByGroup("scnr").Select(t => new ScnrScriptFile(t, _fileNames.GetTagName(t.Index) ?? t.Index.ToString(), t.MetaLocation.BaseGroup, _buildInfo, StringIDs, _expander, Allocator)).ToArray();
				}
			}
		}

	}
}