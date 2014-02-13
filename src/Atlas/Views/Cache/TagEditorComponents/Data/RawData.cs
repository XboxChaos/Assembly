using ICSharpCode.AvalonEdit.Document;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	/// <summary>
	///     Base class for raw data.
	/// </summary>
	public class RawData : ValueField
	{
		private uint _dataAddress;
		private TextDocument _document;
		private string _format;
		private int _length;

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
			set
			{
				_document = value;
				OnPropertyChanged("Document");
			}
		}

		public string Kind
		{
			get { return "byte array"; }
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

		public string Value
		{
			get { return _document.Text; }
			set
			{
				_document.Text = value;
				OnPropertyChanged("Value");
				OnPropertyChanged("Document");
			}
		}

		public uint DataAddress
		{
			get { return _dataAddress; }
			set
			{
				_dataAddress = value;
				OnPropertyChanged("DataAddress");
				OnPropertyChanged("DataAddressHex");
			}
		}

		public string DataAddressHex
		{
			get { return "0x" + DataAddress.ToString("X"); }
		}

		public int Length
		{
			get { return _length; }
			set
			{
				_length = value;
				OnPropertyChanged("Length");
				OnPropertyChanged("MaxLength");
			}
		}

		public int MaxLength
		{
			get { return _format == "bytes" ? _length*2 : _length; }
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitRawData(this);
		}

		public override TagDataField CloneValue()
		{
			return new RawData(Name, Offset, FieldAddress, _document.Text, _length, PluginLine);
		}
	}
}