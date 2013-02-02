using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Blam.Util
{
    public struct Pointer
    {
        private uint _value;
        private PointerConverter _converter;

        public Pointer(uint value, PointerConverter converter)
        {
            _value = value;
            _converter = converter;
        }

        public Pointer(uint value, Pointer template)
        {
            _value = value;
            _converter = template._converter;
        }

        /// <summary>
        /// Gets whether or not the pointer is a null pointer.
        /// </summary>
        public bool IsNull
        {
            get { return (_value == 0 || _converter == null); }
        }

        /// <summary>
        /// Gets whether or not the pointer has a file offset associated with it.
        /// </summary>
        public bool HasOffset
        {
            get { return (_converter == null || _converter.SupportsOffsets); }
        }

        /// <summary>
        /// The pointer as a file offset.
        /// </summary>
        /// <returns>The pointer's equivalent file offset.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the pointer does not have a file offset associated with it.</exception>
        public uint AsOffset()
        {
            if (_converter != null && !_converter.SupportsOffsets)
                throw new InvalidOperationException("The pointer does not have an offset associated with it.");

            if (_converter == null)
                return 0;

            return _converter.PointerToOffset(_value);
        }

        /// <summary>
        /// Gets whether or not the pointer has a memory address associated with it.
        /// </summary>
        public bool HasAddress
        {
            get { return (_converter == null || _converter.SupportsAddresses); }
        }

        /// <summary>
        /// The pointer as a memory address.
        /// </summary>
        /// <returns>The pointer's equivalent memory address.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the pointer does not have a memory address associated with it.</exception>
        public uint AsAddress()
        {
            if (_converter != null && !_converter.SupportsAddresses)
                throw new InvalidOperationException("The pointer does not have a memory address associated with it.");

            if (_converter == null)
                return 0;

            return _converter.PointerToAddress(_value);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Pointer))
                return false;
            return AsOffset() == ((Pointer)obj).AsOffset();
        }

        public bool Equals(Pointer obj)
        {
            return (AsOffset() == obj.AsOffset());
        }

        public override int GetHashCode()
        {
            return (int)_value;
        }

        public static bool operator ==(Pointer p1, Pointer p2)
        {
            return p1.Equals(p2);
        }

        public static bool operator !=(Pointer p1, Pointer p2)
        {
            return !p1.Equals(p2);
        }

        public static Pointer operator +(Pointer p, int i)
        {
            return new Pointer((uint)(p._value + i), p._converter);
        }

        public static Pointer operator -(Pointer p, int i)
        {
            return new Pointer((uint)(p._value - i), p._converter);
        }
    }
}
