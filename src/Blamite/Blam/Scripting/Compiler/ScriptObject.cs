using System.Collections.Generic;

namespace Blamite.Blam.Scripting.Compiler
{
	/// <summary>
	///     A named object which can be referenced by script code.
	/// </summary>
	public class ScriptObject
	{
		private readonly Dictionary<ScriptObjectReflexive, ScriptObject[]> _children =
			new Dictionary<ScriptObjectReflexive, ScriptObject[]>();

		/// <summary>
		///     Initializes a new instance of the <see cref="ScriptObject" /> class.
		/// </summary>
		/// <param name="name">The name of the object.</param>
		public ScriptObject(string name)
		{
			Name = name;
		}

		/// <summary>
		///     Gets the name of the object.
		/// </summary>
		/// <value>The name of the object.</value>
		public string Name { get; private set; }

		/// <summary>
		///     Registers an array of child objects with the object.
		/// </summary>
		/// <param name="source">The reflexive that the children belong to.</param>
		/// <param name="children">The child objects.</param>
		public void RegisterChildren(ScriptObjectReflexive source, ScriptObject[] children)
		{
			_children[source] = children;
		}

		/// <summary>
		///     Gets the child objects read from a child reflexive.
		/// </summary>
		/// <param name="source">The reflexive that the children belong to.</param>
		/// <returns>The child objects of the reflexive.</returns>
		public ScriptObject[] GetChildren(ScriptObjectReflexive source)
		{
			return _children[source];
		}

		/// <summary>
		///     Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		///     A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
		/// </returns>
		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}
	}
}