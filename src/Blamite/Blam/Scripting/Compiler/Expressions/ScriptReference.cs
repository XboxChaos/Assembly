using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler.Expressions
{
    public class ScriptReference : ExpressionBase
    {
        public override short ExpressionType
        {
            get
            {
                return 10;
            }
        }

        public DatumIndex Value { get; set; }

        public ScriptReference()
        {
            StringAddress = 0xCDCDCDCD;
        }

        public override string ValueToString
        {
            get
            {
                return $"{Value.Salt} | {Value.Index}";
            }
        }
    }
}
