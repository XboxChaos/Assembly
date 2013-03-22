namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public class DataRef : RawData
    {
        private uint _dataAddress;
	    private string _format;

        public DataRef(string name, uint offset, string format, uint address, uint dataAddress, string value, int length, uint pluginLine)
            : base(name, offset, format, address, value, length, pluginLine)
        {
            _dataAddress = dataAddress;
	        _format = format;
        }

	    public new string Format
	    {
			get { return _format; }
			set { _format = value; NotifyPropertyChanged("Format"); }
	    }

        public new uint DataAddress
        {
            get { return _dataAddress; }
            set { _dataAddress = value; NotifyPropertyChanged("DataAddress"); }
        }

        public override void Accept(IMetaFieldVisitor visitor)
        {
            visitor.VisitDataRef(this);
        }

        public override MetaField CloneValue()
        {
            var result = new DataRef(Name, Offset, Format, FieldAddress, _dataAddress, Value, Length, PluginLine);
            return result;
        }
    }
}
