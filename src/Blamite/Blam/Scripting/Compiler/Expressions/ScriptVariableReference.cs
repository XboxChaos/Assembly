using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler.Expressions
{
    public class ScriptVariableReference : ExpressionBase
    {
        public override short ExpressionType
        {
            get
            {
                return 29;
            }
        }

        public Int32 Value { get; set; }

        public ScriptVariableReference()
        {
            Value = -1;
        }

        public ScriptVariableReference(UInt16 salt, UInt16 opCode, UInt16 valType, UInt32 strAddr, Int16 line)
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
