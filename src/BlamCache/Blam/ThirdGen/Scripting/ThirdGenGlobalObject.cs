using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Scripting;
using ExtryzeDLL.Flexibility;

namespace ExtryzeDLL.Blam.ThirdGen.Scripting
{
    public class ThirdGenGlobalObject : IGlobalObject
    {
        public ThirdGenGlobalObject(StructureValueCollection values, IStringIDSource stringIDs)
        {
            Name = stringIDs.GetString(new StringID((int)values.GetInteger("name index")));
            Class = (short)values.GetInteger("type");
            PlacementIndex = (short)values.GetInteger("placement index");
        }

        public string Name { get; private set; }
        public short Class { get; private set; }
        public short PlacementIndex { get; private set; }
    }
}
