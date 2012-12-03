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
            Name = stringIDs.GetString((int)values.GetNumber("name index"));
            Class = (short)values.GetNumber("type");
            PlacementIndex = (short)values.GetNumber("placement index");
        }

        public string Name { get; private set; }
        public short Class { get; private set; }
        public short PlacementIndex { get; private set; }
    }
}
