using System;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public class DegreeData : ValueField
	{
		private float _radian;

		public DegreeData(string name, uint offset, uint address, float radian, uint pluginLine)
			: base(name, offset, address, pluginLine)
		{
			_radian = radian;
		}

		public float Degree
		{
			get { return (float) (_radian*(180/Math.PI)); }
			set
			{
				_radian = (float) (value*(Math.PI/180));
				NotifyPropertyChanged("Degree");
			}
		}

		public float Radian
		{
			get { return _radian; }
			set
			{
				_radian = value;
				NotifyPropertyChanged("Radian");
				NotifyPropertyChanged("Degree");
			}
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitDegree(this);
		}

		public override MetaField CloneValue()
		{
			return new DegreeData(Name, Offset, FieldAddress, _radian, PluginLine);
		}
	}
}