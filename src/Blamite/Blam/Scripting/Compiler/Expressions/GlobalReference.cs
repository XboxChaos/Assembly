﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
}
