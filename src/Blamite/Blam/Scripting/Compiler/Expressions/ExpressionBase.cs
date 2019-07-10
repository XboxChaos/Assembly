using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler.Expressions
{
    public abstract class ExpressionBase
    {
        public UInt16 Salt { get; set; }
        public UInt16 OpCode { get; set; }
        public UInt16 ValueType { get; set; }
        public abstract Int16 ExpressionType { get; }
        public DatumIndex NextExpression { get; set; }
        public UInt32 StringAddress { get; set; }
        public Int16 LineNumber { get; set; }
        public abstract string ValueToString { get; }

        public void SetCommonValues(UInt16 salt, UInt16 opCode, UInt16 valType, UInt32 strAddr, Int16 line)
        {
            Salt = salt;
            OpCode = opCode;
            ValueType = valType;
            StringAddress = strAddr;
            LineNumber = line;
        }
    }
}
