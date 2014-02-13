using Atlas.Views.Cache.TagEditorComponents.Data;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	/// <summary>
	///     Base class for colour data.
	/// </summary>
	public class ColourData : ValueField
	{
		private string _dataType;
		private string _format;
		private string _value;

		public ColourData(string name, uint offset, uint address, string format, string dataType, string value,
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
				OnPropertyChanged("Format");
			}
		}

		public string DataType
		{
			get { return _dataType; }
			set
			{
				_dataType = value;
				OnPropertyChanged("DataType");
			}
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

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			if (DataType == "int")
				visitor.VisitColourInt(this);
			else
				visitor.VisitColourFloat(this);
		}

		public override TagDataField CloneValue()
		{
			return new ColourData(Name, Offset, FieldAddress, Format, DataType, Value, PluginLine);
		}
	}
}