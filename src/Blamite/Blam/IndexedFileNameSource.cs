using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam;
using Blamite.Blam.ThirdGen;
using Blamite.Blam.ThirdGen.Structures;
using Blamite.IO;
using Blamite.Flexibility;
using Blamite.Blam.Util;

namespace Blamite.Blam
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
            if (tagIndex >= 0 && tagIndex < _strings.Count)
                return _strings[tagIndex];
            return null;
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
