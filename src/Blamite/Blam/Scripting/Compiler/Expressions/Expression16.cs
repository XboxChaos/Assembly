using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler.Expressions
{
    public class Expression16 : ExpressionBase
    {
        public override short ExpressionType
        {
            get
            {
                return 9;
            }
        }

        public Int16[] Values { get; }



        public Expression16()
        {
            Values = new Int16[] { -1, -1 };
        }

        public Expression16(UInt16 salt, UInt16 opCode, UInt16 valType, UInt32 strAddr, Int16 line)
        {
            Salt = salt;
            OpCode = opCode;
            ValueType = valType;
            StringAddress = strAddr;
            LineNumber = line;
            Values = new Int16[] { -1, -1 };
        }

        public override string ValueToString
        {
            get
            {
                //string result = "";
                //foreach(int16 i in values)
                //    result += (" " + i);

                return $"{Values[0]} | {Values[1]}";
            }
        }
    }
}
