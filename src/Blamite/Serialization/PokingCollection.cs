using System;
using System.Collections.Generic;

namespace Blamite.Serialization
{
	public class PokingCollection
	{
		private readonly Dictionary<string, long> _collection = new Dictionary<string, long>();

		/// <summary>
		///     Adds a builder version to the poking collection.
		/// </summary>
		/// <param name="version">The version string of the game.</param>
		/// <param name="pointer">The pointer for the map header.</param>
		public void AddName(string version, long pointer)
		{
			if (_collection.ContainsKey(version))
				throw new InvalidOperationException("Build version \"" + version + "\" has multiple poking definitions");

			_collection[version] = pointer;
		}

		/// <summary>
		///     Adds a collection of names to this name collection.
		/// </summary>
		/// <param name="versions">The versions to add.</param>
		public void AddNames(PokingCollection versions)
		{
			foreach (var cn in versions._collection)
				_collection[cn.Key] = cn.Value;
		}

		/// <summary>
		///     Attempts to retrieve the defined pointer for the given build.
		/// </summary>
		/// <param name="version">The build version.</param>
		/// <returns>The pointer, otherwise -1.</returns>
		public long RetrievePointer(string version)
		{
			long output = -1;
			_collection.TryGetValue(version, out output);
			return output;

		}

	}
}
