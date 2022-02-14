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
	///     The tag + group table in a third-generation cache file.
	/// </summary>
	public class ThirdGenTagTable : TagTable
	{
		private readonly MetaAllocator _allocator;
		private readonly EngineDescription _buildInfo;
		private readonly SegmentPointer _indexHeaderLocation;
		private readonly FileSegmentGroup _metaArea;
		private readonly IPointerExpander _expander;

		private List<ITag> _tags;
		private List<ITagInterop> _interops;
		private Dictionary<int, ITag> _globalTags;

		public ThirdGenTagTable()
		{
			_tags = new List<ITag>();
			_interops = new List<ITagInterop>();
			Groups = new List<ITagGroup>();
			_globalTags = new Dictionary<int, ITag>();
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
		/// <param name="groupMagic">The magic number (ID) of the tag's group.</param>
		/// <param name="baseSize">The size of the data to initially allocate for the tag.</param>
		/// <param name="stream">The stream to write to.</param>
		/// <returns>
		///     The tag that was allocated.
		/// </returns>
		public override ITag AddTag(int groupMagic, int baseSize, IStream stream)
		{
			if (_indexHeaderLocation == null)
				throw new InvalidOperationException("Tags cannot be added to a shared map");

			ITagGroup tagGroup = Groups.FirstOrDefault(g => (g.Magic == groupMagic));
			if (tagGroup == null)
				throw new InvalidOperationException("Invalid tag group");

			long address = _allocator.Allocate(baseSize, stream);
			var index = new DatumIndex(0x4153, (ushort) _tags.Count); // 0x4153 = 'AS' because the salt doesn't matter
			var result = new ThirdGenTag(index, tagGroup, SegmentPointer.FromPointer(address, _metaArea));
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

			if (Interops != null && Interops.Count > 0)
			{
				var oldCount = (int)headerValues.GetInteger("number of tag interops");
				long oldAddress = (long)headerValues.GetInteger("tag interop table address");
				StructureLayout layout = _buildInfo.Layouts.GetLayout("tag interop element");
				IEnumerable<StructureValueCollection> entries = _interops.OrderBy(i=>i.Pointer).Select(t => ((ThirdGenTagInterop)t).Serialize());
				// hax
				long newAddress = TagBlockWriter.WriteTagBlock(entries, oldCount, oldAddress, _interops.Count, layout, _metaArea,
					_allocator, stream);

				headerValues.SetInteger("number of tag interops", (uint)_interops.Count);
				headerValues.SetInteger("tag interop table address", (ulong)newAddress);
			}

			SaveHeader(headerValues, stream);
		}

		private void SaveTags(StructureValueCollection headerValues, IStream stream)
		{
			var oldCount = (int) headerValues.GetInteger("number of tags");
			long oldAddress = (long)headerValues.GetInteger("tag table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("tag element");
			IEnumerable<StructureValueCollection> entries = _tags.Select(t => ((ThirdGenTag) t).Serialize(Groups, _expander));
			// hax, _tags is a list of ITag objects so we have to upcast
			long newAddress = TagBlockWriter.WriteTagBlock(entries, oldCount, oldAddress, _tags.Count, layout, _metaArea,
				_allocator, stream);

			headerValues.SetInteger("number of tags", (uint) _tags.Count);
			headerValues.SetInteger("tag table address", (ulong)newAddress);
		}

		private void Load(IReader reader)
		{
			StructureValueCollection headerValues = LoadHeader(reader);
			if (headerValues == null)
				return;

			Groups = LoadTagGroups(reader, headerValues).AsReadOnly();
			_tags = LoadTags(reader, headerValues, Groups);
			_globalTags = LoadGlobalTags(reader, headerValues, _tags);
			_interops = LoadTagInterops(reader, headerValues);
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

		private List<ITagGroup> LoadTagGroups(IReader reader, StructureValueCollection headerValues)
		{
			var count = (int) headerValues.GetInteger("number of tag groups");
			long address = (long)headerValues.GetInteger("tag group table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("tag group element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, address, layout, _metaArea);
			return entries.Select<StructureValueCollection, ITagGroup>(e => new ThirdGenTagGroup(e)).ToList();
		}

		private List<ITagInterop> LoadTagInterops(IReader reader, StructureValueCollection headerValues)
		{
			if (!headerValues.HasInteger("number of tag interops"))
				return null;

			var count = (int)headerValues.GetInteger("number of tag interops");
			long address = (long)headerValues.GetInteger("tag interop table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("tag interop element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, address, layout, _metaArea);
			return entries.Select<StructureValueCollection, ITagInterop>(e => new ThirdGenTagInterop(e, _metaArea)).ToList();
		}

		private List<ITag> LoadTags(IReader reader, StructureValueCollection headerValues, IList<ITagGroup> groups)
		{
			var count = (int) headerValues.GetInteger("number of tags");
			long address = (long)headerValues.GetInteger("tag table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("tag element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, address, layout, _metaArea);
			return
				entries.Select<StructureValueCollection, ITag>((e, i) => new ThirdGenTag(e, (ushort) i, _metaArea, groups, _expander))
					.ToList();
		}

		private Dictionary<int, ITag> LoadGlobalTags(IReader reader, StructureValueCollection headerValues, List<ITag> tags)
		{
			var count = (int)headerValues.GetInteger("number of global tags");
			long address = (long)headerValues.GetInteger("global tag table address");

			StructureLayout layout = _buildInfo.Layouts.GetLayout("global tag element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, address, layout, _metaArea);

			Dictionary<int, ITag> output = new Dictionary<int, ITag>();
			foreach (StructureValueCollection ent in entries)
				output[(int)ent.GetInteger("tag group magic")] = tags[(int)ent.GetInteger("datum index") & 0xFFFF];

			return output;
		}
	}
}