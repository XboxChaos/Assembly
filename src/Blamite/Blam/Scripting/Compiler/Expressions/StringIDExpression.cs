using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler.Expressions
{
    public class StringIDExpression : ExpressionBase
    {
        public override short ExpressionType
        {
            get
            {
                return 9;
            }
        }

        public StringID Value { get; set; }

        public StringIDExpression()
        {
            Value = new StringID();
        }

        public StringIDExpression(UInt16 salt, UInt16 opCode, UInt16 valType, UInt32 strAddr, Int16 line)
        {
            Salt = salt;
            OpCode = opCode;
            ValueType = valType;
            StringAddress = strAddr;
            LineNumber = line;
            Value = new StringID();
        }

        public override string ValueToString
        {
            get
            {
                return Value.Value.ToString();
            }
        }
    }
}
