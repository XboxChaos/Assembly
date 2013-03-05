using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.Scripting
{
    public class ScriptValueType
    {
        private string _name;
        private ushort _opcode;
        private int _size;
        private bool _quoted;
        private List<string> _enumValues = new List<string>();

        public ScriptValueType(string name, ushort opcode, int size, bool quoted)
        {
            _name = name;
            _opcode = opcode;
            _size = size;
            _quoted = quoted;
        }

        public void AddEnumValue(string name)
        {
            _enumValues.Add(name);
        }

        public string GetEnumValue(uint value)
        {
            if (value < (uint)_enumValues.Count)
                return _enumValues[(int)value];
            return null;
        }

        public string Name
        {
            get { return _name; }
        }

        public ushort Opcode
        {
            get { return _opcode; }
        }

        public int Size
        {
            get { return _size; }
        }

        public bool Quoted
        {
            get { return _quoted; }
        }
    }
}
