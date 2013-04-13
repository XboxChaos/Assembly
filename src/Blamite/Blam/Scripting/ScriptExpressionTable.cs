using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Scripting;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.Scripting
{
    /// <summary>
    /// A table of expressions in a script.
    /// </summary>
    public class ScriptExpressionTable : IEnumerable<ScriptExpression>
    {
        private List<ScriptExpression> _expressions = new List<ScriptExpression>();
        //private Stack<int> _freeIndices = new Stack<int>();

        public void AddExpression(ScriptExpression expression)
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

        public void AddExpressions(IEnumerable<ScriptExpression> expressions)
        {
            foreach (var expression in expressions)
                AddExpression(expression);
        }

        public ScriptExpression FindExpression(DatumIndex index)
        {
            if (!index.IsValid || index.Index >= _expressions.Count)
                return null;
            return _expressions[index.Index];
        }

        public IEnumerator<ScriptExpression> GetEnumerator()
        {
            return _expressions.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _expressions.GetEnumerator();
        }
    }
}
