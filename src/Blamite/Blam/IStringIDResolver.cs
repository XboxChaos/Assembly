using Blamite.Flexibility;

namespace Blamite.Blam
{
	/// <summary>
	///     Interface for an object which converts stringIDs to and from array indices.
	/// </summary>
	public interface IStringIDResolver
	{
		/// <summary>
		///     Gets the layout of stringIDs.
		/// </summary>
		StringIDLayout IDLayout { get; }

		/// <summary>
		///     Translates a stringID into an index into the global debug strings array.
		/// </summary>
		/// <param name="id">The StringID to translate.</param>
		/// <returns>The index of the string in the global debug strings array.</returns>
		int StringIDToIndex(StringID id);

		/// <summary>
		///     Translates a string index into a stringID which can be written to the file.
		/// </summary>
		/// <param name="index">The index of the string in the global strings array.</param>
		/// <returns>The stringID associated with the index.</returns>
		StringID IndexToStringID(int index);
	}
}