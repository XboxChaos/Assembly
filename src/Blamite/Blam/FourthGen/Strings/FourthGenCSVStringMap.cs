using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using Blamite.Blam.Util;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam
{
    /// <summary>
    ///     A table of strings associated with a table of string offsets.
    /// </summary>
    public class FourthGenCSVStringMap : FileNameSource
    {
        private readonly Dictionary<int, string> _strings = new Dictionary<int, string>();
        private readonly string[] splitValues = new string[] { ",", "\n","\r" };
        private readonly string[] splitValues2 = new string[] { "x" };
        public FourthGenCSVStringMap(IReader reader)
        {
            Load(reader);
        }

        /// <summary>
        ///     Loads the strings into the StringTable
        /// </summary>
        private void Load(IReader reader)
        {
            reader.SeekTo(0);

            string csv = reader.ReadAscii();
            string[] strings = csv.Split(splitValues, StringSplitOptions.RemoveEmptyEntries);

            int count = strings.Length;
            for(int i=0;i<count;i+=2)
            {
                string[] s = strings[i].Split(splitValues2, StringSplitOptions.RemoveEmptyEntries);
                int index = Int32.Parse(s[s.Length-1], System.Globalization.NumberStyles.HexNumber);
                _strings.Add(index, strings[i + 1]);
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
            return _strings.Where(entry => entry.Value == str)
                    .Select(entry => entry.Key)
                    .FirstOrDefault();
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
            string str = _strings.Where(entry => entry.Key == tagIndex)
                    .Select(entry => entry.Value)
                    .FirstOrDefault();
            string index_str = string.Format("0x{0:X8}", tagIndex);
            if (str != null) return str;
            return index_str;
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
        public void SaveChanges(IStream stream)
        {
            SaveData(stream);
        }

        private void SaveData(IStream stream)
        {
            stream.BaseStream.SetLength(0);
            stream.SeekTo(0);
            foreach (KeyValuePair<int, string> key in _strings)
            {
                // Write the entries
                string s = string.Format("0x{0:X8}", key.Key);

                if (key.Value != "")
                {
                    s += ",";
                    s += key.Value;
                    s += "\n";
                    stream.BaseStream.Write(Encoding.ASCII.GetBytes(s), 0, s.Length);
                }
            }
        }
    }
}