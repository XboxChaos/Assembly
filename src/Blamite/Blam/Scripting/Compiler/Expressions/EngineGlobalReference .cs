using System;
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
    }
}
