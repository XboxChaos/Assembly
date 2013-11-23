using ICSharpCode.AvalonEdit.Document;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    /// <summary>
    /// Base class for raw data.
    /// </summary>
    public class RawData : ValueField
    {
        private TextDocument _document;
        private int _length;
        private uint _dataAddress;
	    private string _format;

        public RawData(string name, uint offset, uint address, string value, int length, uint pluginLine)
            : base(name, offset, address, pluginLine)
        {
            _document = new TextDocument(new StringTextSource(value));
            _length = length;
        }

		public RawData(string name, uint offset, string format, uint address, string value, int length, uint pluginLine)
			: base(name, offset, address, pluginLine)
		{
			_document = new TextDocument(new StringTextSource(value));
			_length = length;
			_format = format;
		}

        public TextDocument Document
        {
            get { return _document; }
            set { _document = value; NotifyPropertyChanged("Document"); }
        }

        public string Kind
        {
            get { return "byte array"; }
        }

	    public string Format
	    {
			get { return _format; }
			set { _format = value; NotifyPropertyChanged("Format"); }
	    }

        public string Value
        {
            get { return _document.Text; }
            set { _document.Text = value; NotifyPropertyChanged("Value"); NotifyPropertyChanged("Document"); }
        }

		public uint DataAddress
		{
            get { return _dataAddress; }
            set { _dataAddress = value; NotifyPropertyChanged("DataAddress"); NotifyPropertyChanged("DataAddressHex"); }
		}

        public string DataAddressHex
        {
            get { return "0x" + DataAddress.ToString("X"); }
        }

        public int Length
        {
            get { return _length; }
            set { _length = value; NotifyPropertyChanged("Length"); NotifyPropertyChanged("MaxLength"); }
        }

        public int MaxLength
        {
			get { return _format == "bytes" ? _length * 2 : _length; }
        }

        public override void Accept(IMetaFieldVisitor visitor)
        {
            visitor.VisitRawData(this);
        }

        public override MetaField CloneValue()
        {
            return new RawData(Name, Offset, FieldAddress, _document.Text, _length, PluginLine);
        }
    }
}
