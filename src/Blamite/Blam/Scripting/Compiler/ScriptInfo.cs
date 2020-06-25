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

        public ScriptInfo(BS_ReachParser.ScriDeclContext context, ushort index)
        {
            Name = context.scriptID().GetText();
            ScriptType = context.SCRIPTTYPE().GetText();
            ReturnType = context.retType().GetText();
            Parameters = new List<ParameterInfo>();
            Opcode = index;

            if (context.scriptParams() != null)
            {
                string[] names = context.scriptParams().ID().Select(n => n.GetText()).ToArray();
                string[] valueTypes = context.scriptParams().VALUETYPE().Select(v => v.GetText()).ToArray();
                // extract strings from the context

                if (names.Count() != valueTypes.Count())
                {
                    throw new InvalidOperationException($"Failed to create parameter information for Script \"{Name}\" - Mismatched parameter arrays. Line: {context.Start.Line}");

                }

                // Create parameters from the extracted strings
                for (ushort i = 0; i < names.Count(); i++)
                {
                    var param = new ParameterInfo(names[i], valueTypes[i], i);
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
