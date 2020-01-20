using System;
using System.Collections.Generic;

namespace Blamite.Serialization
{
	public class GroupNameCollection
	{
		private readonly Dictionary<string, string> _groupNames = new Dictionary<string, string>();

		/// <summary>
		///     Adds a name to the name collection.
		/// </summary>
		/// <param name="magic">The group magic as a string.</param>
		/// <param name="name">The name of the group.</param>
		public void AddName(string magic, string name)
		{
			if (_groupNames.ContainsKey(magic))
				throw new InvalidOperationException("Group magic \"" + magic + "\" has multiple definitions");

			_groupNames[magic] = name;
		}

		/// <summary>
		///     Adds a collection of names to this name collection.
		/// </summary>
		/// <param name="names">The names to add.</param>
		public void AddNames(GroupNameCollection names)
		{
			foreach (var cn in names._groupNames)
				_groupNames[cn.Key] = cn.Value;
		}

		/// <summary>
		///     Attempts to retrieve the defined name for the given group magic.
		/// </summary>
		/// <param name="magic">The group magic as a string.</param>
		/// <returns>The group name, otherwise null.</returns>
		public string RetrieveName(string magic)
		{
			string output = null;
			_groupNames.TryGetValue(magic, out output);
			return output;

		}

	}
}
