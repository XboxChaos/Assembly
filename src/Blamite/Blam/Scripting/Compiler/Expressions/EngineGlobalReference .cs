using System;
using Blamite.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler.Expressions
{
    public class EngineGlobalReference : ExpressionBase
    {

        public const ushort Padding = 0xFFFF;
        public ushort MaskedOpcode { get; set; }


        public override short ExpressionType
        {
            get
            {
                return 13; ;
            }
        }

        public EngineGlobalReference()
        {
            MaskedOpcode = 0xFFFF;
        }


        public EngineGlobalReference(UInt16 salt, UInt16 opCode, UInt16 valType, UInt32 strAddr, ushort maskedOpcode, Int16 line)
        {
            Salt = salt;
            OpCode = opCode;
            ValueType = valType;
            StringAddress = strAddr;
            LineNumber = line;
            MaskedOpcode = maskedOpcode;
        }

        public override string ValueToString
        {
            get
            {
                return $"{Padding} | {MaskedOpcode}";
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

            writer.WriteUInt16(Padding);
            writer.WriteUInt16(MaskedOpcode);

            writer.WriteInt16(LineNumber);
            writer.WriteUInt16(0);
        }
    }
}
