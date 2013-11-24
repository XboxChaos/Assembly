using System.Collections.Generic;

namespace Blamite.Blam.Scripting
{
	/// <summary>
	///     Contains script code loaded from a script file.
	/// </summary>
	public class ScriptTable
	{
		/// <summary>
		///     Gets or sets the list of global variables in the file.
		/// </summary>
		public List<ScriptGlobal> Globals { get; set; }

		/// <summary>
		///     Gets or sets the list of scripts in the file.
		/// </summary>
		public List<Script> Scripts { get; set; }

		/// <summary>
		///     Gets or sets the table of expressions in the file.
		/// </summary>
		public ScriptExpressionTable Expressions { get; set; }
	}
}