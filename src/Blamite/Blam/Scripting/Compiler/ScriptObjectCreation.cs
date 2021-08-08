using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.Scripting.Compiler
{
    public static class ScriptObjectCreation
    {
        public static Script GetScriptFromContext(HS_Gen1Parser.ScriptDeclarationContext context, DatumIndex rootExpressionIndex, OpcodeLookup opcodes)
        {
            // Create a new Script.
            Script script = new Script
            {
                Name = context.scriptID().GetTextSanitized(),
                ExecutionType = (short)opcodes.GetScriptTypeOpcode(context.SCRIPTTYPE().GetTextSanitized()),
                ReturnType = (short)opcodes.GetTypeInfo(context.VALUETYPE().GetTextSanitized()).Opcode,
                RootExpressionIndex = rootExpressionIndex
            };
            // Handle scripts with parameters.
            var parameterContext = context.scriptParameters();
            if (parameterContext != null)
            {
                var parameters = parameterContext.parameter();
                for (ushort i = 0; i < parameters.Length; i++)
                {
                    string name = parameters[i].ID().GetTextSanitized();
                    var valueTypeNode = parameters[i].VALUETYPE();
                    string valueType = valueTypeNode is null ? "script" : valueTypeNode.GetTextSanitized();

                    // Add the parameter to the script object.
                    ScriptParameter parameter = new ScriptParameter
                    {
                        Name = name,
                        Type = opcodes.GetTypeInfo(valueType).Opcode
                    };
                    script.Parameters.Add(parameter);
                }
            }
            return script;
        }
    }
}
