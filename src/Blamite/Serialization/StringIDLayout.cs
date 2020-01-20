namespace Blamite.Serialization
{
	/// <summary>
	///     Defines the sizes of the three values in a stringID.
	/// </summary>
	public class StringIDLayout
	{
		public StringIDLayout(int indexSize, int namespaceSize, int lengthSize)
		{
			IndexSize = indexSize;
			NamespaceSize = namespaceSize;
			LengthSize = lengthSize;

			IndexStart = 0;
			NamespaceStart = IndexStart + IndexSize;
			LengthStart = NamespaceStart + NamespaceSize;
		}

		public StringIDLayout(int indexStart, int indexSize, int namespaceStart, int namespaceSize, int lengthStart, int lengthSize)
		{
			IndexStart = indexStart;
			IndexSize = indexSize;
			NamespaceStart = namespaceStart;
			NamespaceSize = namespaceSize;
			LengthStart = lengthStart;
			LengthSize = lengthSize;
		}

		/// <summary>
		///     Gets the starting bit (0 = LSB) of the index portion of the stringID.
		/// </summary>
		public int IndexStart { get; private set; }

		/// <summary>
		///     Gets the size in bits of the index portion of the stringID.
		/// </summary>
		public int IndexSize { get; private set; }

		/// <summary>
		///     Gets the starting bit (0 = LSB) of the set portion of the stringID.
		/// </summary>
		public int NamespaceStart { get; private set; }

		/// <summary>
		///     Gets the size in bits of the set portion of the stringID. Can be zero.
		/// </summary>
		public int NamespaceSize { get; private set; }

		/// <summary>
		///     Gets the starting bit (0 = LSB) of the length portion of the stringID.
		/// </summary>
		public int LengthStart { get; private set; }

		/// <summary>
		///     Gets the size in bits of the length portion of the stringID. Can be zero.
		/// </summary>
		public int LengthSize { get; private set; }
	}
}