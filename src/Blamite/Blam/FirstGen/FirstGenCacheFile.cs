using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blamite.Blam.FirstGen.Structures;
using Blamite.Blam.Localization;
using Blamite.Blam.Resources;
using Blamite.Blam.Resources.Sounds;
using Blamite.Blam.Scripting;
using Blamite.Blam.Shaders;
using Blamite.Blam.Util;
using Blamite.IO;
using Blamite.Serialization;

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
        private FirstGenPointerExpander _expander;
        private Endian _endianness;
        private EffectInterop _effects;


        public FirstGenCacheFile(IReader reader, EngineDescription buildInfo, string buildString)
        {
            _endianness = reader.Endianness;
            _buildInfo = buildInfo;
            _segmenter = new FileSegmenter(buildInfo.SegmentAlignment);
            _expander = new FirstGenPointerExpander();

            // TODO (Dragon): not sure if this is right for first gen
            Allocator = new MetaAllocator(this, 0x10000);

            Load(reader, buildInfo, buildString);
        }


        public void SaveChanges(IStream stream)
        {
            CalculateChecksum(stream);
            WriteHeader(stream);
            // TODO: Write the tag table
        }


        public int HeaderSize {
            get { return _header.HeaderSize; }
        }

        public long FileSize {
            get { return _header.FileSize; }
        }

        public CacheFileType Type {
            get { return _header.Type; }
        }

        public EngineType Engine {
            get { return EngineType.FirstGeneration; }
        }

        public string BuildString {
            get { return _header.BuildString; }
        }

        public string InternalName {
            get { return _header.InternalName; }
        }

        public string ScenarioName {
            get { return _header.ScenarioName; }
        }

        public int XDKVersion {
            get { return _header.XDKVersion; }
            set { _header.XDKVersion = value; }
        }

        public bool ZoneOnly {
            get { return false; }
        }

        public FileSegmentGroup MetaArea {
            get { return _header.MetaArea; }
        }

        public SegmentPointer IndexHeaderLocation {
            get { return _header.IndexHeaderLocation; }
            set { _header.IndexHeaderLocation = value; }
        }

        public Partition[] Partitions {
            get { return _header.Partitions; }
        }

        public FileSegment RawTable {
            get { return _header.RawTable; }
        }

        public FileSegmentGroup LocaleArea {
            get { return _header.LocaleArea; }
        }

        public FileSegmentGroup StringArea {
            get { return _header.StringArea; }
        }

        // TODO (Dragon): firstgen has no stringIDs
        public FileSegment StringIDIndexTable {
            get { return _header.StringIDIndexTable; }
        }

        // TODO (Dragon): firstgen has no stringIDs
        public FileSegment StringIDDataTable {
            get { return _header.StringIDData; }
        }

        public FileSegment FileNameIndexTable {
            get { return _header.FileNameIndexTable; }
        }

        public FileSegment FileNameDataTable {
            get { return _header.FileNameData; }
        }

        public FileNameSource FileNames {
            get { return _fileNames; }
        }

        public StringIDSource StringIDs {
            get { return _stringIDs; }
        }

        public ILanguagePackLoader Languages {
            get { return _languageLoader; }
        }

        public IList<ITagGroup> TagGroups {
            get { return _tags.Groups; }
        }

        public TagTable Tags {
            get { return _tags; }
        }

        public IResourceManager Resources {
            get { return null; }
        }

        public IResourceMetaLoader ResourceMetaLoader {
            get { return new FirstGenResourceMetaLoader(); }
        }

        public IEnumerable<FileSegment> Segments {
            get { return _segmenter.GetWrappers(); }
        }

        public MetaAllocator Allocator { get; private set; }

        public IScriptFile[] ScriptFiles {
            get { return new IScriptFile[0]; }
        }

        public IShaderStreamer ShaderStreamer {
            get { return null; }
        }

        public ISimulationDefinitionTable SimulationDefinitions {
            get { return null; }
        }

        public IList<ITagInterop> TagInteropTable {
            get { return null; }
        }

        public IPointerExpander PointerExpander {
            get { return _expander; }
        }

        public Endian Endianness {
            get { return _endianness; }
        }

        public EffectInterop EffectInterops {
            get { return _effects; }
        }

        public string FileName => throw new NotImplementedException();

        public SoundResourceManager SoundGestalt => throw new NotImplementedException();

        public SoundResourceGestalt LoadSoundResourceGestaltData(IReader reader)
        {
            throw new NotImplementedException();
        }

        private void Load(IReader reader, EngineDescription buildInfo, string buildString)
        {
            _header = LoadHeader(reader, buildInfo, buildString);
            _tags = LoadTagTable(reader, buildInfo);
            _fileNames = LoadFileNames(reader, buildInfo);
            
            // firstgen has no StringIDs
            _stringIDs = LoadStringIDs(reader, buildInfo);

            // hack to get scenario name
            reader.SeekTo(MetaArea.Offset);
            StructureValueCollection values = StructureReader.ReadStructure(reader, buildInfo.Layouts.GetLayout("meta header"));
            
            // TODO (Dragon): idk if we should mask this like this
            var scenarioIndex = (int)values.GetInteger("scenario datum index") & 0xFFFF;
            _header.ScenarioName = _fileNames.GetTagName(scenarioIndex);
        }

        private FirstGenHeader LoadHeader(IReader reader, EngineDescription buildInfo, string buildString)
        {
            reader.SeekTo(0);
            StructureValueCollection values = StructureReader.ReadStructure(reader, buildInfo.Layouts.GetLayout("header"));


            // hack to pack meta header size for metaOffsetMask calculation
            var oldReadPos = reader.Position;
            reader.SeekTo((long)values.GetInteger("meta offset"));
            var tagTableOffset = reader.ReadUInt32();
            values.SetInteger("meta header size", (ulong)buildInfo.Layouts.GetLayout("meta header").Size);
            values.SetInteger("tag table offset", (ulong)tagTableOffset);
            reader.SeekTo(oldReadPos);

            return new FirstGenHeader(values, buildInfo, buildString, _segmenter);
        }

        private FirstGenTagTable LoadTagTable(IReader reader, EngineDescription buildInfo)
        {
            reader.SeekTo(MetaArea.Offset);
            StructureValueCollection values = StructureReader.ReadStructure(reader, buildInfo.Layouts.GetLayout("meta header"));
            return new FirstGenTagTable(reader, values, MetaArea, buildInfo);
        }

        private IndexedFileNameSource LoadFileNames(IReader reader, EngineDescription buildInfo)
        {
            //var strings = new IndexedStringTable(reader, _header.FileNameCount, _header.FileNameIndexTable, _header.FileNameData,
            //    buildInfo.TagNameKey);
            var strings = new FirstGenIndexedStringTable(reader, _tags);
            return new IndexedFileNameSource(strings);
        }

        private IndexedStringIDSource LoadStringIDs(IReader reader, EngineDescription buildInfo)
        {
            var strings = new IndexedStringTable(reader, _header.StringIDCount, _header.StringIDIndexTable, _header.StringIDData,
                buildInfo.StringIDKey);
            return new IndexedStringIDSource(strings, new LengthBasedStringIDResolver(strings));
        }

        private void CalculateChecksum(IReader reader)
        {
			// XOR all of the uint32s in the file after the header
			// based on http://codeescape.com/2009/05/optimized-c-halo-2-map-signing-algorithm/
			uint checksum = 0;
			int blockSize = 0x10000;
			reader.SeekTo(_header.HeaderSize);

			while (reader.Position < reader.Length)
			{
				int actualSize = Math.Min(blockSize, (int)(reader.Length - reader.Position));
				byte[] block = reader.ReadBlock(actualSize);
				for (int i = 0; i < block.Length; i+=4)
					checksum ^= BitConverter.ToUInt32(block, i);
			}

			_header.Checksum = checksum;
		}

        private void WriteHeader(IWriter writer)
        {
            writer.SeekTo(0);
            StructureWriter.WriteStructure(_header.Serialize(), _buildInfo.Layouts.GetLayout("header"), writer);
        }

    }
}
