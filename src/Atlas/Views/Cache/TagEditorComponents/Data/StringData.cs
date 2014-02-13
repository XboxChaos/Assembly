using Atlas.Views.Cache.TagEditorComponents.Data;

namespace Atlas.Views.Cache.TagEditorComponents.Data
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

		public StringData(string name, uint offset, uint address, StringType type, string value, int size, uint pluginLine)
			: base(name, offset, address, pluginLine)
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
				OnPropertyChanged("Value");
			}
		}

		public int Size
		{
			get { return _size; }
			set
			{
				_size = value;
				OnPropertyChanged("Size");
				OnPropertyChanged("MaxLength");
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
				OnPropertyChanged("Type");
				OnPropertyChanged("TypeStr");
			}
		}

		public string TypeStr
		{
			get { return _type.ToString().ToLower(); }
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitString(this);
		}

		public override TagDataField CloneValue()
		{
			return new StringData(Name, Offset, FieldAddress, _type, _value, _size, base.PluginLine);
		}
	}
}