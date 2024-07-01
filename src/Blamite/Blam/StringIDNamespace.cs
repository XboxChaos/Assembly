
namespace Blamite.Blam
{
	/// <summary>
	///     A stringID set definition.
	/// </summary>
	public class StringIDNamespace
	{
		/// <summary>
		///     Constructs a new set definition.
		/// </summary>
		/// <param name="id">The set's ID number.</param>
		/// <param name="minIndex">The minimum index that a stringID must have in order to be counted as part of the namespace.</param>
		/// <param name="globalIndex">The index of the namespace's first string in the global stringID table.</param>
		public StringIDNamespace(int id, int minIndex, int globalIndex)
		{
			ID = id;
			MinIndex = minIndex;
			GlobalIndex = globalIndex;
		}

		/// <summary>
		///     The set's ID number.
		/// </summary>
		public int ID { get; private set; }

		/// <summary>
		///     The minimum index that a stringID must have in order to be counted as part of the set.
		/// </summary>
		public int MinIndex { get; private set; }

		/// <summary>
		///     The index of the set's first string in the global stringID table.
		/// </summary>
		public int GlobalIndex { get; private set; }
	}
}
