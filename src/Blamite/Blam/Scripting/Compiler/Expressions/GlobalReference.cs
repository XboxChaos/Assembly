using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler.Expressions
{
    public class GlobalReference : ExpressionBase
    {

        public GlobalReference()
        {
            Value = -1;
        }

        public Int32 Value { get; set; }


        public override short ExpressionType
        {
            get
            {
                return 13; ;
            }
        }

        public override string ValueToString
        {
            get
            {
                return Value.ToString();
            }
        }
    }
}
