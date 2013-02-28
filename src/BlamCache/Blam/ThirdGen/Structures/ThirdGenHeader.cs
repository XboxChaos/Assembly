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

            values.SetInteger("file size", (uint)FileSize);
            values.SetInteger("type", (uint)Type);
            values.SetString("internal name", InternalName);
            values.SetString("scenario name", ScenarioName);
            values.SetInteger("xdk version", (uint)XDKVersion);

            AdjustPartitions();
            values.SetArray("partitions", SerializePartitions());

            if (_originalRawTableOffset != 0)
            {
                if (RawTable != null)
                    values.SetInteger("raw table offset", (uint)RawTable.Offset);

                if (localeArea != null)
                {
                    // I really don't know what these next two values are supposed to mean...anyone got anything?
                    values.SetInteger("eof index offset", localeArea.OffsetToPointer((int)FileSize)); // Reach, H4
                    values.SetInteger("eof index offset plus string data size", localeArea.OffsetToPointer((int)(FileSize + StringArea.Size))); // H3
                }
            }
            else
            {
                values.SetInteger("meta offset", (uint)MetaArea.Offset);
            }

            if (RawTable != null)
                values.SetInteger("raw table size", (uint)RawTable.Size);

            if (MetaArea != null)
            {
                values.SetInteger("virtual base address", MetaArea.BasePointer);
                values.SetInteger("index header address", IndexHeaderLocation.AsPointer());
                values.SetInteger("virtual size", (uint)MetaArea.Size);
            }

            if (StringBlockLocation != null)
                values.SetInteger("string block offset", (uint)StringBlockLocation.AsPointer());
            
            values.SetInteger("string table count", (uint)StringIDCount);
            if (StringIDData != null)
            {
                values.SetInteger("string table size", (uint)StringIDData.Size);
                values.SetInteger("string table offset", StringIDDataLocation.AsPointer());
            }

            if (StringIDIndexTableLocation != null)
                values.SetInteger("string index table offset", StringIDIndexTableLocation.AsPointer());

            if (StringArea != null)
            {
                values.SetInteger("string data size", (uint)StringArea.Size);
                values.SetInteger("string offset magic", StringArea.BasePointer);
            }

            values.SetInteger("file table count", (uint)FileNameCount);
            if (FileNameData != null)
            {
                values.SetInteger("file table offset", FileNameDataLocation.AsPointer());
                values.SetInteger("file table size", (uint)FileNameData.Size);
            }

            if (FileNameIndexTableLocation != null)
                values.SetInteger("file index table offset", FileNameIndexTableLocation.AsPointer());

            if (localeArea != null)
            {
                values.SetInteger("locale data index offset", localeArea.BasePointer);
                values.SetInteger("locale data size", (uint)localeArea.Size);
                values.SetInteger("locale offset magic", (uint)-localeArea.PointerMask);
            }

            if (UnknownTableLocation != null)
            {
                values.SetInteger("unknown table count", (uint)UnknownCount);
                values.SetInteger("unknown table offset", UnknownTableLocation.AsPointer());
            }
            return values;
        }

        private void AdjustPartitions()
        {
            if (MetaArea == null)
                return;

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
            if (Partitions == null)
                return new StructureValueCollection[0];

            StructureValueCollection[] results = new StructureValueCollection[Partitions.Length];
            for (int i = 0; i < Partitions.Length; i++)
            {
                StructureValueCollection values = new StructureValueCollection();
                values.SetInteger("load address", Partitions[i].BasePointer != null ? Partitions[i].BasePointer.AsPointer() : 0);
                values.SetInteger("size", Partitions[i].Size);
                results[i] = values;
            }
            return results;
        }

        private void Load(StructureValueCollection values, FileSegmenter segmenter)
        {
            _eofSegment = segmenter.WrapEOF((int)values.GetInteger("file size"));

            Type = (CacheFileType)values.GetInteger("type");
            InternalName = values.GetString("internal name");
            ScenarioName = values.GetString("scenario name");
            XDKVersion = (int)values.GetInteger("xdk version");

            RawTable = CalculateRawTableSegment(values, segmenter);

            FileSegment metaSegment = CalculateMetaSegment(values, segmenter);
            if (metaSegment != null)
            {
                uint virtualBase = values.GetInteger("virtual base address");
                MetaArea = new FileSegmentGroup(new MetaAddressConverter(metaSegment, virtualBase));
                MetaArea.AddSegment(metaSegment);

                IndexHeaderLocation = SegmentPointer.FromPointer(values.GetInteger("index header address"), MetaArea);
                Partitions = LoadPartitions(values.GetArray("partitions"));
            }
            else
            {
                Partitions = new Partition[0];
            }

            CalculateStringGroup(values, segmenter);
            LocalePointerConverter = CalculateLocalePointerConverter(values);

            Segments = new List<FileSegment>();
        }

        private FileSegment CalculateMetaSegment(StructureValueCollection values, FileSegmenter segmenter)
        {
            int metaSize = (int)values.GetInteger("virtual size");
            if (metaSize == 0)
                return null;

            int metaOffset = CalculateMetaOffset(values);
            if (metaOffset == 0)
                return null;

            return segmenter.WrapSegment(metaOffset, metaSize, 0x10000, SegmentResizeOrigin.Beginning);
        }

        private int CalculateMetaOffset(StructureValueCollection values)
        {
            if (values.HasInteger("raw table offset") && values.HasInteger("raw table size"))
            {
                // Load raw table info
                int rawTableSize = (int)values.GetInteger("raw table size");
                int rawTableOffset = (int)values.GetInteger("raw table offset");

                // There are two ways to get the meta offset:
                // 1. Raw table offset + raw table size
                // 2. If raw table offset is zero, then the meta offset is directly stored in the header
                //    (The raw table offset can still be calculated in this case, but can't be used to find the meta the traditional way)
                if (rawTableOffset != 0)
                    return rawTableOffset + rawTableSize;
            }

            uint offset;
            if (!values.FindInteger("meta offset", out offset))
                throw new ArgumentException("The XML layout file is missing information on how to find the meta offset.");
            return (int)offset;
        }

        private FileSegment CalculateRawTableSegment(StructureValueCollection values, FileSegmenter segmenter)
        {
            // WAT. H3BETA DOESN'T HAVE THIS. WAT.
            if (values.HasInteger("raw table size") && values.HasInteger("raw table offset"))
            {
                // Load the basic values
                int rawTableSize = (int)values.GetInteger("raw table size");
                int rawTableOffset = (int)values.GetInteger("raw table offset");
                _originalRawTableOffset = rawTableOffset;

                // If the original raw table offset was 0, load it from the alternate pointer
                if (rawTableOffset == 0)
                    rawTableOffset = (int)values.GetInteger("alternate raw table offset");

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
            uint magic = values.GetIntegerOrDefault("string offset magic", 0);
            if (magic > 0 && values.GetIntegerOrDefault("raw table offset", 0) > 0)
                return new BasedPointerConverter(magic, HeaderSize);
            return new IdentityPointerConverter();
        }

        private void CalculateStringGroup(StructureValueCollection values, FileSegmenter segmenter)
        {
            IPointerConverter converter = CalculateStringPointerConverter(values);
            StringArea = new FileSegmentGroup(converter);

            // StringIDs
            StringIDCount = (int)values.GetInteger("string table count");
            if (StringIDCount > 0)
            {
                int sidIndexTableOff = converter.PointerToOffset(values.GetInteger("string index table offset"));
                int sidDataOff = converter.PointerToOffset(values.GetInteger("string table offset"));

                int sidTableSize = (int)values.GetInteger("string table size");
                StringIDIndexTable = segmenter.WrapSegment(sidIndexTableOff, StringIDCount * 4, 4, SegmentResizeOrigin.End);
                StringIDData = segmenter.WrapSegment(sidDataOff, sidTableSize, 1, SegmentResizeOrigin.End);

                StringIDIndexTableLocation = StringArea.AddSegment(StringIDIndexTable);
                StringIDDataLocation = StringArea.AddSegment(StringIDData);

                // idk what this is, but H3Beta has it
                if (values.HasInteger("string block offset"))
                {
                    int sidBlockOff = converter.PointerToOffset(values.GetInteger("string block offset"));
                    StringBlock = segmenter.WrapSegment(sidBlockOff, StringIDCount * 0x80, 0x80, SegmentResizeOrigin.End);
                    StringBlockLocation = StringArea.AddSegment(StringBlock);
                }
            }

            // Tag names
            FileNameCount = (int)values.GetInteger("file table count");
            if (FileNameCount > 0)
            {
                int nameIndexTableOff = converter.PointerToOffset(values.GetInteger("file index table offset"));
                int nameDataOff = converter.PointerToOffset(values.GetInteger("file table offset"));

                int fileTableSize = (int)values.GetInteger("file table size");
                FileNameIndexTable = segmenter.WrapSegment(nameIndexTableOff, FileNameCount * 4, 4, SegmentResizeOrigin.End);
                FileNameData = segmenter.WrapSegment(nameDataOff, fileTableSize, 1, SegmentResizeOrigin.End);

                FileNameIndexTableLocation = StringArea.AddSegment(FileNameIndexTable);
                FileNameDataLocation = StringArea.AddSegment(FileNameData);
            }

            // Some H4-only unknown table
            if (values.HasInteger("unknown table count") && values.HasInteger("unknown table offset"))
            {
                UnknownCount = (int)values.GetInteger("unknown table count");
                if (UnknownCount > 0)
                {
                    int unknownOff = converter.PointerToOffset(values.GetInteger("unknown table offset"));
                    UnknownTable = segmenter.WrapSegment(unknownOff, UnknownCount * 0x10, 0x10, SegmentResizeOrigin.End);
                    UnknownTableLocation = StringArea.AddSegment(UnknownTable);
                }
            }
        }

        private IPointerConverter CalculateLocalePointerConverter(StructureValueCollection values)
        {
            uint mask = values.GetIntegerOrDefault("locale offset magic", 0);
            if (mask != 0)
            {
                uint basePointer = values.GetInteger("locale data index offset");
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
                             partition.GetInteger("load address") != 0 ? SegmentPointer.FromPointer(partition.GetInteger("load address"), MetaArea) : null,
                             partition.GetInteger("size")
                         );
            return result.ToArray();
        }
    }
}
