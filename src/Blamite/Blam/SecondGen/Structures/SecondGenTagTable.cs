using System;
using System.Collections.Generic;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.SecondGen.Structures
{
	public class SecondGenTagTable : TagTable
	{
		private List<ITagGroup> _groups;
		private Dictionary<int, ITagGroup> _groupsById;
		private List<ITag> _tags;

		public SecondGenTagTable(IReader reader, StructureValueCollection headerValues, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			Load(reader, headerValues, metaArea, buildInfo);
		}

		public IList<ITagGroup> Groups
		{
			get { return _groups; }
		}

		public override ITag this[int index]
		{
			get { return _tags[index]; }
		}

		public override int Count
		{
			get { return _tags.Count; }
		}

		public override IEnumerator<ITag> GetEnumerator()
		{
			return _tags.GetEnumerator();
		}

		public override ITag AddTag(int groupMagic, int baseSize, IStream stream)
		{
			throw new NotImplementedException("Adding tags is not supported for second-generation cache files");
		}

		private void Load(IReader reader, StructureValueCollection headerValues, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			if ((uint)headerValues.GetInteger("magic") != CharConstant.FromString("tags"))
				throw new ArgumentException("Invalid index table header magic");

			// Groups
			var numGroups = (int) headerValues.GetInteger("number of tag groups");
			var groupTableOffset = (uint) (metaArea.Offset + (uint)headerValues.GetInteger("tag group table offset"));
			// Offset is relative to the header
			_groups = ReadGroups(reader, groupTableOffset, numGroups, buildInfo);
			_groupsById = BuildGroupLookup(_groups);

			// Tags
			var numTags = (int) headerValues.GetInteger("number of tags");
			var tagTableOffset = (uint) (metaArea.Offset + (uint)headerValues.GetInteger("tag table offset"));
			// Offset is relative to the header
			_tags = ReadTags(reader, tagTableOffset, numTags, buildInfo, metaArea);
		}

		private static List<ITagGroup> ReadGroups(IReader reader, uint groupTableOffset, int numGroups,
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

		private List<ITag> ReadTags(IReader reader, uint tagTableOffset, int numTags, EngineDescription buildInfo,
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