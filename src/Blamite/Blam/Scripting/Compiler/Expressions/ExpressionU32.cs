using System;
using Blamite.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler.Expressions
{
    public class ExpressionU32 : ExpressionBase
    {
        public override short ExpressionType
        {
            get
            {
                return 9;
            }
        }

        public UInt32 Value { get; set; }

        public ExpressionU32()
        {
            Value = 0xFFFFFFFF;
        }

        public ExpressionU32(UInt16 salt, UInt16 opCode, UInt16 valType, UInt32 strAddr, Int16 line)
        {
            Salt = salt;
            OpCode = opCode;
            ValueType = valType;
            StringAddress = strAddr;
            LineNumber = line;
            Value = 0xFFFFFFFF;
        }

        public override string ValueToString
        {
            get
            {
                return Value.ToString();
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

            writer.WriteUInt32(Value);

            writer.WriteInt16(LineNumber);
            writer.WriteUInt16(0);
        }
    }
}
