using System;
using System.Collections.Generic;
using System.Linq;

namespace Blamite.Serialization
{
	public class PokingCollection
	{
		private readonly SortedDictionary<Version, long> _collection = new SortedDictionary<Version, long>();

		/// <summary>
		///     Adds a builder version to the poking collection.
		/// </summary>
		/// <param name="version">The version string of the game.</param>
		/// <param name="pointer">The pointer for the map header.</param>
		public void AddName(Version version, long pointer)
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
			Version v = new Version(version);
			return RetrievePointer(v);
		}

		/// <summary>
		///     Attempts to retrieve the defined pointer for the given build.
		/// </summary>
		/// <param name="version">The build version.</param>
		/// <returns>The pointer, otherwise -1.</returns>
		public long RetrievePointer(Version version)
		{
			long output = 0;
			_collection.TryGetValue(version, out output);
			return output;

		}

		/// <summary>
		///     Attempts to retrieve the pointer of the last defined version in the collection.
		/// </summary>
		/// <returns>The pointer, otherwise -1.</returns>
		public long RetrieveLastPointer()
		{
			//hacky winstore fix yuck
			if (_collection.Count == 0)
				return -1;

			return _collection.OrderByDescending(p => p.Key).First().Value;
		}
	}
}
