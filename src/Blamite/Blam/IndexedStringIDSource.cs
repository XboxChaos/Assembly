using System.Collections.Generic;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam
{
	/// <summary>
	///     A stringID table in a cache file which is made up of a string table and an offset table.
	/// </summary>
	public class IndexedStringIDSource : StringIDSource
	{
		private readonly IStringIDResolver _resolver;
		private readonly IndexedStringTable _strings;

		public IndexedStringIDSource(IndexedStringTable strings, IStringIDResolver resolver)
		{
			_strings = strings;
			_resolver = resolver;
		}

		public override int Count
		{
			get { return _strings.Count; }
		}

		public override StringIDLayout IDLayout
		{
			get { return _resolver.IDLayout; }
		}

		public override int StringIDToIndex(StringID id)
		{
			if (_resolver != null)
				return _resolver.StringIDToIndex(id);
			return -1;
		}

		public override StringID IndexToStringID(int index)
		{
			if (_resolver != null)
				return _resolver.IndexToStringID(index);
			return new StringID((uint) index);
		}

		public override string GetString(int index)
		{
			if (index >= 0 && index < _strings.Count)
				return _strings[index];
			return null;
		}

		public override int FindStringIndex(string str)
		{
			return _strings.IndexOf(str);
		}

		public override IEnumerator<string> GetEnumerator()
		{
			return _strings.GetEnumerator();
		}

		public override StringID AddString(string str)
		{
			_strings.Add(str);
			return IndexToStringID(_strings.Count - 1);
		}

		public override void SetString(int index, string str)
		{
			_strings[index] = str;
		}

		internal void SaveChanges(IStream stream)
		{
			_strings.SaveChanges(stream);
		}
	}
}