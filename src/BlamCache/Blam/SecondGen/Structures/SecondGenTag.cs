using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.Flexibility;

namespace ExtryzeDLL.Blam.SecondGen.Structures
{
    public class SecondGenTag : ITag
    {
        public SecondGenTag(StructureValueCollection values, MetaOffsetConverter converter, Dictionary<int, ITagClass> classesById)
        {
            Load(values, converter, classesById);
        }

        private void Load(StructureValueCollection values, MetaOffsetConverter converter, Dictionary<int, ITagClass> classesById)
        {
            // Load the tag class by looking up the magic value that's stored
            ITagClass tagClass;
            int classMagic = (int)values.GetNumber("class magic");
            if (classesById.TryGetValue(classMagic, out tagClass))
                Class = tagClass;

            MetaLocation = new Pointer(values.GetNumber("offset"), converter);
            Index = new DatumIndex(values.GetNumber("datum index"));
            DataSize = (int)values.GetNumber("data size");
        }

        public ITagClass Class { get; set; }
        public Pointer MetaLocation { get; set; }
        public DatumIndex Index { get; private set; }
        public int DataSize { get; set; }
    }
}
