using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.Serialization;
using Blamite.Blam.Util;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.SecondGen.Structures
{
	/// <summary>
	///     The tag + group table in a second-generation cache file.
	/// </summary>
	public class SecondGenTagTable : TagTable
	{
		private readonly MetaAllocator _allocator;
		private readonly EngineDescription _buildInfo;
		private readonly FileSegmentGroup _metaArea;

		private List<ITagGroup> _groups;
		private Dictionary<int, ITagGroup> _groupsById;
		private List<ITag> _tags;

		public SecondGenTagTable()
		{
			_tags = new List<ITag>();
			_groups = new List<ITagGroup>();
			_groupsById = new Dictionary<int, ITagGroup>();
		}

		public SecondGenTagTable(IReader reader, FileSegmentGroup metaArea, MetaAllocator allocator, EngineDescription buildInfo)
		{
			_metaArea = metaArea;
			_allocator = allocator;
			_buildInfo = buildInfo;

			Load(reader);
		}

		/// <summary>
		///     Gets a read-only list of available tag groups.
		/// </summary>
		/// <value>
		///     Available tag groups.
		/// </value>
		public IList<ITagGroup> Groups
		{
			get { return _groups; }
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
		/// <param name="groupMagic">The magic number (ID) of the tag's group.</param>
		/// <param name="baseSize">The size of the data to initially allocate for the tag.</param>
		/// <param name="stream">The stream to write to.</param>
		/// <returns>
		///     The tag that was allocated.
		/// </returns>
		public override ITag AddTag(int groupMagic, int baseSize, IStream stream)
		{
			throw new NotImplementedException("Adding tags is not supported for second-generation cache files");
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
			StructureLayout headerLayout = _buildInfo.Layouts.GetLayout("meta header");
			StructureValueCollection headerValues = LoadHeader(headerLayout, stream);
			if (headerValues == null)
				return;

			int groupStart = headerLayout.Size; //always follows header
			int groupSize = SaveGroups(headerValues, groupStart, stream);

			int tagStart = groupStart + groupSize; //always follows groups
			SaveTags(headerValues, tagStart, stream);

			SaveHeader(headerValues, headerLayout, stream);
		}

		private int SaveGroups(StructureValueCollection headerValues, int offset, IStream stream)
		{
			StructureLayout layout = _buildInfo.Layouts.GetLayout("tag group element");
			IEnumerable<StructureValueCollection> entries = _groups.Select(g => ((SecondGenTagGroup)g).Serialize());
			var addr = _metaArea.OffsetToPointer(_metaArea.Offset + offset);

			TagBlockWriter.WriteTagBlock(entries, addr, layout, _metaArea, stream);

			headerValues.SetInteger("number of tag groups", (uint)_groups.Count);
			headerValues.SetInteger("tag group table offset", (uint)offset);

			return Groups.Count * layout.Size;
		}

		private void SaveTags(StructureValueCollection headerValues, int offset, IStream stream)
		{
			StructureLayout layout = _buildInfo.Layouts.GetLayout("tag element");
			IEnumerable<StructureValueCollection> entries = _tags.Select(t => ((SecondGenTag)t).Serialize());
			var addr = _metaArea.OffsetToPointer(_metaArea.Offset + offset);

			TagBlockWriter.WriteTagBlock(entries, addr, layout, _metaArea, stream);

			headerValues.SetInteger("number of tags", (uint)_tags.Count);
			headerValues.SetInteger("tag table offset", (uint)offset);
		}

		private void Load(IReader reader)
		{
			StructureLayout headerLayout = _buildInfo.Layouts.GetLayout("meta header");
			StructureValueCollection headerValues = LoadHeader(headerLayout, reader);
			if (headerValues == null)
				return;

			// Groups
			var numGroups = (int) headerValues.GetInteger("number of tag groups");
			var groupTableOffset = (uint) (_metaArea.Offset + (uint)headerValues.GetInteger("tag group table offset"));
			// Offset is relative to the header
			_groups = LoadGroups(reader, groupTableOffset, numGroups, _buildInfo);
			_groupsById = BuildGroupLookup(_groups);

			// Tags
			var numTags = (int) headerValues.GetInteger("number of tags");
			var tagTableOffset = (uint) (_metaArea.Offset + (uint)headerValues.GetInteger("tag table offset"));
			// Offset is relative to the header
			_tags = LoadTags(reader, tagTableOffset, numTags, _buildInfo, _metaArea);
		}

		private StructureValueCollection LoadHeader(StructureLayout headerLayout, IReader reader)
		{
			if (_metaArea == null)
				return null;

			reader.SeekTo(_metaArea.Offset);
			StructureValueCollection result = StructureReader.ReadStructure(reader, headerLayout);
			if ((uint)result.GetInteger("magic") != CharConstant.FromString("tags"))
				throw new ArgumentException("Invalid index table header magic. This map could be compressed, try the Compressor in the Tools menu before reporting.");

			return result;
		}

		private void SaveHeader(StructureValueCollection headerValues, StructureLayout headerLayout, IWriter writer)
		{
			if (_metaArea != null)
			{
				writer.SeekTo(_metaArea.Offset);
				StructureWriter.WriteStructure(headerValues, headerLayout, writer);
			}
		}

		private static List<ITagGroup> LoadGroups(IReader reader, uint groupTableOffset, int numGroups,
			EngineDescription buildInfo)
		{
			StructureLayout layout = buildInfo.Layouts.GetLayout("tag group element");

			var result = new List<ITagGroup>();
			reader.SeekTo(groupTableOffset);
			for (int i = 0; i < numGroups; i++)
			{
				StructureValueCollection values = StructureReader.ReadStructure(reader, layout);
				result.Add(new SecondGenTagGroup(values));
			}
			return result;
		}

		private static Dictionary<int, ITagGroup> BuildGroupLookup(List<ITagGroup> groups)
		{
			var result = new Dictionary<int, ITagGroup>();
			foreach (ITagGroup tagGroup in groups)
				result[tagGroup.Magic] = tagGroup;
			return result;
		}

		private List<ITag> LoadTags(IReader reader, uint tagTableOffset, int numTags, EngineDescription buildInfo,
			FileSegmentGroup metaArea)
		{
			StructureLayout layout = buildInfo.Layouts.GetLayout("tag element");

			var result = new List<ITag>();
			reader.SeekTo(tagTableOffset);
			for (int i = 0; i < numTags; i++)
			{
				StructureValueCollection values = StructureReader.ReadStructure(reader, layout);
				result.Add(new SecondGenTag(values, metaArea, _groupsById));
			}
			return result;
		}
	}
}