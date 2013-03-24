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
        ScriptValueType GetTypeInfo(ushort opcode);
        string GetFunctionName(ushort opcode);
    }
}
