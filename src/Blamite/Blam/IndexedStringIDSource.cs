using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.ThirdGen;
using Blamite.Blam.ThirdGen.Structures;
using Blamite.IO;
using Blamite.Flexibility;
using Blamite.Blam.Util;
using Blamite.Util;
using System.IO;

namespace Blamite.Blam
{
    /// <summary>
    /// A stringID table in a cache file which is made up of a string table and an offset table.
    /// </summary>
    public class IndexedStringIDSource : StringIDSource
    {
        private IndexedStringTable _strings;
        private IStringIDResolver _resolver;

        public IndexedStringIDSource(IndexedStringTable strings, IStringIDResolver resolver)
        {
            _strings = strings;
            _resolver = resolver;
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
            return new StringID(index);
        }

        public override string GetString(int index)
        {
            return _strings[index];
        }

        public override int FindStringIndex(string str)
        {
            return _strings.IndexOf(str);
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return _strings.GetEnumerator();
        }
    }
}
