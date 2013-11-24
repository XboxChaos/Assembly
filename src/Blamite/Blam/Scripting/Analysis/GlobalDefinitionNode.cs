using System.Diagnostics;

namespace Blamite.Blam.Scripting.Analysis
{
	/// <summary>
	///     A script node representing a script global definition.
	/// </summary>
	[DebuggerDisplay("Global {Type} {Name}")]
	public class GlobalDefinitionNode : IScriptNode
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="GlobalDefinitionNode" /> class.
		/// </summary>
		/// <param name="name">The name of the global.</param>
		/// <param name="type">The type of the global.</param>
		/// <param name="valueNode">The node representing the global's value.</param>
		public GlobalDefinitionNode(string name, string type, IScriptNode valueNode)
		{
			Name = name;
			Type = type;
			Value = valueNode;
		}

		/// <summary>
		///     Gets the name of the global.
		/// </summary>
		/// <value>The name of the global.</value>
		public string Name { get; private set; }

		/// <summary>
		///     Gets the type of the global.
		/// </summary>
		/// <value>The type of the global.</value>
		public string Type { get; private set; }

		/// <summary>
		///     Gets the node representing the value of the global.
		/// </summary>
		/// <value>The node representing the value of the global.</value>
		public IScriptNode Value { get; private set; }

		public void Accept(IScriptNodeVisitor visitor)
		{
			visitor.VisitGlobalDefinition(this);
		}
	}
}