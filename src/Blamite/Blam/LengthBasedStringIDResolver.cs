using Blamite.Flexibility;

namespace Blamite.Blam
{
	/// <summary>
	///     A StringIDResolver which accepts and creates stringIDs with length information set.
	/// </summary>
	public class LengthBasedStringIDResolver : IStringIDResolver
	{
		private readonly IndexedStringTable _strings;

		/// <summary>
		///     Constructs a new LengthBasedStringIDResolver.
		/// </summary>
		/// <param name="strings">The IndexedStringTable to reference to get string lengths.</param>
		public LengthBasedStringIDResolver(IndexedStringTable strings)
		{
			_strings = strings;
			IDLayout = new StringIDLayout(24, 0, 8); // TODO: is it necessary to make this a build option?
		}

		/// <summary>
		///     Translates a stringID into an index into the global debug strings array.
		/// </summary>
		/// <param name="id">The StringID to translate.</param>
		/// <returns>The index of the string in the global debug strings array.</returns>
		public int StringIDToIndex(StringID id)
		{
			return id.GetIndex(IDLayout);
		}

		/// <summary>
		///     Translates a string index into a stringID which can be written to the file.
		/// </summary>
		/// <param name="index">The index of the string in the global strings array.</param>
		/// <returns>The stringID associated with the index.</returns>
		public StringID IndexToStringID(int index)
		{
			if (index < 0 || index >= _strings.Count)
				return StringID.Null;

			string str = _strings[index];
			return new StringID(str.Length, 0, index, IDLayout);
		}

		/// <summary>
		///     Gets the layout of stringIDs.
		/// </summary>
		public StringIDLayout IDLayout { get; private set; }
	}
}