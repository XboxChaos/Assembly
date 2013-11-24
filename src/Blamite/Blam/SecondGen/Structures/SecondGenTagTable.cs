using System;
using System.Collections.Generic;
using Blamite.Flexibility;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.SecondGen.Structures
{
	public class SecondGenTagTable : TagTable
	{
		private List<ITagClass> _classes;
		private Dictionary<int, ITagClass> _classesById;
		private List<ITag> _tags;

		public SecondGenTagTable(IReader reader, StructureValueCollection headerValues, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			Load(reader, headerValues, metaArea, buildInfo);
		}

		public IList<ITagClass> Classes
		{
			get { return _classes; }
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

		public override ITag AddTag(int classMagic, int baseSize, IStream stream)
		{
			throw new NotImplementedException("Adding tags is not supported for second-generation cache files");
		}

		private void Load(IReader reader, StructureValueCollection headerValues, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			if (headerValues.GetInteger("magic") != CharConstant.FromString("tags"))
				throw new ArgumentException("Invalid index table header magic");

			// Classes
			var numClasses = (int) headerValues.GetInteger("number of classes");
			var classTableOffset = (uint) (metaArea.Offset + headerValues.GetInteger("class table offset"));
			// Offset is relative to the header
			_classes = ReadClasses(reader, classTableOffset, numClasses, buildInfo);
			_classesById = BuildClassLookup(_classes);

			// Tags
			var numTags = (int) headerValues.GetInteger("number of tags");
			var tagTableOffset = (uint) (metaArea.Offset + headerValues.GetInteger("tag table offset"));
			// Offset is relative to the header
			_tags = ReadTags(reader, tagTableOffset, numTags, buildInfo, metaArea);
		}

		private static List<ITagClass> ReadClasses(IReader reader, uint classTableOffset, int numClasses,
			EngineDescription buildInfo)
		{
			StructureLayout layout = buildInfo.Layouts.GetLayout("class entry");

			var result = new List<ITagClass>();
			reader.SeekTo(classTableOffset);
			for (int i = 0; i < numClasses; i++)
			{
				StructureValueCollection values = StructureReader.ReadStructure(reader, layout);
				result.Add(new SecondGenTagClass(values));
			}
			return result;
		}

		private static Dictionary<int, ITagClass> BuildClassLookup(List<ITagClass> classes)
		{
			var result = new Dictionary<int, ITagClass>();
			foreach (ITagClass tagClass in classes)
				result[tagClass.Magic] = tagClass;
			return result;
		}

		private List<ITag> ReadTags(IReader reader, uint tagTableOffset, int numTags, EngineDescription buildInfo,
			FileSegmentGroup metaArea)
		{
			StructureLayout layout = buildInfo.Layouts.GetLayout("tag entry");

			var result = new List<ITag>();
			reader.SeekTo(tagTableOffset);
			for (int i = 0; i < numTags; i++)
			{
				StructureValueCollection values = StructureReader.ReadStructure(reader, layout);
				result.Add(new SecondGenTag(values, metaArea, _classesById));
			}
			return result;
		}
	}
}