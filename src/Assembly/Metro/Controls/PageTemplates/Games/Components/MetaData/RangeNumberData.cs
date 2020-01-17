using System;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	/// <summary>
	///     Base class for number data.
	/// </summary>
	/// <typeparam name="T">The type of number to hold.</typeparam>
	public abstract class RangeNumberData<T> : ValueField
	{
		private string _type;
		private T _a, _b;

		public RangeNumberData(string name, uint offset, long address, string type, T a, T b, uint pluginLine, string tooltip)
			: base(name, offset, address, pluginLine, tooltip)
		{
			_type = type;
			_a = a;
			_b = b;
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

		public T A
		{
			get { return _a; }
			set
			{
				_a = value;
				NotifyPropertyChanged("A");
			}
		}

		public T B
		{
			get { return _b; }
			set
			{
				_b = value;
				NotifyPropertyChanged("B");
			}
		}

		public int C // only here so the control doesn't complain
		{
			get { return 0; }
			set { }
		}

		public int D // only here so the control doesn't complain
		{
			get { return 0; }
			set { }
		}

	}

	/// <summary>
	///     Unsigned 16-bit integer.
	/// </summary>
	public class RangeUint16Data : RangeNumberData<ushort>
	{
		public RangeUint16Data(string name, uint offset, long address, string type, ushort a, ushort b, uint pluginLine, string tooltip)
			: base(name, offset, address, type, a, b, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitRangeUint16(this);
		}

		public override MetaField CloneValue()
		{
			return new RangeUint16Data(Name, Offset, FieldAddress, Type, A, B, PluginLine, Tooltip);
		}
	}

	/// <summary>
	///     32-bit floating-point number.
	/// </summary>
	public class RangeFloat32Data : VectorData
	{
		public RangeFloat32Data(string name, uint offset, long address, string type, float a, float b, uint pluginLine, string tooltip, float c = 0, float d = 0)
			: base(name, offset, address, type, a, b, c, d, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitRangeFloat32(this);
		}

		public override MetaField CloneValue()
		{
			return new RangeFloat32Data(Name, Offset, FieldAddress, Type, A, B, PluginLine, Tooltip);
		}
	}

	/// <summary>
	///     32-bit floating-point number, converted from radians to degrees
	/// </summary>
	public class RangeDegreeData : DegreeMultData
	{
		public RangeDegreeData(string name, uint offset, long address, string type, float a, float b, uint pluginLine, string tooltip, float c = 0)
			: base(name, offset, address, type, a, b, c, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitRangeDegree(this);
		}

		public override MetaField CloneValue()
		{
			return new RangeDegreeData(Name, Offset, FieldAddress, Type, RadianA, RadianB, PluginLine, Tooltip);
		}
	}
}