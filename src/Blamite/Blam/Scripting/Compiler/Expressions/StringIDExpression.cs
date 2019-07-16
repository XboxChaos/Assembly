using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.IO;

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


        public override void Write(IWriter writer)
        {
            writer.WriteUInt16(Salt);
            writer.WriteUInt16(OpCode);
            writer.WriteUInt16(ValueType);
            writer.WriteInt16(ExpressionType);
            writer.WriteUInt16(NextExpression.Salt);
            writer.WriteUInt16(NextExpression.Index);
            writer.WriteUInt32(StringAddress);

            writer.WriteUInt32(Value.Value);

            writer.WriteInt16(LineNumber);
            writer.WriteUInt16(0);
        }
    }
}
