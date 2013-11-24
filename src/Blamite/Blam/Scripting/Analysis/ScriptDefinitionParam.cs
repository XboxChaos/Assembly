using System.Diagnostics;

namespace Blamite.Blam.Scripting.Analysis
{
	/// <summary>
	///     A parameter in a script definition.
	/// </summary>
	[DebuggerDisplay("Parameter {Type} {Name}")]
	public class ScriptDefinitionParam
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="ScriptDefinitionParam" /> class.
		/// </summary>
		/// <param name="name">The name of the parameter.</param>
		/// <param name="type">The type of the parameter.</param>
		public ScriptDefinitionParam(string name, string type)
		{
			Name = name;
			Type = type;
		}

		/// <summary>
		///     Gets the name of the parameter.
		/// </summary>
		/// <value>The name of the parameter.</value>
		public string Name { get; private set; }

		/// <summary>
		///     Gets the type of the parameter.
		/// </summary>
		/// <value>The type of the parameter.</value>
		public string Type { get; private set; }
	}
}