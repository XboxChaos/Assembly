using System;
using System.Collections.Generic;
using Blamite.IO;

namespace Blamite.Blam.Scripting
{
    /// <summary>
    /// An interface which represents script expression values.
    /// </summary>
    public interface IExpressionValue
    {
        /// <summary>
        /// The Value of the expression, represented as uint.
        /// </summary>
        public uint UintValue { get; }

        /// <summary>
        /// Is true if the value is null.
        /// </summary>
        public bool IsNull { get; }

        /// <summary>
        /// Writes the expression value to a stream.
        /// </summary>
        /// <param name="writer">The steam to write to.</param>
        public void Write(IWriter writer);

        /// <summary>
        /// Converts the value of this instance to a String.
        /// </summary>
        /// <returns></returns>
        public string ToString();
    }

    /// <summary>
    /// Compares <see cref="IExpressionValue"/> objects for equality.
    /// </summary>
    public class ExpressionValueComparer : IEqualityComparer<IExpressionValue>
    {
        public bool Equals(IExpressionValue x, IExpressionValue y)
        {
            return x.UintValue == y.UintValue;
        }

        public int GetHashCode(IExpressionValue obj)
        {
            return obj.GetHashCode();
        }
    }


    /// <summary>
    /// A long, 32 Bit, expression value.
    /// </summary>
    public class LongExpressionValue : IExpressionValue
    {
        private uint value;

        public LongExpressionValue(uint value)
        {
            this.value = value;
        }

        public LongExpressionValue(ushort value1, ushort value2)
        {
            value = (uint)(value1 << 16) | value2;
        }

        public LongExpressionValue(byte value1, byte value2, byte value3, byte value4)
        {
            uint upper = (uint)(value1 << 24 | value2 << 16);
            uint lower = (uint)(value3 << 8 | value4);
            value = upper | lower;
        }

        public LongExpressionValue(StringID sid)
        {
            value = sid.Value;
        }

        public LongExpressionValue(DatumIndex index)
        {
            value = index.Value;
        }

        public LongExpressionValue(ITag tag)
        {
            value = tag.Index.Value;
        }

        public void Write(IWriter writer)
        {
            writer.WriteUInt32(value);
        }

        public uint UintValue { get { return value; } }

        public bool IsNull { get { return value == 0xFFFFFFFF; } }

        public override string ToString()
        {
            return value.ToString("X8");
        }
    }


    /// <summary>
    /// A short, 16 Bit, expression value.
    /// </summary>
    public class ShortValue : IExpressionValue
    {
        private ushort value;

        public ShortValue(ushort value1)
        {
            this.value = value1;
        }

        public void Write(IWriter writer)
        {
            writer.WriteUInt16(value);
            writer.WriteUInt16(0xFFFF);
        }

        public uint UintValue { get { return (uint)(value << 16) | 0xFFFF; } }

        public bool IsNull { get { return value == 0xFFFF; } }

        public override string ToString()
        {
            return value.ToString("X4") + "FFFF";
        }
    }


    /// <summary>
    /// A byte, 8 Bit, expression value.
    /// </summary>
    public class ByteValue : IExpressionValue
    {
        private byte value;

        public ByteValue(byte value)
        {
            this.value = value;
        }

        public void Write(IWriter writer)
        {
            writer.WriteByte(value);
            writer.WriteBlock(new byte[] { 0xFF, 0xFF, 0xFF });
        }

        public uint UintValue { get { return (uint)(value << 24) | 0xFFFFFF; } }

        public bool IsNull { get { return value == 0xFF; } }

        public override string ToString()
        {
            return value.ToString("X2") + "FFFFFF";
        }
    }


    /// <summary>
    /// A floating point, 32 Bit, expression value.
    /// </summary>
    public class FloatValue : IExpressionValue
    {
        private float value;

        public FloatValue(float value)
        {
            this.value = value;
        }

        public void Write(IWriter writer)
        {
            writer.WriteFloat(value);
        }

        public uint UintValue { 
            get
            {
                return BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
            } 
        }

        public bool IsNull { get { return float.IsNaN(UintValue); } }

        public override string ToString()
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToUInt32(bytes, 0).ToString("X8");
        }
    }
}
