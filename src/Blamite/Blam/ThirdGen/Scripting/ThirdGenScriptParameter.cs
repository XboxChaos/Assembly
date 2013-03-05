using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Scripting;
using Blamite.Flexibility;

namespace Blamite.Blam.ThirdGen.Scripting
{
    public class ThirdGenScriptParameter : IScriptParameter
    {
        public ThirdGenScriptParameter(StructureValueCollection values)
        {
            Load(values);
        }

        public string Name { get; private set; }
        public short Type { get; private set; }

        private void Load(StructureValueCollection values)
        {
            Name = values.GetString("name");
            Type = (short)values.GetInteger("type");
        }
    }
}
