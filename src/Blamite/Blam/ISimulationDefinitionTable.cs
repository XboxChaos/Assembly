using System.Collections.Generic;

namespace Blamite.Blam
{
	/// <summary>
	/// A simulation definition table in a cache file.
	/// </summary>
	public interface ISimulationDefinitionTable : IEnumerable<ITag>
	{
		/// <summary>
		/// Adds a tag to the table.
		/// If it is already in the table, nothing will be changed.
		/// </summary>
		/// <param name="tag">The tag to add.</param>
		void Add(ITag tag);

		/// <summary>
		/// Removes a tag from the table.
		/// If it is not in the table, nothing will be changed.
		/// </summary>
		/// <param name="tag"></param>
		void Remove(ITag tag);

		/// <summary>
		/// Checks whether or not a tag is in the table.
		/// </summary>
		/// <param name="tag">The tag to search for.</param>
		/// <returns><c>true</c> if the tag is in the table.</returns>
		bool Contains(ITag tag);

		/// <summary>
		/// Gets the number of tags in the table.
		/// </summary>
		int Count { get; }
	}
}
