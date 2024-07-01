using System;
using System.Collections.Generic;
using System.Linq;

namespace Blamite.Serialization
{
	public class PokingCollection
	{
		private readonly SortedDictionary<Version, PokingInformation> _collection = new SortedDictionary<Version, PokingInformation>();

		/// <summary>
		///     Adds a builder version to the poking collection.
		/// </summary>
		/// <param name="version">The version string of the game.</param>
		/// <param name="pointer">The pointer for the map header.</param>
		public void AddName(Version version, PokingInformation info)
		{
			if (_collection.ContainsKey(version))
				throw new InvalidOperationException("Build version \"" + version + "\" has multiple poking definitions");

			_collection[version] = info;
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
		public PokingInformation RetrieveInformation(string version)
		{
			if (string.IsNullOrEmpty(version))
				return null;
			Version v = new Version(version);
			return RetrieveInformation(v);
		}

		/// <summary>
		///     Attempts to retrieve the defined pointer for the given build.
		/// </summary>
		/// <param name="version">The build version.</param>
		/// <returns>The pointer, otherwise -1.</returns>
		public PokingInformation RetrieveInformation(Version version)
		{
			PokingInformation output = null;
			_collection.TryGetValue(version, out output);
			return output;
		}

		/// <summary>
		///     Attempts to retrieve the pointer of the last defined version in the collection.
		/// </summary>
		/// <returns>The pointer, otherwise -1.</returns>
		public PokingInformation RetrieveLatestInfo()
		{
			//hacky winstore fix yuck
			if (_collection.Count == 0)
				return null;

			return _collection.OrderByDescending(p => p.Key).First().Value;
		}

		/// <summary>
		///		Returns the entire stored collection for external iteration.
		/// </summary>
		/// <returns>All poking definitions.</returns>
		public List<PokingInformation> GetVersions()
		{
			return _collection.Values.ToList();
		}
	}
}
