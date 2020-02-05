using System;
using System.Collections.Generic;

namespace Blamite.Serialization
{
	public class ClassNameCollection
	{
		private readonly Dictionary<string, string> _classNames = new Dictionary<string, string>();

		/// <summary>
		///     Adds a name to the name collection.
		/// </summary>
		/// <param name="magic">The class magic as a string.</param>
		/// <param name="name">The name of the class.</param>
		public void AddName(string magic, string name)
		{
			if (_classNames.ContainsKey(magic))
				throw new InvalidOperationException("Class magic \"" + magic + "\" has multiple definitions");

			_classNames[magic] = name;
		}

		/// <summary>
		///     Adds a collection of names to this name collection.
		/// </summary>
		/// <param name="names">The names to add.</param>
		public void AddNames(ClassNameCollection names)
		{
			foreach (var cn in names._classNames)
				_classNames[cn.Key] = cn.Value;
		}

		/// <summary>
		///     Attempts to retrieve the defined name for the given class magic.
		/// </summary>
		/// <param name="magic">The class magic as a string.</param>
		/// <returns>The class name, otherwise null.</returns>
		public string RetrieveName(string magic)
		{
			string output = null;
			_classNames.TryGetValue(magic, out output);
			return output;

		}

	}
}
