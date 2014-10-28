using Assembly.Helpers.TagEditor.Buffering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.TagEditor.Fields
{
	/// <summary>
	/// Base class for a field representing numeric data.
	/// </summary>
	public abstract class NumericField<T> : ValueTagField
	{
		private string _type;
		private T _value;

		public NumericField(string name, uint offset, uint address, string type, uint pluginLine, TagBufferSource source)
			: base(name, offset, address, pluginLine, source)
		{
			_type = type;
		}

		public string Type
		{
			get { return _type; }
			set
			{
				_type = value;
				NotifyPropertyChanged("Type");
			}
		}

		public T Value
		{
			get { return _value; }
			set
			{
				_value = value;
				NotifyPropertyChanged("Value");
			}
		}

		/// <summary>
		/// Accepts the specified visitor.
		/// </summary>
		/// <param name="visitor">The visitor.</param>
		public abstract override void Accept(ITagFieldVisitor visitor);
	}

	/// <summary>
	/// Unsigned byte.
	/// </summary>
	public class UInt8Field : NumericField<byte>
	{
		public UInt8Field(string name, uint offset, uint address, string type, uint pluginLine, TagBufferSource source)
			: base(name, offset, address, type, pluginLine, source)
		{
		}

		public override void Accept(ITagFieldVisitor visitor)
		{
			visitor.VisitUInt8(this);
		}
	}

	/// <summary>
	/// Signed byte.
	/// </summary>
	public class Int8Field : NumericField<sbyte>
	{
		public Int8Field(string name, uint offset, uint address, string type, uint pluginLine, TagBufferSource source)
			: base(name, offset, address, type, pluginLine, source)
		{
		}

		public override void Accept(ITagFieldVisitor visitor)
		{
			visitor.VisitInt8(this);
		}
	}

	/// <summary>
	/// Unsigned 16-bit integer.
	/// </summary>
	public class UInt16Field : NumericField<ushort>
	{
		public UInt16Field(string name, uint offset, uint address, string type, uint pluginLine, TagBufferSource source)
			: base(name, offset, address, type, pluginLine, source)
		{
		}

		public override void Accept(ITagFieldVisitor visitor)
		{
			visitor.VisitUInt16(this);
		}
	}

	/// <summary>
	/// Signed 16-bit integer.
	/// </summary>
	public class Int16Field : NumericField<short>
	{
		public Int16Field(string name, uint offset, uint address, string type, uint pluginLine, TagBufferSource source)
			: base(name, offset, address, type, pluginLine, source)
		{
		}

		public override void Accept(ITagFieldVisitor visitor)
		{
			visitor.VisitInt16(this);
		}
	}

	/// <summary>
	/// Unsigned 32-bit integer.
	/// </summary>
	public class UInt32Field : NumericField<uint>
	{
		public UInt32Field(string name, uint offset, uint address, string type, uint pluginLine, TagBufferSource source)
			: base(name, offset, address, type, pluginLine, source)
		{
		}

		public override void Accept(ITagFieldVisitor visitor)
		{
			visitor.VisitUInt32(this);
		}
	}

	/// <summary>
	/// Signed 32-bit integer.
	/// </summary>
	public class Int32Field : NumericField<int>
	{
		public Int32Field(string name, uint offset, uint address, string type, uint pluginLine, TagBufferSource source)
			: base(name, offset, address, type, pluginLine, source)
		{
		}

		public override void Accept(ITagFieldVisitor visitor)
		{
			visitor.VisitInt32(this);
		}
	}

	/// <summary>
	/// 32-bit floating-point number.
	/// </summary>
	public class Float32Field : NumericField<float>
	{
		public Float32Field(string name, uint offset, uint address, string type, uint pluginLine, TagBufferSource source)
			: base(name, offset, address, type, pluginLine, source)
		{
		}

		public override void Accept(ITagFieldVisitor visitor)
		{
			visitor.VisitFloat32(this);
		}
	}
}
