using System.Diagnostics;

namespace Blamite.Blam.Scripting.Analysis
{
	/// <summary>
	///     A script node representing a variable or parameter reference.
	/// </summary>
	[DebuggerDisplay("Reference {Name}")]
	public class VariableReferenceNode : IScriptNode
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="VariableReferenceNode" /> class.
		/// </summary>
		/// <param name="name">The name of the variable.</param>
		public VariableReferenceNode(string name)
		{
			Name = name;
		}

		/// <summary>
		///     Gets the name of the variable.
		/// </summary>
		/// <value>The name of the variable.</value>
		public string Name { get; private set; }

		public void Accept(IScriptNodeVisitor visitor)
		{
			visitor.VisitVariableReference(this);
		}
	}
}