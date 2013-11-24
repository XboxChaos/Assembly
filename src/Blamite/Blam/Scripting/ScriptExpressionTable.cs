using System.Collections;
using System.Collections.Generic;

namespace Blamite.Blam.Scripting
{
	/// <summary>
	///     A table of expressions in a script.
	/// </summary>
	public class ScriptExpressionTable : IEnumerable<ScriptExpression>
	{
		private readonly List<ScriptExpression> _expressions = new List<ScriptExpression>();

		public IEnumerator<ScriptExpression> GetEnumerator()
		{
			return _expressions.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _expressions.GetEnumerator();
		}

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
			foreach (ScriptExpression expression in expressions)
				AddExpression(expression);
		}

		public ScriptExpression FindExpression(DatumIndex index)
		{
			if (!index.IsValid || index.Index >= _expressions.Count)
				return null;
			return _expressions[index.Index];
		}
	}
}