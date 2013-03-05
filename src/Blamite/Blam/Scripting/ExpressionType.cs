using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting
{
    public enum ExpressionType : short
    {
        Group = 8,
        Expression = 9,
        ScriptReference = 10,
        GlobalsReference = 13,
        ParameterReference = 29
    }
}
