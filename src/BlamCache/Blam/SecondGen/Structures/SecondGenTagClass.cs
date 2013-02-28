using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Flexibility;

namespace ExtryzeDLL.Blam.SecondGen.Structures
{
    public class SecondGenTagClass : ITagClass
    {
        public SecondGenTagClass(StructureValueCollection values)
        {
            Load(values);
        }

        private void Load(StructureValueCollection values)
        {
            Magic = (int)values.GetInteger("magic");
            ParentMagic = (int)values.GetInteger("parent magic");
            GrandparentMagic = (int)values.GetInteger("grandparent magic");
            // No description stringid :(
        }

        public int Magic { get; set; }
        public int ParentMagic { get; set; }
        public int GrandparentMagic { get; set; }
        public StringID Description { get; set; }
    }
}
