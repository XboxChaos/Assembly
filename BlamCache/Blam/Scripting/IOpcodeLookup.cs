using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtryzeDLL.Blam.Scripting
{
    public interface IOpcodeLookup
    {
        string GetTagClassName(short opcode);
        string GetScriptTypeName(ushort opcode);
        ScriptValueType GetTypeInfo(ushort opcode);
        string GetFunctionName(ushort opcode);
    }
}
