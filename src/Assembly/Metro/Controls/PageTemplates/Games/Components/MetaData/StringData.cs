namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public enum StringType
	{
		ASCII,
		UTF16
	}

	public class StringData : ValueField
	{
		private int _size;
		private StringType _type;
		private string _value;

		public StringData(string name, uint offset, long address, StringType type, string value, int size, uint pluginLine, string tooltip)
			: base(name, offset, address, pluginLine, tooltip)
		{
			_value = value;
			_size = size;
			_type = type;
		}

		public string Value
		{
			get { return _value; }
			set
			{
				_value = value;
				NotifyPropertyChanged("Value");
			}
		}

		public int Size
		{
			get { return _size; }
			set
			{
				_size = value;
				NotifyPropertyChanged("Size");
				NotifyPropertyChanged("MaxLength");
			}
		}

		public int MaxLength
		{
			get
			{
				switch (_type)
				{
					case StringType.ASCII:
						return _size;
					case StringType.UTF16:
						return _size/2;
					default:
						return _size;
				}
			}
		}

		public StringType Type
		{
			get { return _type; }
			set
			{
				_type = value;
				NotifyPropertyChanged("Type");
				NotifyPropertyChanged("TypeStr");
			}
		}

		public string TypeStr
		{
			get { return _type.ToString().ToLower(); }
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitString(this);
		}

		public override MetaField CloneValue()
		{
			return new StringData(Name, Offset, FieldAddress, _type, _value, _size, PluginLine, ToolTip);
		}

		public override string AsString()
		{
			return string.Format("{0} | {1} | {2}", TypeStr, Name, Value);
		}
	}
}