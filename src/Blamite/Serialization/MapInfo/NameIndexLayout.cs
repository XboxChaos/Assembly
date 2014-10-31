namespace Blamite.Serialization.MapInfo
{
	/// <summary>
	///     Defines max team information.
	/// </summary>
	public class NameIndexLayout
	{
		public NameIndexLayout(int index, string name)
		{
			Index = index;
			Name = name;
		}

		/// <summary>
		///     Gets the zero-based index.
		/// </summary>
		public int Index { get; private set; }

		/// <summary>
		///     Gets the friendly name.
		/// </summary>
		public string Name { get; private set; }
	}
}