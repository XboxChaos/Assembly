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
    public class ThirdGenHeader : ICacheFileInfo, IWritable
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

        public CacheFileType Type { get; private set; }

        public string BuildString { get; private set; }

        public string InternalName { get; private set; }
        public string ScenarioName { get; private set; }

        public Pointer MetaBase
        {
            get { return new Pointer(_virtualBase, _addrConverter); }
        }

        public uint MetaSize { get; private set; }

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

        public void WriteTo(IWriter writer)
        {
            throw new NotImplementedException();
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
                RawTableOffset = values.GetNumber("raw table offset");
                RawTableSize = values.GetNumber("raw table size");
                metaOffset = RawTableOffset + RawTableSize;
            }
            else if (!values.FindNumber("meta offset", out metaOffset))
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
            uint stringOffsetMagic = values.GetNumberOrDefault("string offset magic", (uint)HeaderSize);
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
