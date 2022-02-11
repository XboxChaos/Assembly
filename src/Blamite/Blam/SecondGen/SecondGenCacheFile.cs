﻿using System;
using System.Collections.Generic;
using Blamite.Blam.Localization;
using Blamite.Blam.Resources;
using Blamite.Blam.Resources.Sounds;
using Blamite.Blam.Scripting;
using Blamite.Blam.SecondGen.Localization;
using Blamite.Blam.SecondGen.Structures;
using Blamite.Blam.Shaders;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.SecondGen
{
	public class SecondGenCacheFile : ICacheFile
	{
		private readonly EngineDescription _buildInfo;
		private readonly FileSegmenter _segmenter;
		private IndexedFileNameSource _fileNames;
		private SecondGenHeader _header;
		private SecondGenLanguageGlobals _languageInfo;
		private SecondGenLanguagePackLoader _languageLoader;
		private IndexedStringIDSource _stringIDs;

		// NOTE (Dragon): this may cause problems in the future, but since the beta uses a first gen style tag table
		//				  we need to be able to use one here. we *DO* cast to a SecondGenTagTable for other builds though
		//				  (see SaveChanges() and the TagGroups property)
		//private SecondGenTagTable _tags;
		private TagTable _tags;				
		
		
		private DummyPointerExpander _expander;
		private Endian _endianness;
		private SoundResourceManager _soundGestalt;
		private SecondGenSimulationDefinitionTable _simulationDefinitions;

		public SecondGenCacheFile(IReader reader, EngineDescription buildInfo, string filePath)
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
			//_tags.SaveChanges(stream);
			if (BuildString == "02.06.28.07902")
            {
				// TODO (Dragon): forgot that first gen tag tables never got proper saving lol
				//((FirstGen.Structures.FirstGenTagTable)_tags).SaveChanges(stream);
				throw new NotImplementedException("Saving halo 2 beta tag table not *properly* supported, but tag edits may still work if you continue (blame Dragon)");
			}
			else
            {
				((SecondGenTagTable)_tags).SaveChanges(stream);
			}
			
			WriteStringBlock(stream);
			_fileNames.SaveChanges(stream);
			_stringIDs.SaveChanges(stream);
			if (_simulationDefinitions != null)
				_simulationDefinitions.SaveChanges(stream);
			int checksumOffset = WriteHeader(stream);
			WriteLanguageInfo(stream);

			if (checksumOffset != -1)
			{
				//checksum needs to be handled last due to WriteLanguageInfo writing where we need to calculate,
				//and WriteHeader updates important info for languages so it has to come before that, (but maybe that should be run separately?)
				//leaving this hacky checksum writing
				_header.Checksum = ICacheFileExtensions.GenerateChecksum(this, stream);
				stream.SeekTo(checksumOffset);
				stream.WriteUInt32(_header.Checksum);
			}
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

		public IList<ITagGroup> TagGroups
		{
			//get { return _tags.Groups; }
			get {
				if (BuildString == "02.06.28.07902")
					return ((FirstGen.Structures.FirstGenTagTable)_tags).Groups;
				else
					return ((SecondGenTagTable)_tags).Groups;
			}
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
			get { return (_languageInfo != null ? _languageInfo.LocaleArea : null); }
		}

		public ILanguagePackLoader Languages
		{
			get { return _languageLoader; }
		}

		public IResourceManager Resources
		{
			get { return null; }
		}

		public IResourceMetaLoader ResourceMetaLoader
		{
			get { return new SecondGenResourceMetaLoader(); }
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

		public IShaderStreamer ShaderStreamer
		{
			get { return null; }
		}

		public ISimulationDefinitionTable SimulationDefinitions
		{
			get { return _simulationDefinitions; }
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

		public SoundResourceManager SoundGestalt
		{
			get { return _soundGestalt; }
		}

		private void Load(IReader reader)
		{
			_header = LoadHeader(reader, out uint primaryMask);
			_tags = LoadTagTable(reader, primaryMask);
			_fileNames = LoadFileNames(reader);
			_stringIDs = LoadStringIDs(reader);

			LoadLanguageGlobals(reader);
			LoadScriptFiles();
			LoadSimulationDefinitions(reader);
		}

		private SecondGenHeader LoadHeader(IReader reader, out uint primaryMask)
		{
			primaryMask = 0;
			reader.SeekTo(0);
			StructureValueCollection values = StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("header"));

			//set up a mask for xbox if needed
			if (!values.HasInteger("meta offset mask"))
			{
				//oh boy
				StructureLayout indexHeaderLayout = _buildInfo.Layouts.GetLayout("meta header");
				StructureLayout tagElementLayout = _buildInfo.Layouts.GetLayout("tag element");

				uint indexHeaderOffset = (uint)values.GetInteger("meta offset");
				reader.SeekTo(indexHeaderOffset);
				uint firstVal = reader.ReadUInt32();
				reader.SeekTo(indexHeaderOffset + indexHeaderLayout.GetFieldOffset("tag table offset"));
				uint tagTableAddress = reader.ReadUInt32();
				primaryMask = firstVal - (uint)indexHeaderLayout.Size;
				uint tagTableOffset = tagTableAddress - primaryMask + indexHeaderOffset;

				reader.SeekTo(tagTableOffset + tagElementLayout.GetFieldOffset("offset"));
				uint firstTagAddress = reader.ReadUInt32();

				values.SetInteger("xbox meta offset mask", firstTagAddress - (uint)values.GetInteger("tag data offset"));
			}

			return new SecondGenHeader(values, _buildInfo, _segmenter);
		}

		private TagTable LoadTagTable(IReader reader, uint primaryMask)
		{
			
			// TODO (Dragon): this stuff is actually unused for everything other than the beta
			reader.SeekTo(MetaArea.Offset);
			StructureValueCollection values = StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("meta header"));

			if (primaryMask > 0)
				values.SetInteger("meta header mask", primaryMask);
			
			// TODO (Dragon): this is the hackiest thing so far
			if (_buildInfo.BuildVersion == "02.06.28.07902")
				return new FirstGen.Structures.FirstGenTagTable(reader, values, MetaArea, _buildInfo);
			
			return new SecondGenTagTable(reader, MetaArea, Allocator, _buildInfo);
		}

		private IndexedFileNameSource LoadFileNames(IReader reader)
		{
			IndexedStringTable strings;
			if (_buildInfo.BuildVersion == "02.06.28.07902")
			{
				strings = new FirstGenIndexedStringTable(reader, (FirstGen.Structures.FirstGenTagTable)_tags);
			}
			else
			{
				strings = new IndexedStringTable(reader, _header.FileNameCount, _header.FileNameIndexTable, _header.FileNameData,
				_buildInfo.TagNameKey);
			}
			return new IndexedFileNameSource(strings);
		}

		private IndexedStringIDSource LoadStringIDs(IReader reader)
		{
			var strings = new IndexedStringTable(reader, _header.StringIDCount, _header.StringIDIndexTable, _header.StringIDData,
				_buildInfo.StringIDKey);
			return new IndexedStringIDSource(strings, new LengthBasedStringIDResolver(strings));
		}

		private void LoadLanguageGlobals(IReader reader)
		{
			// Find the language data
			ITag languageTag;
			StructureLayout tagLayout;
			if (!FindLanguageTable(out languageTag, out tagLayout))
			{
				// No language data
				_languageLoader = new SecondGenLanguagePackLoader();
				return;
			}

			// Read it
			reader.SeekTo(languageTag.MetaLocation.AsOffset());
			StructureValueCollection values = StructureReader.ReadStructure(reader, tagLayout);
			_languageInfo = new SecondGenLanguageGlobals(values, _segmenter, _buildInfo);
			_languageLoader = new SecondGenLanguagePackLoader(this, _languageInfo, _buildInfo, reader);
		}

		private bool FindLanguageTable(out ITag tag, out StructureLayout layout)
		{
			tag = null;
			layout = null;

			if (_tags == null)
				return false;

			if (_buildInfo.Layouts.HasLayout("matg"))
			{
				tag = _tags.FindTagByGroup("matg");
				layout = _buildInfo.Layouts.GetLayout("matg");
			}
			return (tag != null && layout != null && tag.MetaLocation != null);
		}

		private void LoadScriptFiles()
		{
			ScriptFiles = new IScriptFile[0];

			if (_tags != null)
			{
				List<IScriptFile> l_scriptfiles = new List<IScriptFile>();

				if (_buildInfo.Layouts.HasLayout("scnr"))
				{
					foreach (ITag hs in _tags.FindTagsByGroup("scnr"))
                    {
						l_scriptfiles.Add(new ScnrScriptFile(hs, _fileNames.GetTagName(hs.Index), MetaArea, _buildInfo, StringIDs, _expander, Allocator));
					}
				}
				else
                {
					return;
				}

				ScriptFiles = l_scriptfiles.ToArray();
			}
		}

		private void LoadSimulationDefinitions(IReader reader)
		{
			if (_tags != null && _buildInfo.Layouts.HasLayout("scnr") && _buildInfo.Layouts.HasLayout("simulation definition table element"))
			{
				ITag scnr = _tags.FindTagByGroup("scnr");
				if (scnr != null)
					_simulationDefinitions = new SecondGenSimulationDefinitionTable(scnr, _tags, reader, MetaArea, Allocator, _buildInfo);
			}
		}

		private void WriteStringBlock(IStream stream)
		{
			var segment = StringArea.Segments[0];

			int newSize = _stringIDs.Count * 0x80;

			if (segment.ActualSize < newSize)
				segment.Resize(newSize, stream);

			stream.SeekTo(segment.Offset);

			for (int i = 0; i < _stringIDs.Count; i++)
			{
				byte[] data = new byte[0x80];
				byte[] stringData = System.Text.Encoding.UTF8.GetBytes(_stringIDs.GetString(i));

				Array.Copy(stringData, 0, data, 0, stringData.Length > 0x80 ? 0x80 : stringData.Length);
				stream.WriteBlock(data);
			}
		}

		private int WriteHeader(IWriter writer)
		{
			// Update tagname and stringid info (so. ugly.)
			_header.FileNameCount = _fileNames.Count;
			_header.StringIDCount = _stringIDs.Count;

			StructureLayout headerLayout = _buildInfo.Layouts.GetLayout("header");
			writer.SeekTo(0);
			StructureWriter.WriteStructure(_header.Serialize(), headerLayout, writer);
			int checksumOffset = -1;
			if (headerLayout.HasField("checksum"))
				checksumOffset = headerLayout.GetFieldOffset("checksum");
			return checksumOffset;
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