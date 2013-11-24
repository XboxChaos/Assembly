using System.Collections.Generic;
using System.Diagnostics;

namespace Blamite.Blam.Scripting.Analysis
{
	/// <summary>
	///     A script node representing a script definition.
	/// </summary>
	[DebuggerDisplay("Script {ExecutionType} {ReturnType} {Name}[{Parameters.Count}]")]
	public class ScriptDefinitionNode : IScriptNode
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="ScriptDefinitionNode" /> class.
		/// </summary>
		/// <param name="name">The name of the script.</param>
		/// <param name="executionType">The execution type of the script.</param>
		/// <param name="returnType">The return type of the script.</param>
		public ScriptDefinitionNode(string name, string executionType, string returnType)
		{
			Name = name;
			ExecutionType = executionType;
			ReturnType = returnType;

			Parameters = new List<ScriptDefinitionParam>();
			Nodes = new List<IScriptNode>();
		}

		/// <summary>
		///     Gets the name of the script.
		/// </summary>
		/// <value>The name of the script.</value>
		public string Name { get; private set; }

		/// <summary>
		///     Gets the execution type of the script.
		/// </summary>
		/// <value>The execution type of the script.</value>
		public string ExecutionType { get; private set; }

		/// <summary>
		///     Gets the return type of the script.
		/// </summary>
		/// <value>The return type of the script.</value>
		public string ReturnType { get; private set; }

		/// <summary>
		///     Gets the parameters for the script.
		/// </summary>
		/// <value>The parameters for the script.</value>
		public List<ScriptDefinitionParam> Parameters { get; private set; }

		/// <summary>
		///     Gets the child nodes of the script.
		/// </summary>
		/// <value>The child nodes of the script.</value>
		public List<IScriptNode> Nodes { get; private set; }

		public void Accept(IScriptNodeVisitor visitor)
		{
			visitor.VisitScriptDefinition(this);
		}
	}
}