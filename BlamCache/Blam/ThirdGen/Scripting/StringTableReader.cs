using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam.ThirdGen.Scripting
{
    public class StringTableReader
    {
        private SortedSet<int> _requestedStrings = new SortedSet<int>();

        public void RequestString(int offset)
        {
            _requestedStrings.Add(offset);
        }

        public void ReadRequestedStrings(IReader reader, Pointer baseLocation, CachedStringTable output)
        {
            uint baseOffset = baseLocation.AsOffset();

            int lastEnd = -1;
            foreach (int offset in _requestedStrings)
            {
                if (offset <= lastEnd)
                    continue;
                reader.SeekTo(baseOffset + offset);
                string str = reader.ReadAscii();
                output.CacheString(offset, str);
                lastEnd = offset + str.Length;
            }
        }
    }
}
