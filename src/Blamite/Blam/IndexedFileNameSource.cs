using System.Collections.Generic;
using Blamite.IO;

namespace Blamite.Blam
{
	/// <summary>
	///     A file name table in a cache file which is made up of a string table and an offset table,
	///     and which runs parallel to the tag table.
	/// </summary>
	public class IndexedFileNameSource : FileNameSource
	{
		private readonly IndexedStringTable _strings;

		public IndexedFileNameSource(IndexedStringTable strings)
		{
			_strings = strings;
		}

		public int Count
		{
			get { return _strings.Count; }
		}

		public override string GetTagName(int tagIndex)
		{
			if (tagIndex >= 0 && tagIndex < _strings.Count)
				return _strings[tagIndex];
			return null;
		}

		public override void SetTagName(int tagIndex, string name)
		{
			_strings.Expand(tagIndex + 1);
			_strings[tagIndex] = name;
		}

		public override int FindName(string name)
		{
			return _strings.IndexOf(name);
		}

		public override IEnumerator<string> GetEnumerator()
		{
			return _strings.GetEnumerator();
		}

		internal void SaveChanges(IStream stream)
		{
			_strings.SaveChanges(stream);
		}
	}
}