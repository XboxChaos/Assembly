using System;
using System.Collections.Generic;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public abstract class Multi2Data<T> : ValueField
	{
		private string _type;
		internal T _a, _b;
		
		public Multi2Data(string name, uint offset, long address, string type, T a, T b, uint pluginLine, string tooltip)
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

		public override string AsString()
		{
			return string.Format("{0} | {1} | {2} {3}", Type, Name, A, B);
		}

		public override object GetAsJson()
		{
			List<object> dict = new List<object>(3)
			{
				A,
				B
			};

			return dict;
		}
	}

	public abstract class Multi3Data<T> : ValueField
	{
		private string _type;
		internal T _a, _b, _c;

		public Multi3Data(string name, uint offset, long address, string type, T a, T b, T c, uint pluginLine, string tooltip)
			: base(name, offset, address, pluginLine, tooltip)
		{
			_type = type;
			_a = a;
			_b = b;
			_c = c;
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

		public T C
		{
			get { return _c; }
			set
			{
				_c = value;
				NotifyPropertyChanged("C");
			}
		}

		public override string AsString()
		{
			return string.Format("{0} | {1} | {2} {3} {4}", Type, Name, A, B, C);
		}

		public override object GetAsJson()
		{
			List<object> dict = new List<object>(3)
			{
				A,
				B,
				C
			};

			return dict;
		}
	}

	public abstract class Multi4Data<T> : ValueField
	{
		private string _type;
		internal T _a, _b, _c, _d;

		public Multi4Data(string name, uint offset, long address, string type, T a, T b, T c, T d, uint pluginLine, string tooltip)
			: base(name, offset, address, pluginLine, tooltip)
		{
			_type = type;
			_a = a;
			_b = b;
			_c = c;
			_d = d;
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

		public T C
		{
			get { return _c; }
			set
			{
				_c = value;
				NotifyPropertyChanged("C");
			}
		}

		public T D
		{
			get { return _d; }
			set
			{
				_d = value;
				NotifyPropertyChanged("D");
			}
		}

		public override string AsString()
		{
			return string.Format("{0} | {1} | {2} {3} {4} {5}", Type, Name, A, B, C, D);
		}

		public override object GetAsJson()
		{
			List<object> dict = new List<object>(4)
			{
				A,
				B,
				C,
				D
			};

			return dict;
		}
	}

	public class Degree2Data : Multi2Data<float>
	{
		public Degree2Data(string name, uint offset, long address, string type, float a, float b, uint pluginLine, string tooltip)
			: base(name, offset, address, type, a, b, pluginLine, tooltip)
		{
		}

		public new float A
		{
			get { return FromRadian(_a); }
			set
			{
				_a = ToRadian(value);
				NotifyPropertyChanged("A");
			}
		}
		
		public new float B
		{
			get { return FromRadian(_b); }
			set
			{
				_b = ToRadian(value);
				NotifyPropertyChanged("B");
			}
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
		
		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitDegree2(this);
		}
		
		public override MetaField CloneValue()
		{
			return new Degree2Data(Name, Offset, FieldAddress, Type, RadianA, RadianB, PluginLine, ToolTip);
		}

		public override string AsString()
		{
			return string.Format("{0} | {1} | {2} {3}", Type, Name, A, B);
		}

		public override object GetAsJson()
		{
			List<object> dict = new List<object>(3)
			{
				A,
				B
			};

			return dict;
		}
	}

	public class Degree3Data : Multi3Data<float>
	{
		public Degree3Data(string name, uint offset, long address, string type, float a, float b, float c, uint pluginLine, string tooltip)
			: base(name, offset, address, type, a, b, c, pluginLine, tooltip)
		{
		}

		public new float A
		{
			get { return FromRadian(_a); }
			set
			{
				_a = ToRadian(value);
				NotifyPropertyChanged("A");
			}
		}

		public new float B
		{
			get { return FromRadian(_b); }
			set
			{
				_b = ToRadian(value);
				NotifyPropertyChanged("B");
			}
		}

		public new float C
		{
			get { return FromRadian(_c); }
			set
			{
				_c = ToRadian(value);
				NotifyPropertyChanged("C");
			}
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

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitDegree3(this);
		}

		public override MetaField CloneValue()
		{
			return new Degree3Data(Name, Offset, FieldAddress, Type, RadianA, RadianB, RadianC, PluginLine, ToolTip);
		}

		public override string AsString()
		{
			return string.Format("{0} | {1} | {2} {3} {4}", Type, Name, A, B, C);
		}

		public override object GetAsJson()
		{
			List<object> dict = new List<object>(3)
			{
				A,
				B,
				C
			};

			return dict;
		}
	}

	public class Vector2Data : Multi2Data<float>
	{
		public Vector2Data(string name, uint offset, long address, string type, float a, float b, uint pluginLine, string tooltip)
			: base(name, offset, address, type, a, b, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitVector2(this);
		}

		public override MetaField CloneValue()
		{
			return new Vector2Data(Name, Offset, FieldAddress, Type, A, B, PluginLine, ToolTip);
		}
	}

	public class Vector3Data : Multi3Data<float>
	{
		public Vector3Data(string name, uint offset, long address, string type, float a, float b, float c, uint pluginLine, string tooltip)
			: base(name, offset, address, type, a, b, c, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitVector3(this);
		}

		public override MetaField CloneValue()
		{
			return new Vector3Data(Name, Offset, FieldAddress, Type, A, B, C, PluginLine, ToolTip);
		}
	}

	public class Vector4Data : Multi4Data<float>
	{
		public Vector4Data(string name, uint offset, long address, string type, float a, float b, float c, float d, uint pluginLine, string tooltip)
			: base(name, offset, address, type, a, b, c, d, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitVector4(this);
		}

		public override MetaField CloneValue()
		{
			return new Vector4Data(Name, Offset, FieldAddress, Type, A, B, C, D, PluginLine, ToolTip);
		}
	}

	public class Point2Data : Multi2Data<float>
	{
		public Point2Data(string name, uint offset, long address, string type, float a, float b, uint pluginLine, string tooltip)
			: base(name, offset, address, type, a, b, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitPoint2(this);
		}

		public override MetaField CloneValue()
		{
			return new Point2Data(Name, Offset, FieldAddress, Type, A, B, PluginLine, ToolTip);
		}
	}

	public class Point3Data : Multi3Data<float>
	{
		public Point3Data(string name, uint offset, long address, string type, float a, float b, float c, uint pluginLine, string tooltip)
			: base(name, offset, address, type, a, b, c, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitPoint3(this);
		}

		public override MetaField CloneValue()
		{
			return new Point3Data(Name, Offset, FieldAddress, Type, A, B, C, PluginLine, ToolTip);
		}
	}

	public class Plane2Data : Multi3Data<float>
	{
		public Plane2Data(string name, uint offset, long address, string type, float a, float b, float c, uint pluginLine, string tooltip)
			: base(name, offset, address, type, a, b, c, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitPlane2(this);
		}

		public override MetaField CloneValue()
		{
			return new Plane2Data(Name, Offset, FieldAddress, Type, A, B, C, PluginLine, ToolTip);
		}
	}

	public class Plane3Data : Multi4Data<float>
	{
		public Plane3Data(string name, uint offset, long address, string type, float a, float b, float c, float d, uint pluginLine, string tooltip)
			: base(name, offset, address, type, a, b, c, d, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitPlane3(this);
		}

		public override MetaField CloneValue()
		{
			return new Plane3Data(Name, Offset, FieldAddress, Type, A, B, C, D, PluginLine, ToolTip);
		}
	}

	public class RectangleData : Multi4Data<short>
	{
		public RectangleData(string name, uint offset, long address, string type, short a, short b, short c, short d, uint pluginLine, string tooltip)
			: base(name, offset, address, type, a, b, c, d, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitRect16(this);
		}

		public override MetaField CloneValue()
		{
			return new RectangleData(Name, Offset, FieldAddress, Type, A, B, C, D, PluginLine, ToolTip);
		}
	}

	public class Quaternion16Data : Multi4Data<short>
	{
		public Quaternion16Data(string name, uint offset, long address, string type, short a, short b, short c, short d, uint pluginLine, string tooltip)
			: base(name, offset, address, type, a, b, c, d, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitQuat16(this);
		}

		public override MetaField CloneValue()
		{
			return new Quaternion16Data(Name, Offset, FieldAddress, Type, A, B, C, D, PluginLine, ToolTip);
		}
	}

	public class Point16Data : Multi2Data<short>
	{
		public Point16Data(string name, uint offset, long address, string type, short a, short b, uint pluginLine, string tooltip)
			: base(name, offset, address, type, a, b, pluginLine, tooltip)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitPoint16(this);
		}

		public override MetaField CloneValue()
		{
			return new Point16Data(Name, Offset, FieldAddress, Type, A, B, PluginLine, ToolTip);
		}
	}

}