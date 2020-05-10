using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Scripting.Compiler.Expressions;

namespace Blamite.Blam.Scripting.Compiler
{
    public class ScriptData
    {
        public StringTable Strings { get; private set; }
        public List<ScriptExpression> Expressions { get; private set; }
        public List<Script> Scripts  { get; private set; }
        public List<ScriptGlobal> Globals { get; private set; }
        public List<ITag> TagReferences { get; private set; }

        public ScriptData(List<Script> scripts, List<ScriptGlobal> globals, List<ITag> tagReferences, List<ScriptExpression> expressions, StringTable strings)
        {
            Strings = strings;
            Expressions = expressions;
            Scripts = scripts;
            Globals = globals;
            TagReferences = tagReferences;
        }
    }
}
