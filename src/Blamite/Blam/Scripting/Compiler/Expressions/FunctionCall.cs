using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler.Expressions
{
    public class FunctionCall : ExpressionBase
    {
        public override short ExpressionType
        {
            get
            {
                return 8;
            }
        }

        public DatumIndex Value { get; set; }

        public FunctionCall()
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
