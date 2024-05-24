using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Blamite.Blam
{
	/// <summary>
	///     An external list of tag names in csv format.
	/// </summary>
	class CSVFilenameSource : FileNameSource
	{
		private readonly SortedDictionary<int, string> _strings;
		private readonly string[] splitValues = new string[] { "," };
		public CSVFilenameSource(string localFileName, string fallbackFileName)
		{
			_strings = new SortedDictionary<int, string>();

			if (File.Exists(localFileName))
				Load(localFileName);
			else if (!string.IsNullOrEmpty(fallbackFileName) && File.Exists(fallbackFileName))
				Load(fallbackFileName);
		}

		/// <summary>
		///     Loads the strings into the table.
		/// </summary>
		private void Load(string fileName)
		{
			string[] lines = File.ReadAllLines(fileName);
			foreach (string s in lines)
			{
				if (string.IsNullOrEmpty(s))
					continue;
				int index;
				string[] split = s.Split(splitValues, StringSplitOptions.RemoveEmptyEntries);
				if (split[0].StartsWith("0x"))
					index = int.Parse(split[0].Substring(2), System.Globalization.NumberStyles.HexNumber);
				else
					index = int.Parse(split[0]);

				_strings[index] = split[1];
			}
		}

		/// <summary>
		///     Gets the number of strings in the table.
		/// </summary>
		public int Count
		{
			get { return _strings.Count; }
		}

		/// <summary>
		///     Gets or sets the string at an index.
		/// </summary>
		/// <param name="index">The index of the string to get or set.</param>
		/// <returns>The string at the given index.</returns>
		public string this[int index]
		{
			get { return _strings[index]; }
			set { _strings[index] = value; }
		}

		public override IEnumerator<string> GetEnumerator()
		{
			return null;
		}

		/// <summary>
		///     Adds a string to the table.
		/// </summary>
		/// <param name="str">The string to add.</param>
		public void Add(int index, string str)
		{
			_strings.Add(index, str);
		}

		/// <summary>
		///     Searches for a given string and returns the zero-based index of its first occurrence in the table. O(n).
		/// </summary>
		/// <param name="str">The string to search for. Case-sensitive.</param>
		/// <returns>The zero-based index of the first occurrence of the string in the table, or -1 if not found.</returns>
		public int IndexOf(string str)
		{
			if (_strings.ContainsValue(str))
				return _strings.FirstOrDefault(x => x.Value == str).Key;

			return -1;
		}

		/// <summary>
		///     Finds the index of the first tag which has a given name.
		/// </summary>
		/// <param name="name">The tag name to search for.</param>
		/// <returns>The index in the tag table of the first tag with the given name, or -1 if not found.</returns>
		public override int FindName(string name) { return IndexOf(name); }

		/// <summary>
		///     Given a tag, retrieves its filename if it exists.
		/// </summary>
		/// <param name="tag">The tag to get the filename of.</param>
		/// <returns>The tag's name if available, or null otherwise.</returns>
		public override string GetTagName(int tagIndex)
		{
			if (_strings.ContainsKey(tagIndex))
				return _strings[tagIndex];

			return null;
		}

		/// <summary>
		///     Sets the name of a tag based upon its index in the tag table.
		/// </summary>
		/// <param name="tagIndex">The index of the tag to set the name of.</param>
		/// <param name="name">The new name.</param>
		public override void SetTagName(int tagIndex, string name) { _strings[tagIndex] = name; }

		/// <summary>
		///     Saves changes made to the string table.
		/// </summary>
		/// <param name="stream">The stream to manipulate.</param>
		public void SaveChanges(string fileName)
		{
			SaveData(fileName);
		}

		private void SaveData(string fileName)
		{
			using (StringWriter sw = new StringWriter())
			{
				foreach (KeyValuePair<int, string> key in _strings)
				{
					if (string.IsNullOrEmpty(key.Value))
						continue;

					sw.WriteLine($"0x{key.Key:X8},{key.Value}");
				}

				File.WriteAllText(fileName, sw.ToString());
			}
		}
	}
}
