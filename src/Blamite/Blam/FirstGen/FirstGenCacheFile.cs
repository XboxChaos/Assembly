using System;
using System.Collections.Generic;
using Blamite.Blam.FirstGen.Structures;
using Blamite.Blam.Localization;
using Blamite.Blam.Resources;
using Blamite.Blam.Resources.Sounds;
using Blamite.Blam.Scripting;
using Blamite.Blam.Shaders;
using Blamite.Blam.Util;
using Blamite.IO;
using Blamite.Serialization;
using Blamite.Util;

namespace Blamite.Blam.FirstGen
{
	public class FirstGenCacheFile : ICacheFile
	{
		private readonly EngineDescription _buildInfo;
		private readonly ILanguagePackLoader _languageLoader = new DummyLanguagePackLoader();
		private readonly FileSegmenter _segmenter;
		private IndexedFileNameSource _fileNames;
		private FirstGenHeader _header;
		private IndexedStringIDSource _stringIDs;
		private FirstGenTagTable _tags;
		private DummyPointerExpander _expander;
		private Endian _endianness;

		public FirstGenCacheFile(IReader reader, EngineDescription buildInfo, string filePath)
		{
			FilePath = filePath;
			_endianness = reader.Endianness;
			_buildInfo = buildInfo;
			_segmenter = new FileSegmenter(buildInfo.SegmentAlignment);
			_expander = new DummyPointerExpander();

			// TODO (Dragon): not sure if this is right for first gen
			Allocator = new MetaAllocator(this, 0x10000);

			Load(reader);
		}

		public void SaveChanges(IStream stream)
		{
			// TODO: Write the tag table
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
			get { return EngineType.FirstGeneration; }
		}

		public string BuildString
		{
			get { return _header.BuildString; }
		}

		public string InternalName
		{
			get { return _header.InternalName; }
		}

		public string ScenarioName
		{
			get { return _header.ScenarioName; }
		}
		public int XDKVersion
		{
			get { return _header.XDKVersion; }
			set { _header.XDKVersion = value; }
		}

		public bool ZoneOnly
		{
			get { return false; }
		}

