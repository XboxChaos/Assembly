using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.IO;

namespace Blamite.Blam.Scripting
{
    public class StringTable
    {
        private Dictionary<string, uint> _strings = new Dictionary<string, uint>();
        private uint _eof = 0;

        public uint Cache(string str)
        {
            uint result;
            if (_strings.TryGetValue(str, out result))
                return result;
            else
            {
                result = _eof;
                _strings.Add(str, result);
                _eof += (uint)(Encoding.ASCII.GetByteCount(str) + 1);
                return result;
            }
        }

        public string GetString(uint offset)
        {
            return _strings.Single(s => s.Value == offset).Key;
        }

        public int Size
        {
            get { return (int)_eof; }
        }

        public void Write(IWriter writer)
        {
            _strings.OrderBy(str => str.Value);
            foreach (var str in _strings)
                writer.WriteAscii(str.Key);
        }
    }
}
