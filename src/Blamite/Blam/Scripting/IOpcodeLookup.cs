using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.Scripting
{
    public interface IOpcodeLookup
    {
        string GetScriptTypeName(ushort opcode);
        ushort GetScriptTypeOpcode(string name);

        ScriptValueType GetTypeInfo(ushort opcode);
        ScriptValueType GetTypeInfo(string name);

        ScriptFunctionInfo GetFunctionInfo(ushort opcode);
        List<ScriptFunctionInfo> GetFunctionInfo(string name);
    }
}
