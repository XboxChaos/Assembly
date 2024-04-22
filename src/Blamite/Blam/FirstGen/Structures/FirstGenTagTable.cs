using Blamite.IO;
using Blamite.Serialization;
using Blamite.Util;
using System;
using System.Collections.Generic;

namespace Blamite.Blam.FirstGen.Structures
{
	public class FirstGenTagTable : TagTable
	{
		private List<ITagGroup> _groups;
		private Dictionary<int, ITagGroup> _groupsById;
		private List<ITag> _tags;
		private Dictionary<int, ITag> _globalTags;

		public FirstGenTagTable(IReader reader, StructureValueCollection headerValues, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			Load(reader, headerValues, metaArea, buildInfo);
		}

		public override ITag GetGlobalTag(int magic)
		{
			if (_globalTags.ContainsKey(magic))
				return _globalTags[magic];
			else
				return null;
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

		public override ITag AddTag(int groupMagic, uint baseSize, IStream stream)
		{
			throw new NotImplementedException("Adding tags is not supported for first-generation cache files");
		}

		private void Load(IReader reader, StructureValueCollection headerValues, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			if ((uint)headerValues.GetInteger("magic") != CharConstant.FromString("tags"))
				throw new ArgumentException("Invalid index table header magic. This map could be compressed, try the Compressor in the Tools menu before reporting.");

			// Tags
			var numTags = (int)headerValues.GetInteger("number of tags");

			uint tagTableOffset;
			if (headerValues.HasInteger("meta header mask"))
				tagTableOffset = (uint)(metaArea.Offset + (uint)headerValues.GetInteger("tag table address") - (uint)headerValues.GetInteger("meta header mask"));
			else
				tagTableOffset = (uint)(metaArea.Offset + (uint)headerValues.GetInteger("tag table address") - metaArea.BasePointer);

			// Offset is relative to the header
			_groups = ReadGroups(reader, tagTableOffset, numTags, buildInfo);
			_groupsById = BuildGroupLookup(_groups);

			_tags = ReadTags(reader, tagTableOffset, numTags, buildInfo, metaArea, headerValues);

			_globalTags = new Dictionary<int, ITag>();
			//store global tag
			_globalTags[CharConstant.FromString("scnr")] = _tags[(int)headerValues.GetInteger("scenario datum index") & 0xFFFF];
		}

		private static List<ITagGroup> ReadGroups(IReader reader, uint groupTableOffset, int numTags, EngineDescription buildInfo)
		{
			// group table hack since firstgen doesnt have an explicit class table
			StructureLayout layout = buildInfo.Layouts.GetLayout("tag element");

			var result = new List<ITagGroup>();
			reader.SeekTo(groupTableOffset);
			for (int i = 0; i < numTags; i++)
			{
				StructureValueCollection values = StructureReader.ReadStructure(reader, layout);
				ITagGroup groupRes = new FirstGenTagGroup(values);
				if (!result.Exists(x => x.Magic == groupRes.Magic))
				{
					result.Add(groupRes);
				}
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
			FileSegmentGroup metaArea, StructureValueCollection headerValues)
		{
			StructureLayout layout = buildInfo.Layouts.GetLayout("tag element");

			var result = new List<ITag>();
			reader.SeekTo(tagTableOffset);

			ulong metaOffset = 0;
			if (headerValues.HasInteger("meta header mask"))
				metaOffset = (ulong)metaArea.BasePointer - headerValues.GetInteger("meta header mask");

			for (int i = 0; i < numTags; i++)
			{
				StructureValueCollection values = StructureReader.ReadStructure(reader, layout);

				//h2 alpha/beta store names differently, convert it to something expected
				if (metaOffset > 0)
				{
					ulong nameOffset = values.GetInteger("name address");
					nameOffset += metaOffset;
					values.SetInteger("name address", nameOffset);
				}
				result.Add(new FirstGenTag(values, metaArea, _groupsById));
			}
			return result;
		}
	}
}
