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

        public ScriptReference(UInt16 salt, UInt16 opCode, UInt16 valType, Int16 line)
        {
            Salt = salt;
            OpCode = opCode;
            ValueType = valType;
            StringAddress = 0xCDCDCDCD;
            LineNumber = line;
            Value = new DatumIndex();
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
