using System;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	/// <summary>
	///     Base class for number data.
	/// </summary>
	/// <typeparam name="T">The type of number to hold.</typeparam>
	public abstract class NumberData<T> : ValueField
	{
		private string _type;
		private T _value;

		public NumberData(string name, uint offset, long address, string type, T value, uint pluginLine, string tooltip)
			: base(name, offset, address, pluginLine, tooltip)
		{
			_type = type;
			_value = value;
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

		public override string AsString()
		{
			return string.Format("{0} | {1} | {2}", Type, Name, Value);
		}
	}

	/// <summary>
	///     Unsigned byte.
	/// </summary>
	public class Uint8Data : NumberData<byte>
	{
		public Uint8Data(string name, uint offset, long address, string type, byte value, uint pluginLine, string tooltip)
			: base(name, offset, address, type, value, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitUint8(this);
		}

		public override MetaField CloneValue()
		{
			return new Uint8Data(Name, Offset, FieldAddress, Type, Value, PluginLine, ToolTip);
		}
	}

	/// <summary>
	///     Signed byte.
	/// </summary>
	public class Int8Data : NumberData<sbyte>
	{
		public Int8Data(string name, uint offset, long address, string type, sbyte value, uint pluginLine, string tooltip)
			: base(name, offset, address, type, value, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitInt8(this);
		}

		public override MetaField CloneValue()
		{
			return new Int8Data(Name, Offset, FieldAddress, Type, Value, PluginLine, ToolTip);
		}
	}

	/// <summary>
	///     Unsigned 16-bit integer.
	/// </summary>
	public class Uint16Data : NumberData<ushort>
	{
		public Uint16Data(string name, uint offset, long address, string type, ushort value, uint pluginLine, string tooltip)
			: base(name, offset, address, type, value, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitUint16(this);
		}

		public override MetaField CloneValue()
		{
			return new Uint16Data(Name, Offset, FieldAddress, Type, Value, PluginLine, ToolTip);
		}
	}

	/// <summary>
	///     Signed 16-bit integer.
	/// </summary>
	public class Int16Data : NumberData<short>
	{
		public Int16Data(string name, uint offset, long address, string type, short value, uint pluginLine, string tooltip)
			: base(name, offset, address, type, value, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitInt16(this);
		}

		public override MetaField CloneValue()
		{
			return new Int16Data(Name, Offset, FieldAddress, Type, Value, PluginLine, ToolTip);
		}
	}

	/// <summary>
	///     Unsigned 32-bit integer.
	/// </summary>
	public class Uint32Data : NumberData<uint>
	{
		public Uint32Data(string name, uint offset, long address, string type, uint value, uint pluginLine, string tooltip)
			: base(name, offset, address, type, value, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitUint32(this);
		}

		public override MetaField CloneValue()
		{
			return new Uint32Data(Name, Offset, FieldAddress, Type, Value, PluginLine, ToolTip);
		}
	}

	/// <summary>
	///     Signed 32-bit integer.
	/// </summary>
	public class Int32Data : NumberData<int>
	{
		public Int32Data(string name, uint offset, long address, string type, int value, uint pluginLine, string tooltip)
			: base(name, offset, address, type, value, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitInt32(this);
		}

		public override MetaField CloneValue()
		{
			return new Int32Data(Name, Offset, FieldAddress, Type, Value, PluginLine, ToolTip);
		}
	}

	/// <summary>
	///     Unsigned 64-bit integer.
	/// </summary>
	public class Uint64Data : NumberData<ulong>
	{
		public Uint64Data(string name, uint offset, long address, string type, ulong value, uint pluginLine, string tooltip)
			: base(name, offset, address, type, value, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitUint64(this);
		}

		public override MetaField CloneValue()
		{
			return new Uint64Data(Name, Offset, FieldAddress, Type, Value, PluginLine, ToolTip);
		}
	}

	/// <summary>
	///     Signed 64-bit integer.
	/// </summary>
	public class Int64Data : NumberData<long>
	{
		public Int64Data(string name, uint offset, long address, string type, long value, uint pluginLine, string tooltip)
			: base(name, offset, address, type, value, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitInt64(this);
		}

		public override MetaField CloneValue()
		{
			return new Int64Data(Name, Offset, FieldAddress, Type, Value, PluginLine, ToolTip);
		}
	}

	/// <summary>
	///     32-bit floating-point number.
	/// </summary>
	public class Float32Data : NumberData<float>
	{
		public Float32Data(string name, uint offset, long address, string type, float value, uint pluginLine, string tooltip)
			: base(name, offset, address, type, value, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitFloat32(this);
		}

		public override MetaField CloneValue()
		{
			return new Float32Data(Name, Offset, FieldAddress, Type, Value, PluginLine, ToolTip);
		}
	}

	/// <summary>
	///     32-bit floating-point number, converted from radians to degrees
	/// </summary>
	public class DegreeData : NumberData<float>
	{
		private float _radian;

		public DegreeData(string name, uint offset, long address, string type, float radian, uint pluginLine, string tooltip)
			: base(name, offset, address, type, radian, pluginLine, tooltip)
		{
			_radian = radian;
		}

		public new float Value
		{
			get { return FromRadian(_radian); }
			set
			{
				_radian = ToRadian(value);
				NotifyPropertyChanged("Value");
			}
		}

		public float Radian
		{
			get { return _radian; }
			set
			{
				_radian = value;
				NotifyPropertyChanged("Radian");
				NotifyPropertyChanged("Value");
			}
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitDegree(this);
		}

		public override MetaField CloneValue()
		{
			return new DegreeData(Name, Offset, FieldAddress, Type, _radian, PluginLine, ToolTip);
		}
	}
}