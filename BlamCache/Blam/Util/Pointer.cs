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

        public uint AsOffset()
        {
            return _converter.PointerToOffset(_value);
        }

        public uint AsAddress()
        {
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
