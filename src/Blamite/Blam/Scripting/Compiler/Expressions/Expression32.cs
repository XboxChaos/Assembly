using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler.Expressions
{
    public class Expression32 : ExpressionBase
    {
        public override short ExpressionType
        {
            get
            {
                return 9;
            }
        }

        public Int32 Value { get; set; }

        public Expression32()
        {
            Value = -1;
        }

        public Expression32(UInt16 salt, UInt16 opCode, UInt16 valType, UInt32 strAddr, Int16 line)
        {
            Salt = salt;
            OpCode = opCode;
            ValueType = valType;
            StringAddress = strAddr;
            LineNumber = line;
            Value = -1;
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
