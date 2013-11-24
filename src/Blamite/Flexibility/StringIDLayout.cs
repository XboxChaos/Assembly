namespace Blamite.Flexibility
{
	/// <summary>
	///     Defines the sizes of the three values in a stringID.
	/// </summary>
	public class StringIDLayout
	{
		public StringIDLayout(int indexSize, int setSize, int lengthSize)
		{
			IndexSize = indexSize;
			SetSize = setSize;
			LengthSize = lengthSize;

			IndexStart = 0;
			SetStart = IndexStart + IndexSize;
			LengthStart = SetStart + SetSize;
		}

		public StringIDLayout(int indexStart, int indexSize, int setStart, int setSize, int lengthStart, int lengthSize)
		{
			IndexStart = indexStart;
			IndexSize = indexSize;
			SetStart = setStart;
			SetSize = setSize;
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
		public int SetStart { get; private set; }

		/// <summary>
		///     Gets the size in bits of the set portion of the stringID. Can be zero.
		/// </summary>
		public int SetSize { get; private set; }

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