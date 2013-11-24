namespace Blamite.Blam.LanguagePack
{
	/// <summary>
	///     A localized string, composed of a stringID key and a unicode string value.
	/// </summary>
	public class LocalizedString
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="LocalizedString" /> class from a key and a value.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		public LocalizedString(StringID key, string value)
		{
			Key = key;
			Value = value;
		}

		/// <summary>
		///     Gets or sets the string's key.
		/// </summary>
		public StringID Key { get; set; }

		/// <summary>
		///     Gets or sets the string's value.
		/// </summary>
		public string Value { get; set; }
	}
}