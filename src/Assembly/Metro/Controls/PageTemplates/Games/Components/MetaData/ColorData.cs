using System.Windows.Media;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	/// <summary>
	///     Base class for color data.
	/// </summary>
	public class ColorData : ValueField
	{
		private string _dataType;
		private bool _alpha;
		private bool _basic;
		private Color _value;

		public ColorData(string name, uint offset, long address, bool alpha, bool basic, string dataType, Color value,
			uint pluginLine, string tooltip)
			: base(name, offset, address, pluginLine, tooltip)
		{
			_value = value;
			_alpha = alpha;
			_basic = basic;
			_dataType = dataType;
		}

		public bool Alpha
		{
			get { return _alpha; }
			set
			{
				_alpha = value;
				NotifyPropertyChanged("Alpha");
			}
		}

		public bool Basic
		{
			get { return _basic; }
			set
			{
				_basic = value;
				NotifyPropertyChanged("Basic");
			}
		}

		public string DataType
		{
			get { return _dataType; }
			set
			{
				_dataType = value;
				NotifyPropertyChanged("DataType");
			}
		}

		public Color Value
		{
			get { return _value; }
			set
			{
				_value = value;
				NotifyPropertyChanged("Value");
			}
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			if (DataType == "color32")
				visitor.VisitColourInt(this);
			else
				visitor.VisitColourFloat(this);
		}

		public override MetaField CloneValue()
		{
			return new ColorData(Name, Offset, FieldAddress, Alpha, Basic, DataType, Value, PluginLine, ToolTip);
		}

		public override string AsString()
		{
			return string.Format("{0} | {1} | {2} {3} {4}", DataType, Name, Value.R, Value.G, Value.B);
		}
	}
}