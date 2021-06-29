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

		public FirstGenTagTable(IReader reader, StructureValueCollection headerValues, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			Load(reader, headerValues, metaArea, buildInfo);
		}

		public IList<ITagGroup> Groups
		{
			get { return _groups; }
			//private set { _groups = value; }
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
			throw new NotImplementedException("Adding tags is not supported for first-generation cache files");
		}

		private void Load(IReader reader, StructureValueCollection headerValues, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			if ((uint)headerValues.GetInteger("magic") != CharConstant.FromString("tags"))
				throw new ArgumentException("Invalid index table header magic");

			// Tags
			var numTags = (int)headerValues.GetInteger("number of tags");

			// TODO (Dragon): idk if this is acceptable
			uint tagTableOffset;
			if (buildInfo.BuildVersion == "02.01.07.4998" || buildInfo.BuildVersion == "02.06.28.07902")
			{
				//tagTableOffset = (uint)(metaArea.Offset + (uint)headerValues.GetInteger("tag table offset") - metaArea.BasePointer);
				tagTableOffset = (uint)(metaArea.Offset + (uint)headerValues.GetInteger("tag table offset") - (uint)headerValues.GetInteger("meta header mask"));
			}
			else
			{
				tagTableOffset = (uint)(metaArea.Offset + (uint)headerValues.GetInteger("tag table offset") - metaArea.BasePointer);
			}

			// Offset is relative to the header
			// hack to "spoof" a group table since firstgen has none
			_groups = ReadGroups(reader, tagTableOffset, numTags, buildInfo);
			_groupsById = BuildGroupLookup(_groups);

			_tags = ReadTags(reader, tagTableOffset, numTags, buildInfo, metaArea);
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
			FileSegmentGroup metaArea)
		{
			StructureLayout layout = buildInfo.Layouts.GetLayout("tag element");

			var result = new List<ITag>();
			reader.SeekTo(tagTableOffset);

			// TODO (Dragon): DUDE
			long oldpos = reader.Position;
			reader.SeekTo(metaArea.Offset);
			uint metaOffset = reader.ReadUInt32();
			metaOffset -= (uint)(tagTableOffset - metaArea.Offset);
			reader.SeekTo(oldpos);

			for (int i = 0; i < numTags; i++)
			{
				StructureValueCollection values = StructureReader.ReadStructure(reader, layout);
				
				// TODO (Dragon): SERIOUSLY
				// JUST FUCKING FIX THE FILE SEGMENTS OH MY GOD
				if (buildInfo.BuildVersion == "02.01.07.4998" || buildInfo.BuildVersion == "02.06.28.07902")
				{
					ulong nameOffset = values.GetInteger("name offset");
					//nameOffset += ((ulong)metaArea.BasePointer - metaOffset) - (ulong)metaArea.BasePointer + (ulong)metaArea.Offset;
					nameOffset += (ulong)metaArea.BasePointer - metaOffset;
					values.SetInteger("name offset", nameOffset);
				}
				result.Add(new FirstGenTag(values, metaArea, _groupsById));
			}
			return result;
		}
	}
}
