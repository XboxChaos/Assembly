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

namespace ExtryzeDLL.Blam.ThirdGen.Structures
{
    public class ThirdGenFileNameSource : IFileNameSource
    {
        private ThirdGenStringTable _strings;

        public ThirdGenFileNameSource(IReader reader, int count, int tableSize, Pointer indexTableLocation, Pointer dataLocation, BuildInformation buildInfo)
        {
            _strings = new ThirdGenStringTable(reader, count, tableSize, indexTableLocation, dataLocation, buildInfo.FileNameKey);
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
