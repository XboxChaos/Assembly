using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Blam.ThirdGen.Scripting
{
    public class CachedStringTable
    {
        private Dictionary<int, string> _strings = new Dictionary<int, string>();

        public void CacheString(int offset, string str)
        {
            _strings[offset] = str;
        }

        public string GetString(int offset)
        {
            string result;
            if (_strings.TryGetValue(offset, out result))
                return result;
            return null;
        }
    }
}
