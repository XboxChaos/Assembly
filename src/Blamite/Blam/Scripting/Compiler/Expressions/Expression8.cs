using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler.Expressions
{
    public class Expression8 : ExpressionBase
    {
        public override short ExpressionType
        {
            get
            {
                return 9;
            }
        }

        public byte[] Values { get; }



        public Expression8()
        {
            Values = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        }

        public Expression8(UInt16 salt, UInt16 opCode, UInt16 valType, UInt32 strAddr, Int16 line)
        {
            Salt = salt;
            OpCode = opCode;
            ValueType = valType;
            StringAddress = strAddr;
            LineNumber = line;
            Values = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        }

        public override string ValueToString
        {
            get
            {
                //string result = "";
                //foreach (byte b in values)
                //    result += (" " + b);

                return $"{Values[0]} | {Values[1]} | {Values[2]} | {Values[3]}";
            }
        }
    }
}
