using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.IO;

namespace Blamite.Blam.Scripting.Compiler.Expressions
{
    public class GlobalReference : ExpressionBase
    {
        public Int32 Value { get; set; }

        public override short ExpressionType
        {
            get
            {
                return 13; ;
            }
        }

        public GlobalReference()
        {
            Value = -1;
        }

        public GlobalReference(UInt16 salt, UInt16 opCode, UInt16 valType, UInt32 strAddr, int value, Int16 line)
        {
            Salt = salt;
            OpCode = opCode;
            ValueType = valType;
            StringAddress = strAddr;
            LineNumber = line;
            Value = value;
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

            writer.WriteInt32(Value);

            writer.WriteInt16(LineNumber);
            writer.WriteUInt16(0);
        }
    }
}
