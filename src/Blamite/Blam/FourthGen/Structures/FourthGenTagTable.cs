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
using System.Linq;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.FourthGen.Structures
{
	/// <summary>
	///     The tag + class table in a third-generation cache file.
	/// </summary>
	public class FourthGenTagTable : TagTable
	{
		private readonly MetaAllocator _allocator;
		private readonly EngineDescription _buildInfo;
		//private readonly SegmentPointer _indexHeaderLocation;
		//private readonly FileSegmentGroup _metaArea;

		private List<ITag> _tags;

		public FourthGenTagTable()
		{
			_tags = new List<ITag>();
			Classes = new List<ITagClass>();
		}

		public FourthGenTagTable(IReader reader, MetaAllocator allocator, EngineDescription buildInfo)
		{
			//_indexHeaderLocation = indexHeaderLocation;
			//_metaArea = metaArea;
			_allocator = allocator;
			_buildInfo = buildInfo;

			Load(reader);
		}

		/// <summary>
		///     Gets a read-only list of available tag classes.
		/// </summary>
		/// <value>
		///     Available tag classes.
		/// </value>
		public IList<ITagClass> Classes { get; private set; }

		/// <summary>
		///     Gets the tag at a given index.
		/// </summary>
		/// <param name="index">The index of the tag to retrieve.</param>
		/// <returns>The tag at the given index.</returns>
		public override ITag this[int index]
		{
			get { return _tags[index]; }
		}

		/// <summary>
		///     Gets the number of tags in the table.
		/// </summary>
		public override int Count
		{
			get { return _tags.Count; }
		}

		/// <summary>
		///     Adds a tag to the table and allocates space for its base data.
		/// </summary>
		/// <param name="classMagic">The magic number (ID) of the tag's class.</param>
		/// <param name="baseSize">The size of the data to initially allocate for the tag.</param>
		/// <param name="stream">The stream to write to.</param>
		/// <returns>
		///     The tag that was allocated.
		/// </returns>
		public override ITag AddTag(int classMagic, int baseSize, IStream stream)
		{
            /*
			//if (_indexHeaderLocation == null)
			//	throw new InvalidOperationException("Tags cannot be added to a shared map");

			ITagClass tagClass = Classes.FirstOrDefault(c => (c.Magic == classMagic));
			if (tagClass == null)
				throw new InvalidOperationException("Invalid tag class");

			uint address = _allocator.Allocate(baseSize, stream);
			var index = new DatumIndex(0x4153, (ushort) _tags.Count); // 0x4153 = 'AS' because the salt doesn't matter
            var result = new FourthGenTag(index, tagClass, (uint)stream.Position);
			_tags.Add(result);

			return result;
             * */

            ITagClass tagClass = Classes.FirstOrDefault(c => (c.Magic == classMagic));
            if (tagClass == null)
                throw new InvalidOperationException("Invalid tag class");

            var offset = stream.BaseStream.Position;
            uint address = _allocator.Allocate(baseSize, stream);
            var index = new DatumIndex(0x4153, (ushort)_tags.Count); // 0x4153 = 'AS' because the salt doesn't matter

            // File Segment
            FileSegmenter segmenter = new FileSegmenter();
            segmenter.DefineSegment(0, (int)stream.Length, 0x4, SegmentResizeOrigin.Beginning); // Define a segment for the header
            //_eofSegment = segmenter.WrapEOF((int)map_values.GetInteger("file size"));
            FileSegment segment = new FileSegment(0, segmenter);
            FileSegmentGroup segmentgroup = new FileSegmentGroup();

            SegmentPointer pointer = new SegmentPointer(segment, segmentgroup, (int)offset);
            FourthGenTag result = new FourthGenTag(index, tagClass, pointer);

            _tags.Add(result);

            return result;
		}

		/// <summary>
		///     Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
		/// </returns>
		public override IEnumerator<ITag> GetEnumerator()
		{
			return _tags.GetEnumerator();
		}

		/// <summary>
		///     Saves changes to the tag table.
		/// </summary>
		/// <param name="stream">The stream to write changes to.</param>
		public void SaveChanges(IStream stream)
		{
			StructureValueCollection headerValues = LoadHeader(stream);
			if (headerValues == null)
				return;

			//SaveTags(headerValues, stream);
			SaveHeader(headerValues, stream);
		}

		private void SaveTags(StructureValueCollection headerValues, IStream stream)
		{
			var oldCount = (int) headerValues.GetInteger("number of tags");
			uint oldAddress = headerValues.GetInteger("tag table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("tag entry");
			IEnumerable<StructureValueCollection> entries = _tags.Select(t => ((FourthGenTag) t).Serialize(Classes));
			// hax, _tags is a list of ITag objects so we have to upcast
            var _metaArea = new FileSegmentGroup(new BasedPointerConverter(0, 0));
			uint newAddress = ReflexiveWriter.WriteReflexive(entries, oldCount, oldAddress, _tags.Count, layout, _metaArea,
				_allocator, stream);

			headerValues.SetInteger("number of tags", (uint) _tags.Count);
			headerValues.SetInteger("tag table address", newAddress);
		}

		private void Load(IReader reader)
		{
            
			StructureValueCollection headerValues = LoadHeader(reader);
			if (headerValues == null)
				return;

            Classes = LoadClasses(reader, headerValues);
			_tags = LoadTags(reader, headerValues, Classes);
		}

		private StructureValueCollection LoadHeader(IReader reader)
		{
			//if (_indexHeaderLocation == null)
			//	return null;

            //reader.SeekTo(_indexHeaderLocation.AsOffset());
            reader.SeekTo(0);
            StructureLayout headerLayout = _buildInfo.Layouts.GetLayout("tags_header");
			StructureValueCollection result = StructureReader.ReadStructure(reader, headerLayout);

			//if (result.GetInteger("magic") != CharConstant.FromString("tags"))
			//	throw new ArgumentException("Invalid index table header magic");

			return result;
		}

		private void SaveHeader(StructureValueCollection headerValues, IWriter writer)
		{
			//if (_indexHeaderLocation != null)
			{
				//writer.SeekTo(_indexHeaderLocation.AsOffset());
                writer.SeekTo(0);
                StructureLayout headerLayout = _buildInfo.Layouts.GetLayout("tags_header");
				StructureWriter.WriteStructure(headerValues, headerLayout, writer);
			}
		}

		private List<ITagClass> LoadClasses(IReader reader, StructureValueCollection headerValues)
		{
			//var count = (int) headerValues.GetInteger("number of classes");
			//uint address = headerValues.GetInteger("class table address");
			//StructureLayout layout = _buildInfo.Layouts.GetLayout("class entry");
			//StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
			//return entries.Select<StructureValueCollection, ITagClass>(e => new FourthGenTagClass(e)).ToList();
            return new List<ITagClass>();
		}

        private ITagClass TryAddClass(StructureValueCollection values)
        {
            uint magic = values.GetInteger("magic");

            for (int j = 0; j < Classes.Count(); j++)
            {
                if (Classes[j].Magic == magic) // Check of magic already exists
                {
                    return Classes[j]; // Exists, return it.
                    break;
                }
            }
            // Does not exist. Create it and return it.
            FourthGenTagClass tagclass = new FourthGenTagClass(values);
            Classes.Add(tagclass);
            return tagclass;
        }

        private ITagClass TryAddClass(int magic)
        {
            for (int j = 0; j < Classes.Count(); j++)
            {
                if (Classes[j].Magic == magic) // Check of magic already exists
                {
                    return Classes[j]; // Exists, return it.
                    break;
                }
            }
            // Does not exist. Create it and return it.
            FourthGenTagClass tagclass = new FourthGenTagClass(magic);
            Classes.Add(tagclass);
            return tagclass;
        }

		private List<ITag> LoadTags(IReader reader, StructureValueCollection headerValues, IList<ITagClass> classes)
		{
            /*
            StructureLayout layout = _buildInfo.Layouts.GetLayout("tag entry");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
			return
				entries.Select<StructureValueCollection, ITag>((e, i) => new FourthGenTag(e, (ushort) i, _metaArea, classes))
					.ToList();
             */

            List<ITag> tags = new List<ITag>(); // New list of tags
            reader.SeekTo(0);
            var count = (int) headerValues.GetInteger("number of tags");
			uint address = headerValues.GetInteger("tag table address");
            StructureLayout layout = _buildInfo.Layouts.GetLayout("tag entry");

            // File Segment
            FileSegmenter segmenter = new FileSegmenter();
            segmenter.DefineSegment(0, (int)reader.Length, 0x4, SegmentResizeOrigin.Beginning); // Define a segment for the header
            //_eofSegment = segmenter.WrapEOF((int)map_values.GetInteger("file size"));
            FileSegment segment = new FileSegment(0, segmenter);
            FileSegmentGroup segmentgroup = new FileSegmentGroup();

            // Find all of the tag offsets
            reader.BaseStream.Position = address; // Start at the beginning og the offsets
            List<uint> tagOffsets = new List<uint>();
            for (int i = 0; i < count; i++) tagOffsets.Add(reader.ReadUInt32());


            for (int i = 0; i < count; i++) // Loop through each offset
            {
                //if (tagOffsets[i] == 0) tags.Add(null);
                //else
                if (tagOffsets[i] != 0)
                {
                    var headerOffset = (uint)reader.BaseStream.Position;
                    /*
                    var checksum = reader.ReadUInt32();                         // 0x00 uint32 checksum?
                    var totalSize = reader.ReadUInt32();                        // 0x04 uint32 total size
                    var numDependencies = reader.ReadInt16();                   // 0x08 int16  dependencies count
                    var numDataFixups = reader.ReadInt16();                     // 0x0A int16  data fixup count
                    var numResourceFixups = reader.ReadInt16();                 // 0x0C int16  resource fixup count
                    reader.BaseStream.Position += 2;                            // 0x0E int16  (padding)
                    var mainStructOffset = reader.ReadUInt32();                 // 0x10 uint32 main struct offset
                    var tagClass = reader.ReadInt32();                          // 0x14 int32  class
                    var parentClass = reader.ReadInt32();                       // 0x18 int32  parent class
                    var grandparentClass = reader.ReadInt32();                  // 0x1C int32  grandparent class
                    var classId = reader.ReadUInt32();                          // 0x20 uint32 class stringid
                    */

                    reader.BaseStream.Position = tagOffsets[i];
                    StructureValueCollection tag_entry_values = StructureReader.ReadStructure(reader, layout);

                    ITagClass tagclass = TryAddClass(tag_entry_values);
                    //FourthGenTag tag = new FourthGenTag(new DatumIndex(headerOffset), tagclass, headerOffset);

                    SegmentPointer pointer = new SegmentPointer(segment, segmentgroup, (int)tagOffsets[i]);
                    FourthGenTag tag = new FourthGenTag(new DatumIndex(headerOffset), tagclass, pointer);
                    tags.Add(tag);
                }
            }

            return tags.Where(t => t != null).ToList(); // Remove NULL Entries
		}
	}
}