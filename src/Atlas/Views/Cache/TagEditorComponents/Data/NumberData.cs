using Atlas.Views.Cache.TagEditorComponents.Data;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	/// <summary>
	///     Base class for number data.
	/// </summary>
	/// <typeparam name="T">The type of number to hold.</typeparam>
	public abstract class NumberData<T> : ValueField
	{
		private string _type;
		private T _value;

		public NumberData(string name, uint offset, uint address, string type, T value, uint pluginLine)
			: base(name, offset, address, pluginLine)
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
				OnPropertyChanged("Type");
			}
		}

		public T Value
		{
			get { return _value; }
			set
			{
				_value = value;
				OnPropertyChanged("Value");
			}
		}
	}

	/// <summary>
	///     Unsigned byte.
	/// </summary>
	public class Uint8Data : NumberData<byte>
	{
		public Uint8Data(string name, uint offset, uint address, string type, byte value, uint pluginLine)
			: base(name, offset, address, type, value, pluginLine)
		{
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitUint8(this);
		}

		public override TagDataField CloneValue()
		{
			return new Uint8Data(Name, Offset, FieldAddress, Type, Value, base.PluginLine);
		}
	}

	/// <summary>
	///     Signed byte.
	/// </summary>
	public class Int8Data : NumberData<sbyte>
	{
		public Int8Data(string name, uint offset, uint address, string type, sbyte value, uint pluginLine)
			: base(name, offset, address, type, value, pluginLine)
		{
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitInt8(this);
		}

		public override TagDataField CloneValue()
		{
			return new Int8Data(Name, Offset, FieldAddress, Type, Value, base.PluginLine);
		}
	}

	/// <summary>
	///     Unsigned 16-bit integer.
	/// </summary>
	public class Uint16Data : NumberData<ushort>
	{
		public Uint16Data(string name, uint offset, uint address, string type, ushort value, uint pluginLine)
			: base(name, offset, address, type, value, pluginLine)
		{
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitUint16(this);
		}

		public override TagDataField CloneValue()
		{
			return new Uint16Data(Name, Offset, FieldAddress, Type, Value, base.PluginLine);
		}
	}

	/// <summary>
	///     Signed 16-bit integer.
	/// </summary>
	public class Int16Data : NumberData<short>
	{
		public Int16Data(string name, uint offset, uint address, string type, short value, uint pluginLine)
			: base(name, offset, address, type, value, pluginLine)
		{
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitInt16(this);
		}

		public override TagDataField CloneValue()
		{
			return new Int16Data(Name, Offset, FieldAddress, Type, Value, base.PluginLine);
		}
	}

	/// <summary>
	///     Unsigned 32-bit integer.
	/// </summary>
	public class Uint32Data : NumberData<uint>
	{
		public Uint32Data(string name, uint offset, uint address, string type, uint value, uint pluginLine)
			: base(name, offset, address, type, value, pluginLine)
		{
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitUint32(this);
		}

		public override TagDataField CloneValue()
		{
			return new Uint32Data(Name, Offset, FieldAddress, Type, Value, base.PluginLine);
		}
	}

	/// <summary>
	///     Signed 32-bit integer.
	/// </summary>
	public class Int32Data : NumberData<int>
	{
		public Int32Data(string name, uint offset, uint address, string type, int value, uint pluginLine)
			: base(name, offset, address, type, value, pluginLine)
		{
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitInt32(this);
		}

		public override TagDataField CloneValue()
		{
			return new Int32Data(Name, Offset, FieldAddress, Type, Value, base.PluginLine);
		}
	}

	/// <summary>
	///     32-bit floating-point number.
	/// </summary>
	public class Float32Data : NumberData<float>
	{
		public Float32Data(string name, uint offset, uint address, string type, float value, uint pluginLine)
			: base(name, offset, address, type, value, pluginLine)
		{
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitFloat32(this);
		}

		public override TagDataField CloneValue()
		{
			return new Float32Data(Name, Offset, FieldAddress, Type, Value, base.PluginLine);
		}
	}
}