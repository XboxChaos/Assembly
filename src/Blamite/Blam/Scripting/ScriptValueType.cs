using System.Collections.Generic;

namespace Blamite.Blam.Scripting
{
	public class ScriptValueType
	{
		private readonly List<string> _enumValues = new List<string>();
		private readonly string _name;
		private readonly ushort _opcode;
		private readonly bool _quoted;
		private readonly int _size;
		private readonly string _tag;
		private readonly bool _object;

		public ScriptValueType(string name, ushort opcode, int size, bool quoted, string tag, bool obj)
		{
			_name = name;
			_opcode = opcode;
			_size = size;
			_quoted = quoted;
			_tag = tag;
			_object = obj;
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

		public string TagGroup
		{
			get { return _tag; }
		}

		public bool IsObject
		{
			get { return _object; }
		}

		public void AddEnumValue(string name)
		{
			_enumValues.Add(name);
		}

		public string GetEnumValue(uint value)
		{
			if (value < (uint) _enumValues.Count)
				return _enumValues[(int) value];
			return null;
		}

        public int GetEnumIndex(string name)
        {
            return _enumValues.FindIndex(e => e == name);
        }

		public bool IsEnum { get { return _enumValues.Count > 0; } }
	}
}