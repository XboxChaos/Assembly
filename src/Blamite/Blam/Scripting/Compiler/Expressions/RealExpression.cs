using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler.Expressions
{
    public class RealExpression : ExpressionBase
    {
        public override short ExpressionType
        {
            get
            {
                return 9;
            }
        }

        public float Value { get; set; }

        public RealExpression()
        {
            Value = 0;
            StringAddress = 0xCDCDCDCD;
        }

        public RealExpression(UInt16 salt, UInt16 opCode, UInt16 valType, Int16 line)
        {
            Salt = salt;
            OpCode = opCode;
            ValueType = valType;
            StringAddress = 0xCDCDCDCD;
            LineNumber = line;
            Value = 0;
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
