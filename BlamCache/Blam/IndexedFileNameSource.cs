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

namespace ExtryzeDLL.Blam.ThirdGen
{
    /// <summary>
    /// A file name table in a cache file which is made up of a string table and an offset table,
    /// and which runs parallel to the tag table.
    /// </summary>
    public class IndexedFileNameSource : IFileNameSource
    {
        private IndexedStringTable _strings;

        public IndexedFileNameSource(IndexedStringTable strings)
        {
            _strings = strings;
        }

        public string FindTagName(DatumIndex tagIndex)
        {
            if (!tagIndex.IsValid || tagIndex.Index >= _strings.Strings.Count)
                return null;
            return _strings.Strings[tagIndex.Index];
        }

        public string FindTagName(ITag tag)
        {
            return FindTagName(tag.Index);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _strings.Strings.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _strings.Strings.GetEnumerator();
        }
    }
}
