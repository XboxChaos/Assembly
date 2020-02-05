namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	/// <summary>
	///     Base class for colour data.
	/// </summary>
	public class ColourData : ValueField
	{
		private string _dataType;
		private string _format;
		private string _value;

		public ColourData(string name, uint offset, long address, string format, string dataType, string value,
			uint pluginLine)
			: base(name, offset, address, pluginLine)
		{
			_format = format;
			_value = value;
			_dataType = dataType;
		}

		public string Format
		{
			get { return _format; }
			set
			{
				_format = value;
				NotifyPropertyChanged("Format");
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

		public string Value
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
			if (DataType == "int")
				visitor.VisitColourInt(this);
			else
				visitor.VisitColourFloat(this);
		}

		public override MetaField CloneValue()
		{
			return new ColourData(Name, Offset, FieldAddress, Format, DataType, Value, PluginLine);
		}
	}
}