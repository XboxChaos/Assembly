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

		private FileSegmentGroup[] _bspAreas;

		public FirstGenCacheFile(IReader reader, EngineDescription buildInfo, string filePath)
		{
			FilePath = filePath;
			_endianness = reader.Endianness;
			_buildInfo = buildInfo;
			_segmenter = new FileSegmenter(buildInfo.SegmentAlignment);
			_expander = new DummyPointerExpander();

			Allocator = new MetaAllocator(this, 0x1000);

			Load(reader);
		}

		public void SaveChanges(IStream stream)
		{
			// TODO: Write the tag table
			_header.Checksum = ICacheFileExtensions.GenerateChecksum(this, stream);
			WriteHeader(stream);
		}

		public void SaveTagNames(IStream stream)
		{
			SaveChanges(stream);
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
			get { return new DummyResourceMetaLoader(); }
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

		public FileSegmentGroup[] BSPAreas
		{
			get { return _bspAreas; }
		}

		private void Load(IReader reader)
		{
			_header = LoadHeader(reader, out uint primaryMask);
			_tags = LoadTagTable(reader, primaryMask);
			_fileNames = LoadFileNames(reader);
			
			_stringIDs = LoadStringIDs(reader);

			FixupStructureTags(reader);

			//header doesn't contain a scenario path, but later engines do so might as well grab it
			ITag scenario = _tags.GetGlobalTag(CharConstant.FromString("scnr"));
			_header.ScenarioName = _fileNames.GetTagName(scenario.Index) ?? scenario.Index.ToString();

			LoadScriptFiles();
		}

		private FirstGenHeader LoadHeader(IReader reader, out uint primaryMask)
		{
			primaryMask = 0;
			reader.SeekTo(0);
			StructureValueCollection values = StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("header"));

			//h2 alpha
			if (values.HasInteger("tag data offset"))
			{
				//oh boy
				StructureLayout indexHeaderLayout = _buildInfo.Layouts.GetLayout("meta header");
				StructureLayout tagElementLayout = _buildInfo.Layouts.GetLayout("tag element");

				uint indexHeaderOffset = (uint)values.GetInteger("meta offset");
				reader.SeekTo(indexHeaderOffset + indexHeaderLayout.GetFieldOffset("tag table address"));
				uint tagTableAddress = reader.ReadUInt32();

				primaryMask = tagTableAddress - (uint)indexHeaderLayout.Size;
				uint tagTableOffset = tagTableAddress - primaryMask + indexHeaderOffset;

				reader.SeekTo(tagTableOffset + tagElementLayout.GetFieldOffset("memory address"));
				uint firstTagAddress = reader.ReadUInt32();

				values.SetInteger("xbox meta offset mask", firstTagAddress - (uint)values.GetInteger("tag data offset"));
				values.SetInteger("xbox bsp mask", primaryMask - indexHeaderOffset);
			}
			else
			{
				reader.SeekTo((long)values.GetInteger("meta offset"));
				var tagTableOffset = reader.ReadUInt32();
				values.SetInteger("meta header size", (ulong)_buildInfo.Layouts.GetLayout("meta header").Size);
				values.SetInteger("tag table offset", (ulong)tagTableOffset);
			}

			values.SetInteger("true filesize", (uint)reader.Length);

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

		private void FixupStructureTags(IReader reader)
		{
			if (_buildInfo.Layouts.HasLayout("scenario bsp table element"))
			{
				var scenarioTag = _tags.GetGlobalTag(CharConstant.FromString("scnr"));
				if (scenarioTag != null)
				{
					reader.SeekTo(scenarioTag.MetaLocation.AsOffset());
					StructureValueCollection scenarioValues = StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("scnr"));

					int bspCount = (int)scenarioValues.GetInteger("number of scenario bsps");
					uint bspAddr = (uint)scenarioValues.GetInteger("scenario bsp table address");

					StructureLayout layout = _buildInfo.Layouts.GetLayout("scenario bsp table element");
					StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, bspCount, bspAddr, layout, MetaArea);

					_bspAreas = new FileSegmentGroup[bspCount];

					for (int i = 0; i < bspCount; i++)
					{
						StructureValueCollection ent = entries[i];

						uint offset = (uint)ent.GetInteger("data offset");
						if (offset == 0)
							continue;

						//bsp
						ITag bspTag = _tags[(int)ent.GetInteger("sbsp datum") & 0xFFFF];
						reader.SeekTo(offset);
						StructureLayout bspHeaderLayout = _buildInfo.Layouts.GetLayout("bsp header");
						StructureValueCollection bspHeadValues = StructureReader.ReadStructure(reader, bspHeaderLayout);

						//register area
						FileSegment bspSegment = new FileSegment(
							_segmenter.DefineSegment(offset, (uint)ent.GetInteger("data size"), 0x4, SegmentResizeOrigin.End), _segmenter);

						_bspAreas[i] = new FileSegmentGroup(new MetaOffsetConverter(bspSegment, (uint)ent.GetInteger("data address")));
						_bspAreas[i].AddSegment(bspSegment);

						bspTag.MetaLocation = SegmentPointer.FromPointer((uint)bspHeadValues.GetInteger("bsp address"), _bspAreas[i]);
						bspTag.Source = TagSource.BSP;

						//lightmap
						if (ent.HasInteger("ltmp datum"))
						{
							uint ltmpdatum = (uint)ent.GetInteger("ltmp datum");
							if (ltmpdatum != 0xFFFFFFFF)
							{
								ITag ltmpTag = _tags[(int)ent.GetInteger("ltmp datum") & 0xFFFF];

								uint lightaddr = (uint)bspHeadValues.GetInteger("lightmap address");
								ltmpTag.MetaLocation = SegmentPointer.FromPointer(lightaddr, _bspAreas[i]);
								ltmpTag.Source = TagSource.BSP;
							}
						}

					}
				}
			}
		}

		private IndexedFileNameSource LoadFileNames(IReader reader)
		{
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
						ScriptFiles[0] = new ScnrScriptFile(hs, _fileNames.GetTagName(hs.Index) ?? hs.Index.ToString(), hs.MetaLocation.BaseGroup, _buildInfo, StringIDs, _expander, Allocator);
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