		public FileSegmentGroup MetaArea
		{
			get { return _header.MetaArea; }
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

		public FileSegmentGroup LocaleArea
		{
			get { return _header.LocaleArea; }
		}

		public FileSegmentGroup StringArea
		{
			get { return _header.StringArea; }
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
		public FileNameSource FileNames
		{
			get { return _fileNames; }
		}

		public StringIDSource StringIDs
		{
			get { return _stringIDs; }
		}

		public ILanguagePackLoader Languages
		{
			get { return _languageLoader; }
		}

		public IList<ITagGroup> TagGroups
		{
			get { return _tags.Groups; }
		}

		public TagTable Tags
		{
			get { return _tags; }
		}

		public IResourceManager Resources
		{
			get { return null; }
		}

		public IResourceMetaLoader ResourceMetaLoader
		{
			get { return new FirstGenResourceMetaLoader(); }
		}

		public IEnumerable<FileSegment> Segments
		{
			get { return _segmenter.GetWrappers(); }
		}

		public MetaAllocator Allocator { get; private set; }

		public IScriptFile[] ScriptFiles { get; private set; }

		public IShaderStreamer ShaderStreamer
		{
			get { return null; }
		}

		public ISimulationDefinitionTable SimulationDefinitions
		{
			get { return null; }
		}

		public IList<ITagInterop> TagInteropTable
		{
			get { return null; }
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

		public SoundResourceManager SoundGestalt => throw new NotImplementedException();

		private void Load(IReader reader)
		{
			_header = LoadHeader(reader, out uint mask);
			_tags = LoadTagTable(reader, mask);
			_fileNames = LoadFileNames(reader);
			
			_stringIDs = LoadStringIDs(reader);

			//header doesn't contain a scenario path, but later engines do so might as well grab it
			ITag scenario = _tags.GetGlobalTag(CharConstant.FromString("scnr"));
			_header.ScenarioName = _fileNames.GetTagName(scenario.Index);

			LoadScriptFiles();
		}

		private FirstGenHeader LoadHeader(IReader reader, out uint mask)
		{
			mask = 0;
			reader.SeekTo(0);
			StructureValueCollection values = StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("header"));

			// hack to pack meta header size for metaOffsetMask calculation
			var oldReadPos = reader.Position;

			if (values.HasInteger("tag data offset"))
			{
				//oh boy
				StructureLayout indexHeaderLayout = _buildInfo.Layouts.GetLayout("meta header");
				StructureLayout tagElementLayout = _buildInfo.Layouts.GetLayout("tag element");

				uint indexHeaderOffset = (uint)values.GetInteger("meta offset");
				reader.SeekTo(indexHeaderOffset);
				uint firstVal = reader.ReadUInt32();
				reader.SeekTo(indexHeaderOffset + indexHeaderLayout.GetFieldOffset("tag table offset"));
				uint tagTableAddress = reader.ReadUInt32();
				mask = firstVal - (uint)indexHeaderLayout.Size;
				uint tagTableOffset = tagTableAddress - mask + indexHeaderOffset;

				reader.SeekTo(tagTableOffset + tagElementLayout.GetFieldOffset("offset"));
				uint firstTagAddress = reader.ReadUInt32();

				values.SetInteger("xbox meta offset mask", firstTagAddress - (uint)values.GetInteger("tag data offset"));
			}
			else
			{
				reader.SeekTo((long)values.GetInteger("meta offset"));
				var tagTableOffset = reader.ReadUInt32();
				values.SetInteger("meta header size", (ulong)_buildInfo.Layouts.GetLayout("meta header").Size);
				values.SetInteger("tag table offset", (ulong)tagTableOffset);
			}
			

			return new FirstGenHeader(values, _buildInfo, _segmenter);
		}

		private FirstGenTagTable LoadTagTable(IReader reader, uint mask)
		{
			reader.SeekTo(MetaArea.Offset);
			StructureValueCollection values = StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("meta header"));

			if (mask > 0)
				values.SetInteger("meta header mask", mask);
			
			return new FirstGenTagTable(reader, values, MetaArea, _buildInfo);
		}

		private IndexedFileNameSource LoadFileNames(IReader reader)
		{
			//var strings = new IndexedStringTable(reader, _header.FileNameCount, _header.FileNameIndexTable, _header.FileNameData,
			//    _buildInfo.TagNameKey);
			var strings = new FirstGenIndexedStringTable(reader, _tags);
			return new IndexedFileNameSource(strings);
		}

		private IndexedStringIDSource LoadStringIDs(IReader reader)
		{
			var strings = new IndexedStringTable(reader, _header.StringIDCount, _header.StringIDIndexTable, _header.StringIDData,
				_buildInfo.StringIDKey);
			return new IndexedStringIDSource(strings, new LengthBasedStringIDResolver(strings));
		}

		private void LoadScriptFiles()
		{
			if (_tags != null)
			{
				ScriptFiles = new IScriptFile[0];

				if (_buildInfo.Layouts.HasLayout("scnr"))
				{
					//caches are intended for 1 scenario, so only load the *real* one
					ITag hs = _tags.GetGlobalTag(CharConstant.FromString("scnr"));
					if (hs != null)
					{
						ScriptFiles = new IScriptFile[1];
						ScriptFiles[0] = new ScnrScriptFile(hs, _fileNames.GetTagName(hs.Index), MetaArea, _buildInfo, StringIDs, _expander, Allocator);
					}
				}
			}
		}

		private void WriteHeader(IWriter writer)
		{
			writer.SeekTo(0);
			StructureWriter.WriteStructure(_header.Serialize(), _buildInfo.Layouts.GetLayout("header"), writer);
		}

	}
}
