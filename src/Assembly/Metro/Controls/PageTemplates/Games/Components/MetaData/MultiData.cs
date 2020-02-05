using System;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public class RectangleData : ValueField
	{
		private short _a, _b, _c, _d;
		private string _type;

		public RectangleData(string name, uint offset, long address, string type, short a, short b, short c, short d, uint pluginLine)
			: base(name, offset, address, pluginLine)
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

		public short A
		{
			get { return _a; }
			set
			{
				_a = value;
				NotifyPropertyChanged("A");
			}
		}

		public short B
		{
			get { return _b; }
			set
			{
				_b = value;
				NotifyPropertyChanged("B");
			}
		}

		public short C
		{
			get { return _c; }
			set
			{
				_c = value;
				NotifyPropertyChanged("C");
			}
		}

		public short D
		{
			get { return _d; }
			set
			{
				_d = value;
				NotifyPropertyChanged("D");
			}
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitRect16(this);
		}

		public override MetaField CloneValue()
		{
			return new RectangleData(Name, Offset, FieldAddress, Type, A, B, C, D, base.PluginLine);
		}

	}

	public abstract class DegreeMultData : ValueField
	{
		private float _a, _b, _c;
		private string _type;

		public DegreeMultData(string name, uint offset, long address, string type, float a, float b, float c, uint pluginLine)
			: base(name, offset, address, pluginLine)
		{
			_a = a;
			_b = b;
			_c = c;
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

		public float A
		{
			get { return (float)(_a * (180 / Math.PI)); }
			set
			{
				_a = (float)(value * (Math.PI / 180));
				NotifyPropertyChanged("A");
			}
		}

		public float B
		{
			get { return (float)(_b * (180 / Math.PI)); }
			set
			{
				_b = (float)(value * (Math.PI / 180));
				NotifyPropertyChanged("B");
			}
		}

		public float C
		{
			get { return (float)(_c * (180 / Math.PI)); }
			set
			{
				_c = (float)(value * (Math.PI / 180));
				NotifyPropertyChanged("C");
			}
		}

		public float D // only here so the control doesn't complain
		{
			get { return 0; }
			set { }
		}

		public float RadianA
		{
			get { return _a; }
			set
			{
				_a = value;
				NotifyPropertyChanged("RadianA");
				NotifyPropertyChanged("A");
			}
		}

		public float RadianB
		{
			get { return _b; }
			set
			{
				_b = value;
				NotifyPropertyChanged("RadianB");
				NotifyPropertyChanged("B");
			}
		}

		public float RadianC
		{
			get { return _c; }
			set
			{
				_c = value;
				NotifyPropertyChanged("RadianC");
				NotifyPropertyChanged("C");
			}
		}

	}

	public class Degree2Data : DegreeMultData
	{
		public Degree2Data(string name, uint offset, long address, string type, float a, float b, uint pluginLine, float c = 0)
			: base(name, offset, address, type, a, b, c, pluginLine)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitDegree2(this);
		}

		public override MetaField CloneValue()
		{
			return new Degree2Data(Name, Offset, FieldAddress, Type, RadianA, RadianB, base.PluginLine);
		}
	}

	public class Degree3Data : DegreeMultData
	{
		public Degree3Data(string name, uint offset, long address, string type, float a, float b, float c, uint pluginLine)
			: base(name, offset, address, type, a, b, c, pluginLine)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitDegree3(this);
		}

		public override MetaField CloneValue()
		{
			return new Degree3Data(Name, Offset, FieldAddress, Type, RadianA, RadianB, RadianC, base.PluginLine);
		}
	}

	public abstract class VectorData : ValueField
	{
		private float _a, _b, _c, _d;
		private string _type;

		public VectorData(string name, uint offset, long address, string type, float a, float b, float c, float d, uint pluginLine)
			: base(name, offset, address, pluginLine)
		{
			_a = a;
			_b = b;
			_c = c;
			_d = d;
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

		public float A
		{
			get { return _a; }
			set
			{
				_a = value;
				NotifyPropertyChanged("A");
			}
		}

		public float B
		{
			get { return _b; }
			set
			{
				_b = value;
				NotifyPropertyChanged("B");
			}
		}

		public float C
		{
			get { return _c; }
			set
			{
				_c = value;
				NotifyPropertyChanged("C");
			}
		}

		public float D
		{
			get { return _d; }
			set
			{
				_d = value;
				NotifyPropertyChanged("D");
			}
		}

	}

	public class Vector2Data : VectorData
	{
		public Vector2Data(string name, uint offset, long address, string type, float a, float b, uint pluginLine, float c = 0, float d = 0)
			: base(name, offset, address, type, a, b, c, d, pluginLine)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitVector2(this);
		}

		public override MetaField CloneValue()
		{
			return new Vector2Data(Name, Offset, FieldAddress, Type, A, B, base.PluginLine);
		}
	}

	public class Vector3Data : VectorData
	{
		public Vector3Data(string name, uint offset, long address, string type, float a, float b, float c, uint pluginLine, float d = 0)
			: base(name, offset, address, type, a, b, c, d, pluginLine)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitVector3(this);
		}

		public override MetaField CloneValue()
		{
			return new Vector3Data(Name, Offset, FieldAddress, Type, A, B, C, base.PluginLine);
		}
	}

	public class Vector4Data : VectorData //needs to be split off with quaternion math someday
	{
		public Vector4Data(string name, uint offset, long address, string type, float a, float b, float c, float d, uint pluginLine)
			: base(name, offset, address, type, a, b, c, d, pluginLine)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitVector4(this);
		}

		public override MetaField CloneValue()
		{
			return new Vector4Data(Name, Offset, FieldAddress, Type, A, B, C, D, base.PluginLine);
		}
	}
}