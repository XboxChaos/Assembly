using System.Collections.Generic;
using System.Diagnostics;

namespace Blamite.Blam.Scripting.Analysis
{
	/// <summary>
	///     A node representing a function call.
	/// </summary>
	[DebuggerDisplay("Call {FunctionName}[{Arguments.Count}]")]
	public class FunctionCallNode : IScriptNode
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="FunctionCallNode" /> class.
		/// </summary>
		/// <param name="functionName">The name of the function to call.</param>
		public FunctionCallNode(string functionName)
		{
			FunctionName = functionName;
			Arguments = new List<IScriptNode>();
		}

		/// <summary>
		///     Gets the name of the function to call.
		/// </summary>
		/// <value>The name of the function to call.</value>
		public string FunctionName { get; private set; }

		/// <summary>
		///     Gets the arguments for the call.
		/// </summary>
		/// <value>The arguments for the call.</value>
		public List<IScriptNode> Arguments { get; private set; }

		public void Accept(IScriptNodeVisitor visitor)
		{
			visitor.VisitFunctionCall(this);
		}
	}
}