using Blamite.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.FirstGen.Structures
{
    // TODO (Dragon): firstgen doesnt have an explicit group table
    //                so we have to "spoof" one while loading tags
    class FirstGenTagGroup : ITagGroup
    {
        public FirstGenTagGroup(StructureValueCollection values)
        {
            Load(values);
        }

        public int Magic { get; set; }
        public int ParentMagic { get; set; }
        public int GrandparentMagic { get; set; }
        public StringID Description { get; set; }

        private void Load(StructureValueCollection values)
        {
            Magic = (int)values.GetInteger("tag group magic");
            ParentMagic = (int)values.GetInteger("parent group magic");
            GrandparentMagic = (int)values.GetInteger("grandparent group magic");
            // No description stringid :(
        }
    }
}
