using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.Flexibility;

namespace ExtryzeDLL.Blam.SecondGen.Structures
{
    public class SecondGenHeader : ICacheFileInfo
    {
        private IdentityOffsetConverter _identityConverter = new IdentityOffsetConverter();

        public SecondGenHeader(StructureValueCollection values, BuildInformation info, string buildString)
        {
            BuildString = buildString;
            HeaderSize = info.HeaderSize;
            Load(values);
        }

        public int HeaderSize { get; private set; }
        public uint FileSize { get; set; }

        public CacheFileType Type { get; private set; }

        public string InternalName { get; private set; }
        public string ScenarioName { get; private set; }

        public string BuildString { get; private set; }

        public Pointer IndexHeaderLocation { get; private set; }

        public Pointer MetaBase
        {
            get { return new Pointer(MetaOffset, _identityConverter); }
        }

        public uint VirtualBaseAddress { get; set; }
        public uint MetaOffset { get; set; }
        public uint MetaSize { get; set; }

        public int XDKVersion { get; set; }

        public Partition[] Partitions { get; private set; }

        public uint RawTableOffset { get; set; }
        public uint RawTableSize { get; set; }

        public uint MetaOffsetMask { get; private set; }

        public uint AddressMask
        {
            get { return MetaOffsetMask - MetaOffset; }
        }

        public uint LocaleOffsetMask { get; set; }

        public Pointer LocaleDataLocation { get; set; }
        public int LocaleDataSize { get; set; }

        public int StringIDCount { get; set; }
        public int StringIDTableSize { get; set; }
        public Pointer StringIDIndexTableLocation { get; set; }
        public Pointer StringIDDataLocation { get; set; }

        public int FileNameCount { get; set; }
        public int FileNameTableSize { get; set; }
        public Pointer FileNameIndexTableLocation { get; set; }
        public Pointer FileNameDataLocation { get; set; }

        public uint Checksum { get; set; }

        public StructureValueCollection Serialize()
        {
            StructureValueCollection result = new StructureValueCollection();
            result.SetNumber("file size", FileSize);
            result.SetNumber("meta offset", MetaOffset);
            result.SetNumber("meta size", MetaSize);
            result.SetNumber("meta offset mask", MetaOffsetMask);
            result.SetNumber("type", (uint)Type);
            result.SetNumber("string table count", (uint)StringIDCount);
            result.SetNumber("string table size", (uint)StringIDTableSize);
            result.SetNumber("string index table offset", StringIDIndexTableLocation.AsOffset());
            result.SetNumber("string table offset", StringIDDataLocation.AsOffset());
            result.SetString("internal name", InternalName);
            result.SetString("scenario name", ScenarioName);
            result.SetNumber("file table count", (uint)FileNameCount);
            result.SetNumber("file table offset", FileNameDataLocation.AsOffset());
            result.SetNumber("file table size", (uint)FileNameTableSize);
            result.SetNumber("file index table offset", FileNameIndexTableLocation.AsOffset());
            result.SetNumber("raw table offset", RawTableOffset);
            result.SetNumber("raw table size", RawTableSize);
            result.SetNumber("checksum", Checksum);
            return result;
        }

        private void Load(StructureValueCollection values)
        {
            FileSize = values.GetNumber("file size");
            MetaOffset = values.GetNumber("meta offset");
            MetaSize = values.GetNumber("meta size");
            MetaOffsetMask = values.GetNumber("meta offset mask");

            Type = (CacheFileType)values.GetNumber("type");

            StringIDCount = (int)values.GetNumber("string table count");
            StringIDTableSize = (int)values.GetNumber("string table size");
            StringIDIndexTableLocation = new Pointer(values.GetNumber("string index table offset"), _identityConverter);
            StringIDDataLocation = new Pointer(values.GetNumber("string table offset"), _identityConverter);

            InternalName = values.GetString("internal name");
            ScenarioName = values.GetString("scenario name");

            FileNameCount = (int)values.GetNumber("file table count");
            FileNameDataLocation = new Pointer(values.GetNumber("file table offset"), _identityConverter);
            FileNameTableSize = (int)values.GetNumber("file table size");
            FileNameIndexTableLocation = new Pointer(values.GetNumber("file index table offset"), _identityConverter);

            RawTableOffset = values.GetNumber("raw table offset");
            RawTableSize = values.GetNumber("raw table size");

            Checksum = values.GetNumber("checksum");

            // Set up a bogus partition table
            Partitions = new Partition[1];
            Partitions[0] = new Partition(MetaBase, MetaSize);
        }
    }
}
