using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Blam
{
    /// <summary>
    /// A constant ID representing a debug string.
    /// </summary>
    public struct StringID
    {
        private short _set;
        private ushort _index;

        /// <summary>
        /// Constructs a new StringID from a set and an index.
        /// </summary>
        /// <param name="set">The set the stringID belongs to.</param>
        /// <param name="index">The index of the stringID within the set.</param>
        public StringID(short set, ushort index)
        {
            _set = set;
            _index = index;
        }

        /// <summary>
        /// Constructs a new StringID from a value.
        /// </summary>
        /// <param name="value">The 32-bit value of the stringID.</param>
        public StringID(int value)
        {
            _set = (short)(value >> 16);
            _index = (ushort)(value & 0xFFFF);
        }

        /// <summary>
        /// The set that the stringID belongs to.
        /// </summary>
        public short Set
        {
            get { return _set; }
        }

        /// <summary>
        /// The index of the string within the set.
        /// </summary>
        public ushort Index
        {
            get { return _index; }
        }

        /// <summary>
        /// The value of the stringID as a 32-bit integer.
        /// </summary>
        public int Value
        {
            get { return (Set << 16) | Index; }
        }

        public override bool Equals(object obj)
        {
            return (obj is StringID) && (this == (StringID)obj);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(StringID x, StringID y)
        {
            return (x.Value == y.Value);
        }

        public static bool operator !=(StringID x, StringID y)
        {
            return !(x == y);
        }

        public override string ToString()
        {
            return "0x" + Value.ToString("X8");
        }
    }
}
