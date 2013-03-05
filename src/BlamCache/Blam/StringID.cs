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
        private byte _length;
        private byte _set;
        private ushort _index;

        /// <summary>
        /// Constructs a new StringID from a set and an index.
        /// </summary>
        /// <param name="set">The set the stringID belongs to.</param>
        /// <param name="index">The index of the stringID within the set.</param>
        public StringID(byte set, ushort index)
        {
            _length = 0;
            _set = set;
            _index = index;
        }

        /// <summary>
        /// Constructs a new StringID from a length, a set, and an index. (Pre-third-gen games only.)
        /// </summary>
        /// <param name="length">The length of the string.</param>
        /// <param name="set">The set the stringID belongs to.</param>
        /// <param name="index">The index of the stringID within the set.</param>
        public StringID(byte length, byte set, ushort index)
        {
            _length = length;
            _set = set;
            _index = index;
        }

        /// <summary>
        /// Constructs a new StringID from a value.
        /// </summary>
        /// <param name="value">The 32-bit value of the stringID.</param>
        public StringID(int value)
        {
            _length = (byte)(value >> 24);
            _set = (byte)(value >> 16);
            _index = (ushort)value;
        }

        /// <summary>
        /// The length of the string that the stringID points to. (Pre-third-gen games only.)
        /// </summary>
        public byte Length
        {
            get { return _length; }
        }

        /// <summary>
        /// The set that the stringID belongs to.
        /// </summary>
        public byte Set
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
            get { return (_length << 24) | (_set << 16) | _index; }
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

        /// <summary>
        /// A null stringID.
        /// </summary>
        public static readonly StringID Null = new StringID(0);
    }
}
