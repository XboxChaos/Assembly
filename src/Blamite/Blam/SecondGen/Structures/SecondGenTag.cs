using System.Collections.Generic;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.SecondGen.Structures
{
	public class SecondGenTag : ITag
	{
		public SecondGenTag(StructureValueCollection values, FileSegmentGroup metaArea, Dictionary<int, ITagClass> classesById)
		{
			Load(values, metaArea, classesById);
		}

		public int DataSize { get; set; }

		public ITagClass Class { get; set; }
		public SegmentPointer MetaLocation { get; set; }
		public DatumIndex Index { get; private set; }

		private void Load(StructureValueCollection values, FileSegmentGroup metaArea, Dictionary<int, ITagClass> classesById)
		{
			uint offset = values.GetInteger("offset");
			if (offset > 0)
				MetaLocation = SegmentPointer.FromPointer(offset, metaArea);

			// Load the tag class by looking up the magic value that's stored
			var classMagic = (int) values.GetInteger("class magic");
			if (classMagic != -1)
				Class = classesById[classMagic];

			Index = new DatumIndex(values.GetInteger("datum index"));
			DataSize = (int) values.GetInteger("data size");
		}
	}
}