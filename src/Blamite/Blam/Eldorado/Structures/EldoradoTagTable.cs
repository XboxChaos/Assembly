using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.Eldorado.Structures
{
	/// <summary>
	///     The eldorado tag file
	/// </summary>
	public class EldoradoTagTable : TagTable
	{
		private readonly EngineDescription _buildInfo;

		private List<ITag> _tags;
		private List<ITagInterop> _interops;
		private Dictionary<int, ITag> _globalTags;

		public EldoradoTagTable()
		{
			_tags = new List<ITag>();
			_interops = new List<ITagInterop>();
			Groups = new List<ITagGroup>();
			_globalTags = new Dictionary<int, ITag>();
		}

		public EldoradoTagTable(IReader reader, EngineDescription buildInfo, FileSegmenter tagSegmenter)
		{
			_buildInfo = buildInfo;

			Load(reader, tagSegmenter);
		}

		/// <summary>
		///     Gets a read-only list of available tag groups.
		/// </summary>
		/// <value>
		///     Available tag groups.
		/// </value>
		public IList<ITagGroup> Groups { get; private set; }

		public IList<ITagInterop> Interops
		{
			get { return _interops; }
		}

		public override ITag GetGlobalTag(int magic)
		{
			if (_globalTags.ContainsKey(magic))
				return _globalTags[magic];
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
		public override ITag AddTag(int groupMagic, uint baseSize, IStream stream)
		{
			throw new NotImplementedException();
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

			//tags would be here but thats a can o worms
			SaveHeader(headerValues, stream);
		}

		private void Load(IReader reader, FileSegmenter segmenter)
		{

			StructureValueCollection headerValues = LoadHeader(reader);
			if (headerValues == null)
				return;

			Groups = LoadGroups(reader, headerValues);
			_tags = LoadTags(reader, headerValues, segmenter);
		}

		private StructureValueCollection LoadHeader(IReader reader)
		{
			reader.SeekTo(0);
			StructureLayout headerLayout = _buildInfo.Layouts.GetLayout("tags header");
			StructureValueCollection result = StructureReader.ReadStructure(reader, headerLayout);

			return result;
		}

		private void SaveHeader(StructureValueCollection headerValues, IWriter writer)
		{
			writer.SeekTo(0);
			StructureLayout headerLayout = _buildInfo.Layouts.GetLayout("tags header");
			StructureWriter.WriteStructure(headerValues, headerLayout, writer);
		}

		private List<ITagGroup> LoadGroups(IReader reader, StructureValueCollection headerValues)
		{
			return new List<ITagGroup>();
		}

		private ITagGroup TryAddGroup(StructureValueCollection values)
		{
			uint magic = (uint)values.GetInteger("magic");

			for (int j = 0; j < Groups.Count(); j++)
			{
				if (Groups[j].Magic == magic) // Check of magic already exists
				{
					return Groups[j]; // Exists, return it.
				}
			}
			// Does not exist. Create it and return it.
			EldoradoTagGroup tagclass = new EldoradoTagGroup(values);
			Groups.Add(tagclass);
			return tagclass;
		}

		private List<ITag> LoadTags(IReader reader, StructureValueCollection headerValues, FileSegmenter segmenter)
		{
			List<ITag> tags = new List<ITag>(); // New list of tags

			var count = (int)headerValues.GetInteger("number of tags");
			uint address = (uint)headerValues.GetInteger("tag table offset");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("tag element header");

			// Find all of the tag offsets
			reader.BaseStream.Position = address; // Start at the beginning of the offsets
			List<uint> tagOffsets = new List<uint>();

			for (int i = 0; i < count; i++) tagOffsets.Add(reader.ReadUInt32());

			reader.BaseStream.Position = tagOffsets[0];
			for (int i = 0; i < count; i++) // Loop through each offset
			{
				if (tagOffsets[i] != 0)
				{
					var headerOffset = tagOffsets[i];
					reader.BaseStream.Position = headerOffset;

					StructureValueCollection tag_entry_values = StructureReader.ReadStructure(reader, layout);

					uint size = (uint)tag_entry_values.GetInteger("size");

					int structOffset = (int)tag_entry_values.GetInteger("base data offset");
					int metaOffset = (int)tagOffsets[i] + structOffset;

					ITagGroup group = TryAddGroup(tag_entry_values);

					FileSegment tagSegment = new FileSegment(segmenter.DefineSegment(headerOffset, size, 0x4, SegmentResizeOrigin.End), segmenter);
					FileSegmentGroup segGroup = new FileSegmentGroup(new MetaAddressConverter(tagSegment, 0x40000000));
					segGroup.AddSegment(tagSegment);

					SegmentPointer meta = SegmentPointer.FromPointer(0x40000000 + structOffset, segGroup);

					EldoradoTag tag = new EldoradoTag(new DatumIndex((uint)i), group, null, meta);
					tags.Add(tag);
				}
				else // Null Tag
				{
					EldoradoTag tag = new EldoradoTag(new DatumIndex((uint)i), null, null, null);
					tags.Add(tag);
				}
			}

			return tags;
		}
	}
}
