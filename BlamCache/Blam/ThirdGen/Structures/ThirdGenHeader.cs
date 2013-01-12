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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ExtryzeDLL.Blam;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.IO;
using ExtryzeDLL.Flexibility;

namespace ExtryzeDLL.Blam.ThirdGen.Structures
{
    /// <summary>
    /// A cache file header whose layout can be changed.
    /// </summary>
    public class ThirdGenHeader : ICacheFileInfo
    {
        private uint _virtualBase;

        private MetaAddressConverter _addrConverter;
        private IndexOffsetConverter _indexConverter;
        private HeaderOffsetConverter _stringOffsetConverter;

        public ThirdGenHeader(StructureValueCollection values, BuildInformation info, string buildString)
        {
            BuildString = buildString;
            HeaderSize = info.HeaderSize;
            Load(values);
        }

        public int HeaderSize { get; private set; }

        public uint FileSize { get; set; }

        public CacheFileType Type { get; set; }

        public string BuildString { get; private set; }

        public string InternalName { get; private set; }
        public string ScenarioName { get; private set; }

        public Pointer MetaBase
        {
            get { return new Pointer(_virtualBase, _addrConverter); }
        }

        public uint MetaSize { get; set; }

        public int XDKVersion { get; set; }

        public Partition[] Partitions { get; private set; }

        public uint RawTableOffset { get; private set; }
        public uint RawTableSize { get; private set; }

        public Pointer IndexHeaderLocation { get; set; }
        public uint LocaleOffsetMask { get; set; }

        public uint AddressMask
        {
            get { return _addrConverter.AddressMask; }
        }

        public int StringIDCount { get; private set; }
        public int StringIDTableSize { get; private set; }
        public Pointer StringIDIndexTableLocation { get; private set; }
        public Pointer StringIDDataLocation { get; private set; }

        public int FileNameCount { get; private set; }
        public int FileNameTableSize { get; private set; }
        public Pointer FileNameIndexTableLocation { get; private set; }
        public Pointer FileNameDataLocation { get; private set; }

        public MetaAddressConverter MetaPointerConverter
        {
            get { return _addrConverter; }
        }

        public IndexOffsetConverter LocalePointerConverter
        {
            get { return _indexConverter; }
        }

        /// <summary>
        /// Serializes the header's values, storing them into a StructureValueCollection.
        /// </summary>
        /// <returns>The resulting StructureValueCollection.</returns>
        public StructureValueCollection Serialize()
        {
            StructureValueCollection values = new StructureValueCollection();
            values.SetNumber("virtual base address", _virtualBase);
            values.SetNumber("raw table offset", RawTableOffset);
            values.SetNumber("raw table offset from header", (uint)(RawTableOffset - HeaderSize));
            values.SetNumber("raw table size", RawTableSize);
            values.SetNumber("meta offset", MetaBase.AsOffset());
            values.SetNumber("locale offset magic", LocaleOffsetMask);
            values.SetNumber("file size", FileSize);
            values.SetNumber("index header address", IndexHeaderLocation.AsAddress());
            values.SetNumber("virtual size", MetaSize);
            values.SetNumber("type", (uint)Type);
            values.SetNumber("string table count", (uint)StringIDCount);
            values.SetNumber("string table size", (uint)StringIDTableSize);
            values.SetNumber("string index table offset", _stringOffsetConverter.PointerToRaw(StringIDIndexTableLocation));
            values.SetNumber("string table offset", _stringOffsetConverter.PointerToRaw(StringIDDataLocation));
            values.SetString("internal name", InternalName);
            values.SetString("scenario name", ScenarioName);
            values.SetNumber("file table count", (uint)FileNameCount);
            values.SetNumber("file table offset", _stringOffsetConverter.PointerToRaw(FileNameDataLocation));
            values.SetNumber("file table size", (uint)FileNameTableSize);
            values.SetNumber("file index table offset", _stringOffsetConverter.PointerToRaw(FileNameIndexTableLocation));
            values.SetNumber("xdk version", (uint)XDKVersion);
            values.SetArray("partitions", SerializePartitions());

            return values;
        }

