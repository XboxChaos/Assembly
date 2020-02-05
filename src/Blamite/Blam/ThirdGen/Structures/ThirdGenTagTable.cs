/* Copyright 2012 Aaron Dierking, TJ Tunnell, Jordan Mueller, Alex Reed
 * 
 * This file is part of Blamite.
 * 
 * Blamite is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Blamite is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Blamite.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.ThirdGen.Structures
{
	/// <summary>
	///     The tag + class table in a third-generation cache file.
	/// </summary>
	public class ThirdGenTagTable : TagTable
	{
		private readonly MetaAllocator _allocator;
		private readonly EngineDescription _buildInfo;
		private readonly SegmentPointer _indexHeaderLocation;
		private readonly FileSegmentGroup _metaArea;
		private readonly IPointerExpander _expander;

		private List<ITag> _tags;
		private List<IPolyart> _polyart;
		private Dictionary<int, ITag> _importantTags;

		public ThirdGenTagTable()
		{
			_tags = new List<ITag>();
			_polyart = new List<IPolyart>();
			Classes = new List<ITagClass>();
			_importantTags = new Dictionary<int, ITag>();
		}

		public ThirdGenTagTable(IReader reader, SegmentPointer indexHeaderLocation, FileSegmentGroup metaArea,
			MetaAllocator allocator, EngineDescription buildInfo, IPointerExpander expander)
		{
			_indexHeaderLocation = indexHeaderLocation;
			_metaArea = metaArea;
			_allocator = allocator;
			_buildInfo = buildInfo;
			_expander = expander;

			Load(reader);
		}

		/// <summary>
		///     Gets a read-only list of available tag classes.
		/// </summary>
		/// <value>
		///     Available tag classes.
		/// </value>
		public IList<ITagClass> Classes { get; private set; }

		public IList<IPolyart> Polyart
		{
			get { return _polyart; }
		}

		public ITag GetImportantTag(int magic)
		{
			if (_importantTags.ContainsKey(magic))
				return _importantTags[magic];
			else
				return null;
		}

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
			if (_indexHeaderLocation == null)
				throw new InvalidOperationException("Tags cannot be added to a shared map");

			ITagClass tagClass = Classes.FirstOrDefault(c => (c.Magic == classMagic));
			if (tagClass == null)
				throw new InvalidOperationException("Invalid tag class");

			long address = _allocator.Allocate(baseSize, stream);
			var index = new DatumIndex(0x4153, (ushort) _tags.Count); // 0x4153 = 'AS' because the salt doesn't matter
			var result = new ThirdGenTag(index, tagClass, SegmentPointer.FromPointer(address, _metaArea));
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

			SaveTags(headerValues, stream);

			if (Polyart != null && Polyart.Count > 0)
			{
				var oldCount = (int)headerValues.GetInteger("number of polyart fixups");
				long oldAddress = (long)headerValues.GetInteger("polyart fixup table address");
				StructureLayout layout = _buildInfo.Layouts.GetLayout("polyart table entry");
				IEnumerable<StructureValueCollection> entries = _polyart.Select(t => ((ThirdGenPolyart)t).Serialize());
				// hax
				long newAddress = ReflexiveWriter.WriteReflexive(entries, oldCount, oldAddress, _polyart.Count, layout, _metaArea,
					_allocator, stream);

				headerValues.SetInteger("number of polyart fixups", (uint)_polyart.Count);
				headerValues.SetInteger("polyart fixup table address", (ulong)newAddress);
			}

			SaveHeader(headerValues, stream);
		}

		private void SaveTags(StructureValueCollection headerValues, IStream stream)
		{
			var oldCount = (int) headerValues.GetInteger("number of tags");
			long oldAddress = (long)headerValues.GetInteger("tag table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("tag entry");
			IEnumerable<StructureValueCollection> entries = _tags.Select(t => ((ThirdGenTag) t).Serialize(Classes, _expander));
			// hax, _tags is a list of ITag objects so we have to upcast
			long newAddress = ReflexiveWriter.WriteReflexive(entries, oldCount, oldAddress, _tags.Count, layout, _metaArea,
				_allocator, stream);

			headerValues.SetInteger("number of tags", (uint) _tags.Count);
			headerValues.SetInteger("tag table address", (ulong)newAddress);
		}

		private void Load(IReader reader)
		{
			StructureValueCollection headerValues = LoadHeader(reader);
			if (headerValues == null)
				return;

			Classes = LoadClasses(reader, headerValues).AsReadOnly();
			_tags = LoadTags(reader, headerValues, Classes);
			_importantTags = LoadImportant(reader, headerValues, _tags);
			_polyart = LoadPolyart(reader, headerValues);
		}

		private StructureValueCollection LoadHeader(IReader reader)
		{
			if (_indexHeaderLocation == null)
				return null;

			reader.SeekTo(_indexHeaderLocation.AsOffset());
			StructureLayout headerLayout = _buildInfo.Layouts.GetLayout("index header");
			StructureValueCollection result = StructureReader.ReadStructure(reader, headerLayout);
			if ((uint)result.GetInteger("magic") != CharConstant.FromString("tags"))
				throw new ArgumentException("Invalid index table header magic");

			return result;
		}

		private void SaveHeader(StructureValueCollection headerValues, IWriter writer)
		{
			if (_indexHeaderLocation != null)
			{
				writer.SeekTo(_indexHeaderLocation.AsOffset());
				StructureLayout headerLayout = _buildInfo.Layouts.GetLayout("index header");
				StructureWriter.WriteStructure(headerValues, headerLayout, writer);
			}
		}

		private List<ITagClass> LoadClasses(IReader reader, StructureValueCollection headerValues)
		{
			var count = (int) headerValues.GetInteger("number of classes");
			long address = (long)headerValues.GetInteger("class table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("class entry");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
			return entries.Select<StructureValueCollection, ITagClass>(e => new ThirdGenTagClass(e)).ToList();
		}

		private List<IPolyart> LoadPolyart(IReader reader, StructureValueCollection headerValues)
		{
			if (!headerValues.HasInteger("number of polyart fixups"))
				return null;

			var count = (int)headerValues.GetInteger("number of polyart fixups");
			long address = (long)headerValues.GetInteger("polyart fixup table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("polyart table entry");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
			return entries.Select<StructureValueCollection, IPolyart>(e => new ThirdGenPolyart(e, _metaArea)).ToList();
		}

		private List<ITag> LoadTags(IReader reader, StructureValueCollection headerValues, IList<ITagClass> classes)
		{
			var count = (int) headerValues.GetInteger("number of tags");
			long address = (long)headerValues.GetInteger("tag table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("tag entry");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
			return
				entries.Select<StructureValueCollection, ITag>((e, i) => new ThirdGenTag(e, (ushort) i, _metaArea, classes, _expander))
					.ToList();
		}

		private Dictionary<int, ITag> LoadImportant(IReader reader, StructureValueCollection headerValues, List<ITag> tags)
		{
			var count = (int)headerValues.GetInteger("number of important tags");
			long address = (long)headerValues.GetInteger("important tag table address");

			StructureLayout layout = _buildInfo.Layouts.GetLayout("important tag entry");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);

			Dictionary<int, ITag> output = new Dictionary<int, ITag>();
			foreach (StructureValueCollection ent in entries)
				output[(int)ent.GetInteger("class magic")] = tags[(int)ent.GetInteger("datum index") & 0xFFFF];

			return output;
		}
	}
}