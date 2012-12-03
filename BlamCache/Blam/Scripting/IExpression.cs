using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Blam.Scripting
{
    public interface IExpression
    {
        DatumIndex Index { get; }
        ushort Opcode { get; }
        ExpressionType Type { get; }

        short ValueType { get; }
        string StringValue { get; }
        uint Value { get; }
        short LineNumber { get; }

        IExpression Next { get; }
    }
}
