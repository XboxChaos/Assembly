using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.IO;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.Blam.Util;

namespace ExtryzeDLL.Blam
{
    /// <summary>
    /// A file name table in a cache file which is made up of a string table and an offset table,
    /// and which runs parallel to the tag table.
    /// </summary>
    public class IndexedFileNameSource : FileNameSource
    {
        private IndexedStringTable _strings;

        public IndexedFileNameSource(IndexedStringTable strings)
        {
            _strings = strings;
        }

        public override string GetTagName(int tagIndex)
        {
            if (tagIndex >= _strings.Count)
                return null;
            return _strings[tagIndex];
        }

        public override int FindName(string name)
        {
            return _strings.IndexOf(name);
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return _strings.GetEnumerator();
        }
    }
}
