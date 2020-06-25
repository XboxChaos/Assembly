using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler
{
    public class ParameterInfo :IScriptingConstantInfo
    {
        public string Name { get; private set; }
        public string ReturnType { get; private set; }        
        public ushort Opcode { get; private set; }

        public ParameterInfo(string name, string valueType, ushort index)
        {
            Name = name;
            ReturnType = valueType;
            Opcode = index;
        }
    }
}
