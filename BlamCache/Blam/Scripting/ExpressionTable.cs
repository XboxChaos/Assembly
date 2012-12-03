using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Scripting;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam.Scripting
{
    public class ExpressionTable : IEnumerable<IExpression>
    {
        private List<IExpression> _expressions = new List<IExpression>();
        private Stack<int> _freeIndices = new Stack<int>();

        public void AddExpression(IExpression expression)
        {
            if (expression.Index.IsValid)
            {
                if (expression.Index.Index < _expressions.Count)
                {
                    _expressions[expression.Index.Index] = expression;
                }
                else
                {
                    for (int i = _expressions.Count; i < expression.Index.Index; i++)
                        _expressions.Add(null);
                    _expressions.Add(expression);
                }
            }
        }

        public IExpression FindExpression(DatumIndex index)
        {
            if (!index.IsValid || index.Index >= _expressions.Count)
                return null;
            return _expressions[index.Index];
        }

        public IEnumerator<IExpression> GetEnumerator()
        {
            return _expressions.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _expressions.GetEnumerator();
        }
    }
}
