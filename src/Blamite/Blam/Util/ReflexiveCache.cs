using System.Collections.Generic;
using System.Linq;

namespace Blamite.Blam.Util
{
	/// <summary>
	///     Contains a collection of reflexives which can have be looked up and reused.
	/// </summary>
	/// <typeparam name="EntryType">The type of each entry in the reflexive. This type must implement Equals.</typeparam>
	public class ReflexiveCache<EntryType>
	{
		private readonly Dictionary<int, List<CachedReflexive>> _reflexivesBySize =
			new Dictionary<int, List<CachedReflexive>>();

		/// <summary>
		///     Adds a reflexive to the cache.
		/// </summary>
		/// <param name="address">The address of the reflexive.</param>
		/// <param name="entries">The entries in the reflexive.</param>
		public void Add(uint address, ICollection<EntryType> entries)
		{
			List<CachedReflexive> reflexives;
			if (!_reflexivesBySize.TryGetValue(entries.Count, out reflexives))
			{
				reflexives = new List<CachedReflexive>();
				_reflexivesBySize[entries.Count] = reflexives;
			}

			var reflexive = new CachedReflexive(address, entries);
			reflexives.Add(reflexive);
		}

		/// <summary>
		///     Checks if a reflexive has been cached and retrieves its address if it is.
		/// </summary>
		/// <param name="reflexive">The reflexive to search for.</param>
		/// <param name="address">The variable to store the reflexive's address to on success.</param>
		/// <returns>true if the reflexive was cached and its address was retrieved.</returns>
		public bool TryGetAddress(ICollection<EntryType> reflexive, out uint address)
		{
			address = 0;

			// Find all reflexives that have the given size
			List<CachedReflexive> reflexives;
			if (!_reflexivesBySize.TryGetValue(reflexive.Count, out reflexives))
				return false;

			// Compare the entries of each reflexive
			foreach (CachedReflexive cached in reflexives)
			{
				if (cached.Entries.SequenceEqual(reflexive))
				{
					address = cached.Address;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		///     A cached reflexive.
		/// </summary>
		private class CachedReflexive
		{
			/// <summary>
			///     Initializes a new instance of the <see cref="ReflexiveCache{EntryType}.CachedReflexive" /> class.
			/// </summary>
			/// <param name="address">The address of the reflexive.</param>
			/// <param name="entries">The entries in the reflexive.</param>
			public CachedReflexive(uint address, ICollection<EntryType> entries)
			{
				Address = address;
				Entries = entries;
			}

			/// <summary>
			///     Gets the address of the cached reflexive.
			/// </summary>
			public uint Address { get; private set; }

			/// <summary>
			///     Gets the entries in the cached reflexive.
			/// </summary>
			public ICollection<EntryType> Entries { get; private set; }
		}
	}
}