using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.IO;

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

        public FunctionCall(UInt16 salt, UInt16 opcode, UInt16 valType, Int16 line)
        {
            Salt = salt;
            OpCode = opcode;
            ValueType = valType;
            LineNumber = line;
            StringAddress = 0xCDCDCDCD;
        }

        public override string ValueToString
        {
            get
            {
                return $"{Value.Salt} | {Value.Index}";
            }
        }

        public override void Write(IWriter writer)
        {
            writer.WriteUInt16(Salt);
            writer.WriteUInt16(OpCode);
            writer.WriteUInt16(ValueType);
            writer.WriteInt16(ExpressionType);
            writer.WriteUInt16(NextExpression.Salt);
            writer.WriteUInt16(NextExpression.Index);
            writer.WriteUInt32(StringAddress);

            writer.WriteUInt16(Value.Salt);
            writer.WriteUInt16(Value.Index);

            writer.WriteInt16(LineNumber);
            writer.WriteUInt16(0);
        }
    }
}
