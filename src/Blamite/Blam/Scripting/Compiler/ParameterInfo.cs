using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler
{
    public class ParameterInfo
    {
        public string Name { get; private set; }
        public string ValueType { get; private set; }

        public ParameterInfo(string name, string valueType)
        {
            Name = name;
            ValueType = valueType;
        }
    }
}
