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
    public class ThirdGenHeader
    {
        private int _originalRawTableOffset;
        private FileSegment _eofSegment;

        public ThirdGenHeader(StructureValueCollection values, BuildInformation info, string buildString, FileSegmenter segmenter)
        {
            BuildString = buildString;
            HeaderSize = info.HeaderSize;
            Load(values, segmenter);
        }

        public int HeaderSize { get; private set; }

        public uint FileSize
        {
            get { return (uint)_eofSegment.Offset; }
        }

        public CacheFileType Type { get; private set; }
        public string InternalName { get; private set; }
        public string ScenarioName { get; private set; }
        public string BuildString { get; private set; }
        public int XDKVersion { get; set; }

        public FileSegmentGroup MetaArea { get; private set; }
        public SegmentPointer IndexHeaderLocation { get; set; }
        public Partition[] Partitions { get; private set; }

        public FileSegment RawTable { get; private set; }

        public IPointerConverter LocalePointerConverter { get; private set; }

        public FileSegmentGroup StringArea { get; private set; }

        public int StringIDCount { get; set; }
        public FileSegment StringIDIndexTable { get; private set; }
        public SegmentPointer StringIDIndexTableLocation { get; set; }
        public FileSegment StringIDData { get; private set; }
        public SegmentPointer StringIDDataLocation { get; set; }

        public FileSegment StringBlock { get; private set; }
        public SegmentPointer StringBlockLocation { get; set; }

        public int FileNameCount { get; set; }
        public FileSegment FileNameIndexTable { get; private set; }
        public SegmentPointer FileNameIndexTableLocation { get; set; }
        public FileSegment FileNameData { get; private set; }
        public SegmentPointer FileNameDataLocation { get; set; }

        public int UnknownCount { get; set; }
        public FileSegment UnknownTable { get; private set; }
        public SegmentPointer UnknownTableLocation { get; set; }
        
        public IList<FileSegment> Segments { get; private set; }

        /// <summary>
        /// Serializes the header's values, storing them into a StructureValueCollection.
        /// </summary>
        /// <param name="localeArea">The locale area of the cache file. Can be null.</param>
        /// <param name="localePointerMask">The value to add to locale pointers to translate them to file offsets.</param>
        /// <returns>The resulting StructureValueCollection.</returns>
        public StructureValueCollection Serialize(FileSegmentGroup localeArea)
        {
            StructureValueCollection values = new StructureValueCollection();

            if (_originalRawTableOffset != 0)
            {
                if (RawTable != null)
                    values.SetNumber("raw table offset", (uint)RawTable.Offset);

                if (localeArea != null)
                {
                    // I really don't know what these next two values are supposed to mean...anyone got anything?
                    values.SetNumber("eof index offset", localeArea.OffsetToPointer((int)FileSize)); // Reach, H4
                    values.SetNumber("eof index offset plus string data size", localeArea.OffsetToPointer((int)(FileSize + StringArea.Size))); // H3
                }
            }
            else
            {
                values.SetNumber("meta offset", (uint)MetaArea.Offset);
            }

            if (RawTable != null)
                values.SetNumber("raw table size", (uint)RawTable.Size);

            values.SetNumber("virtual base address", MetaArea.BasePointer);
            values.SetNumber("locale offset magic", (uint)-localeArea.PointerMask);
            values.SetNumber("file size", (uint)FileSize);
            values.SetNumber("index header address", IndexHeaderLocation.AsPointer());
            values.SetNumber("virtual size", (uint)MetaArea.Size);
            values.SetNumber("type", (uint)Type);
            if (StringBlockLocation != null)
                values.SetNumber("string block offset", (uint)StringBlockLocation.AsPointer());
            values.SetNumber("string table count", (uint)StringIDCount);
            values.SetNumber("string table size", (uint)StringIDData.Size);
            values.SetNumber("string index table offset", StringIDIndexTableLocation.AsPointer());
            values.SetNumber("string table offset", StringIDDataLocation.AsPointer());
            values.SetNumber("string data size", (uint)StringArea.Size);
            values.SetNumber("string offset magic", StringArea.BasePointer);
            values.SetString("internal name", InternalName);
            values.SetString("scenario name", ScenarioName);
            values.SetNumber("file table count", (uint)FileNameCount);
            values.SetNumber("file table offset", FileNameDataLocation.AsPointer());
            values.SetNumber("file table size", (uint)FileNameData.Size);
            values.SetNumber("file index table offset", FileNameIndexTableLocation.AsPointer());
            values.SetNumber("xdk version", (uint)XDKVersion);

            AdjustPartitions();
            values.SetArray("partitions", SerializePartitions());

            if (localeArea != null)
            {
                values.SetNumber("locale data index offset", localeArea.BasePointer);
                values.SetNumber("locale data size", (uint)localeArea.Size);
            }

            if (UnknownTableLocation != null)
            {
                values.SetNumber("unknown table count", (uint)UnknownCount);
                values.SetNumber("unknown table offset", UnknownTableLocation.AsPointer());
            }
            return values;
        }

        private void AdjustPartitions()
        {
            // Find the first partition with a non-null address and change it to the meta area's base address
            var partition = Partitions.First((p) => p.BasePointer != null);
            if (partition != null)
                partition.BasePointer = SegmentPointer.FromPointer(MetaArea.BasePointer, MetaArea);

            // Recalculate the size of each partition
            int partitionEnd = MetaArea.Offset + MetaArea.Size;
            for (int i = Partitions.Length - 1; i >= 0; i--)
            {
                if (Partitions[i].BasePointer == null)
                    continue;

                int offset = Partitions[i].BasePointer.AsOffset();
                Partitions[i].Size = (uint)(partitionEnd - offset);
                partitionEnd = offset;
            }
        }

        private StructureValueCollection[] SerializePartitions()
        {
            StructureValueCollection[] results = new StructureValueCollection[Partitions.Length];
            for (int i = 0; i < Partitions.Length; i++)
            {
                StructureValueCollection values = new StructureValueCollection();
                values.SetNumber("load address", Partitions[i].BasePointer != null ? Partitions[i].BasePointer.AsPointer() : 0);
                values.SetNumber("size", Partitions[i].Size);
                results[i] = values;
            }
            return results;
        }

        private void Load(StructureValueCollection values, FileSegmenter segmenter)
        {
            _eofSegment = segmenter.WrapEOF((int)values.GetNumber("file size"));

            Type = (CacheFileType)values.GetNumber("type");
            InternalName = values.GetString("internal name");
            ScenarioName = values.GetString("scenario name");
            XDKVersion = (int)values.GetNumber("xdk version");

            RawTable = CalculateRawTableSegment(values, segmenter);

            FileSegment metaSegment = CalculateMetaSegment(values, segmenter);
            uint virtualBase = values.GetNumber("virtual base address");
            MetaArea = new FileSegmentGroup(new MetaAddressConverter(metaSegment, virtualBase));
            MetaArea.AddSegment(metaSegment);

            IndexHeaderLocation = SegmentPointer.FromPointer(values.GetNumber("index header address"), MetaArea);
            Partitions = LoadPartitions(values.GetArray("partitions"));

            CalculateStringGroup(values, segmenter);
            LocalePointerConverter = CalculateLocalePointerConverter(values);

            Segments = new List<FileSegment>();
        }

        private FileSegment CalculateMetaSegment(StructureValueCollection values, FileSegmenter segmenter)
        {
            int metaOffset = CalculateMetaOffset(values);
            int metaSize = (int)values.GetNumber("virtual size");
            return segmenter.WrapSegment(metaOffset, metaSize, 0x10000, SegmentResizeOrigin.Beginning);
        }

        private int CalculateMetaOffset(StructureValueCollection values)
        {
            if (values.HasNumber("raw table offset") && values.HasNumber("raw table size"))
            {
                // Load raw table info
                int rawTableSize = (int)values.GetNumber("raw table size");
                int rawTableOffset = (int)values.GetNumber("raw table offset");

                // There are two ways to get the meta offset:
                // 1. Raw table offset + raw table size
                // 2. If raw table offset is zero, then the meta offset is directly stored in the header
                //    (The raw table offset can still be calculated in this case, but can't be used to find the meta the traditional way)
                if (rawTableOffset != 0)
                    return rawTableOffset + rawTableSize;
            }

            uint offset;
            if (!values.FindNumber("meta offset", out offset))
                throw new ArgumentException("The XML layout file is missing information on how to find the meta offset.");
            return (int)offset;
        }

        private FileSegment CalculateRawTableSegment(StructureValueCollection values, FileSegmenter segmenter)
        {
            // WAT. H3BETA DOESN'T HAVE THIS. WAT.
            if (values.HasNumber("raw table size") && values.HasNumber("raw table offset"))
            {
                // Load the basic values
                int rawTableSize = (int)values.GetNumber("raw table size");
                int rawTableOffset = (int)values.GetNumber("raw table offset");
                _originalRawTableOffset = rawTableOffset;

                // If the original raw table offset was 0, load it from the alternate pointer
                if (rawTableOffset == 0)
                    rawTableOffset = (int)values.GetNumber("alternate raw table offset");

                return segmenter.WrapSegment(rawTableOffset, rawTableSize, 0x1000, SegmentResizeOrigin.End);
            }
            else
            {
                return null;
            }
        }

        private IPointerConverter CalculateStringPointerConverter(StructureValueCollection values)
        {
            // If the original raw table offset isn't zero, then "string offset magic" contains the base pointer to the string area
            // and the string area is located immediately after the header
            // Otherwise, pointers are just file offsets
            uint magic = values.GetNumberOrDefault("string offset magic", 0);
            if (magic > 0 && values.GetNumberOrDefault("raw table offset", 0) > 0)
                return new BasedPointerConverter(magic, HeaderSize);
            return new IdentityPointerConverter();
        }

        private void CalculateStringGroup(StructureValueCollection values, FileSegmenter segmenter)
        {
            IPointerConverter converter = CalculateStringPointerConverter(values);

            // StringIDs
            int sidIndexTableOff = converter.PointerToOffset(values.GetNumber("string index table offset"));
            int sidDataOff = converter.PointerToOffset(values.GetNumber("string table offset"));

            StringIDCount = (int)values.GetNumber("string table count");
            int sidTableSize = (int)values.GetNumber("string table size");
            StringIDIndexTable = segmenter.WrapSegment(sidIndexTableOff, StringIDCount * 4, 4, SegmentResizeOrigin.End);
            StringIDData = segmenter.WrapSegment(sidDataOff, sidTableSize, 1, SegmentResizeOrigin.End);

            // idk what this is, but H3Beta has it
            if (values.HasNumber("string block offset"))
            {
                int sidBlockOff = converter.PointerToOffset(values.GetNumber("string block offset"));
                StringBlock = segmenter.WrapSegment(sidBlockOff, StringIDCount * 0x80, 0x80, SegmentResizeOrigin.End);
            }

            // Tag names
            int nameIndexTableOff = converter.PointerToOffset(values.GetNumber("file index table offset"));
            int nameDataOff = converter.PointerToOffset(values.GetNumber("file table offset"));

            FileNameCount = (int)values.GetNumber("file table count");
            int fileTableSize = (int)values.GetNumber("file table size");
            FileNameIndexTable = segmenter.WrapSegment(nameIndexTableOff, FileNameCount * 4, 4, SegmentResizeOrigin.End);
            FileNameData = segmenter.WrapSegment(nameDataOff, fileTableSize, 1, SegmentResizeOrigin.End);

            // Some H4-only unknown table
            if (values.HasNumber("unknown table count") && values.HasNumber("unknown table offset"))
            {
                int unknownOff = converter.PointerToOffset(values.GetNumber("unknown table offset"));

                UnknownCount = (int)values.GetNumber("unknown table count");
                UnknownTable = segmenter.WrapSegment(unknownOff, UnknownCount * 0x10, 0x10, SegmentResizeOrigin.End);
            }

            // Add the segments to the group
            StringArea = new FileSegmentGroup(converter);
            StringIDIndexTableLocation = StringArea.AddSegment(StringIDIndexTable);
            StringIDDataLocation = StringArea.AddSegment(StringIDData);
            FileNameIndexTableLocation = StringArea.AddSegment(FileNameIndexTable);
            FileNameDataLocation = StringArea.AddSegment(FileNameData);

            if (UnknownTable != null)
                UnknownTableLocation = StringArea.AddSegment(UnknownTable);
            if (StringBlock != null)
                StringBlockLocation = StringArea.AddSegment(StringBlock);
        }

        private IPointerConverter CalculateLocalePointerConverter(StructureValueCollection values)
        {
            uint mask = values.GetNumberOrDefault("locale offset magic", 0);
            if (mask != 0)
            {
                uint basePointer = values.GetNumber("locale data index offset");
                int baseOffset = (int)(basePointer + mask);
                return new BasedPointerConverter(basePointer, baseOffset);
            }
            return new IdentityPointerConverter(); // Locale pointers are file offsets
        }

        private Partition[] LoadPartitions(StructureValueCollection[] partitionValues)
        {
            var result = from partition in partitionValues
                         select new Partition
                         (
                             partition.GetNumber("load address") != 0 ? SegmentPointer.FromPointer(partition.GetNumber("load address"), MetaArea) : null,
                             partition.GetNumber("size")
                         );
            return result.ToArray();
        }
    }
}
