using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler
{
    public class ScriptInfo : IScriptingConstantInfo
    {
        public string Name { get; private set; }
        public string ScriptType { get; private set; }
        public string ReturnType { get; private set; }
        public List<ParameterInfo> Parameters { get; private set; }

        /// <summary>
        ///     Gets the opcode of the global.
        /// </summary>
        /// <value>The opcode of the function.</value>
        public ushort Opcode { get; private set; }

        public ScriptInfo(BS_ReachParser.ScriptDeclarationContext context, ushort index)
        {
            Name = context.scriptID().GetText();
            ScriptType = context.SCRIPTTYPE().GetText();
            ReturnType = context.VALUETYPE().GetText();
            Parameters = new List<ParameterInfo>();
            Opcode = index;

            var parameterContext = context.scriptParameters();
            if (parameterContext != null)
            {
                var parameters = parameterContext.parameterGroup();
                // Create parameters from the extracted strings
                for (ushort i = 0; i < parameters.Length; i++)
                {
                    string name = parameters[i].ID().GetText();
                    string valueType = parameters[i].VALUETYPE().GetText();
                    var param = new ParameterInfo(name, valueType, i);
                    Parameters.Add(param);
                }
            }                
        }

        public ScriptInfo(string name, string scriptType, string returnType, ushort index)
        {
            Name = name;
            ScriptType = scriptType;
            ReturnType = returnType;
            Parameters = new List<ParameterInfo>();
            Opcode = index;
        }
    }
}
