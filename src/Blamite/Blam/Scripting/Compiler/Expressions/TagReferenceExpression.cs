using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler.Expressions
{
    public class TagReferenceExpression : ExpressionBase
    {
        public override short ExpressionType
        {
            get
            {
                return 9;
            }
        }

        public DatumIndex Value { get; set; }

        public TagReferenceExpression()
        {
            StringAddress = 0xCDCDCDCD;
            Value = new DatumIndex();
        }

        public TagReferenceExpression(UInt16 salt, UInt16 opCode, UInt16 valType, UInt32 strAddr, Int16 line)
        {
            Salt = salt;
            OpCode = opCode;
            ValueType = valType;
            StringAddress = strAddr;
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