        private StructureValueCollection[] SerializePartitions()
        {
            StructureValueCollection[] results = new StructureValueCollection[Partitions.Length];
            for (int i = 0; i < Partitions.Length; i++)
            {
                StructureValueCollection values = new StructureValueCollection();
                values.SetNumber("load address", Partitions[i].BasePointer.AsAddress());
                values.SetNumber("size", Partitions[i].Size);
                results[i] = values;
            }
            return results;
        }

        private void Load(StructureValueCollection values)
        {
            _addrConverter = LoadAddressConverter(values);
            _indexConverter = LoadIndexOffsetConverter(values);
            _stringOffsetConverter = LoadHeaderOffsetConverter(values);

            FileSize = values.GetNumber("file size");
            IndexHeaderLocation = new Pointer(values.GetNumber("index header address"), _addrConverter);
            MetaSize = values.GetNumber("virtual size");
            Type = (CacheFileType)values.GetNumber("type");

            StringIDCount = (int)values.GetNumber("string table count");
            StringIDTableSize = (int)values.GetNumber("string table size");
            StringIDIndexTableLocation = new Pointer(values.GetNumber("string index table offset"), _stringOffsetConverter);
            StringIDDataLocation = new Pointer(values.GetNumber("string table offset"), _stringOffsetConverter);

            InternalName = values.GetString("internal name");
            ScenarioName = values.GetString("scenario name");

            FileNameCount = (int)values.GetNumber("file table count");
            FileNameDataLocation = new Pointer(values.GetNumber("file table offset"), _stringOffsetConverter);
            FileNameTableSize = (int)values.GetNumber("file table size");
            FileNameIndexTableLocation = new Pointer(values.GetNumber("file index table offset"), _stringOffsetConverter);

            XDKVersion = (int)values.GetNumber("xdk version");
            Partitions = LoadPartitions(values.GetArray("partitions"));
        }

        private MetaAddressConverter LoadAddressConverter(StructureValueCollection values)
        {
            uint metaOffset = 0;
            _virtualBase = values.GetNumber("virtual base address");

            if (values.HasNumber("raw table offset") && values.HasNumber("raw table size"))
            {
                // Load raw table info
                RawTableSize = values.GetNumber("raw table size");
                RawTableOffset = values.GetNumber("raw table offset");

                // There are two ways to get the meta offset:
                // 1. Raw table offset + raw table size
                // 2. If raw table offset is zero, then the meta offset is directly stored in the header
                //    (The raw table offset can still be calculated in this case, but can't be used to find the meta the traditional way)
                if (RawTableOffset != 0)
                    metaOffset = RawTableOffset + RawTableSize;
                else
                    RawTableOffset = values.GetNumber("raw table offset from header") + (uint)HeaderSize;
            }
            if (metaOffset == 0 && !values.FindNumber("meta offset", out metaOffset))
            {
                throw new ArgumentException("The XML layout file is missing information on how to calculate map magic.");
            }

            return new MetaAddressConverter(_virtualBase, metaOffset);
        }

        private IndexOffsetConverter LoadIndexOffsetConverter(StructureValueCollection values)
        {
            LocaleOffsetMask = values.GetNumberOrDefault("locale offset magic", 0);
            return new IndexOffsetConverter(this);
        }

        private HeaderOffsetConverter LoadHeaderOffsetConverter(StructureValueCollection values)
        {
            // Only apply a modifier if the original raw table offset wasn't zero
            uint stringOffsetMagic = (uint)HeaderSize;
            if (values.HasNumber("raw table offset") && values.GetNumber("raw table offset") > 0)
                stringOffsetMagic = values.GetNumberOrDefault("string offset magic", (uint)HeaderSize);

            return new HeaderOffsetConverter(stringOffsetMagic, HeaderSize);
        }

        private Partition[] LoadPartitions(StructureValueCollection[] partitionValues)
        {
            var result = from partition in partitionValues
                         select new Partition
                         (
                             new Pointer(partition.GetNumber("load address"), _addrConverter),
                             partition.GetNumber("size")
                         );
            return result.ToArray();
        }
    }
}
