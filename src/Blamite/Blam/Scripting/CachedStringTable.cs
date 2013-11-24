using System.Collections.Generic;

namespace Blamite.Blam.Scripting
{
	/// <summary>
	///     A cached table of strings, where strings can be retrieved by their offset in the table.
	/// </summary>
	public class CachedStringTable
	{
		private readonly Dictionary<int, string> _strings = new Dictionary<int, string>();

		/// <summary>
		///     Caches a string in the table.
		/// </summary>
		/// <param name="offset">The string's offset in the table.</param>
		/// <param name="str">The string.</param>
		public void CacheString(int offset, string str)
		{
			_strings[offset] = str;
		}

		/// <summary>
		///     Gets the string at an offset in the table.
		/// </summary>
		/// <param name="offset">The offset of the string to retrieve.</param>
		/// <returns>The string if it has been cached, or null otherwise.</returns>
		public string GetString(int offset)
		{
			string result;
			if (_strings.TryGetValue(offset, out result))
				return result;
			return null;
		}
	}
}