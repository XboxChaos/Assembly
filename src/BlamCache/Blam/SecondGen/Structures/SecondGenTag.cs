using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam.SecondGen.Structures
{
    public class SecondGenTag : ITag
    {
        public SecondGenTag(StructureValueCollection values, FileSegmentGroup metaArea, Dictionary<int, ITagClass> classesById)
        {
            Load(values, metaArea, classesById);
        }

        private void Load(StructureValueCollection values, FileSegmentGroup metaArea, Dictionary<int, ITagClass> classesById)
        {
            uint offset = values.GetInteger("offset");
            if (offset > 0)
            {
                // Load the tag class by looking up the magic value that's stored
                int classMagic = (int)values.GetInteger("class magic");
                if (classMagic != -1)
                    Class = classesById[classMagic];

                MetaLocation = SegmentPointer.FromPointer(offset, metaArea);
                Index = new DatumIndex(values.GetInteger("datum index"));
                DataSize = (int)values.GetInteger("data size");
            }
        }

        public ITagClass Class { get; set; }
        public SegmentPointer MetaLocation { get; set; }
        public DatumIndex Index { get; private set; }
        public int DataSize { get; set; }
    }
}
