using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Scripting;
using Blamite.Flexibility;

namespace Blamite.Blam.ThirdGen.Scripting
{
    public class ThirdGenGlobal : IGlobal
    {
        public ThirdGenGlobal(StructureValueCollection values, ExpressionTable allExpressions)
        {
            Name = values.GetString("name");
            Type = (short)values.GetInteger("type");

            DatumIndex valueIndex = new DatumIndex(values.GetInteger("expression index"));
            if (valueIndex.IsValid)
                Value = allExpressions.FindExpression(valueIndex);
        }

        public string Name { get; private set; }
        public short Type { get; private set; }
        public IExpression Value { get; private set; }
    }
}
