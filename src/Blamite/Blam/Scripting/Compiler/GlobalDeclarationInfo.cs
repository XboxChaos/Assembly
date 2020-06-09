using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler
{
    public class GlobalDeclarationInfo
    {
        public string Name { get; private set; }
        public string ReturnType { get; private set; }

        public GlobalDeclarationInfo(BS_ReachParser.GloDeclContext context)
        {
            Name = context.ID().GetText();
            ReturnType = context.VALUETYPE().GetText();
        }
    }
}
