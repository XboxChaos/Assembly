using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public RawData(string name, uint offset, uint address, string value, int length, uint pluginLine)
            : base(name, offset, address, pluginLine)
        {
            _document = new TextDocument(new StringTextSource(value));
            _length = length;
        }

        public TextDocument Document
        {
            get { return _document; }
            set
            {
	            _document = value;

				if (value.Text.Length > MaxLength)
					_document.Text = value.Text.Remove(MaxLength);

				NotifyPropertyChanged("Document");
            }
        }

		public string Value
		{
			get { return _document.Text; }
			set
			{
				_document.Text = value;

				if (value.Length > MaxLength)
					_document.Text = value.Remove(MaxLength);

				NotifyPropertyChanged("Value");
				NotifyPropertyChanged("Document");
			}
		}

		public uint DataAddress
		{
			get { return FieldAddress; }
		}

        public int Length
        {
            get { return _length; }
            set { _length = value; NotifyPropertyChanged("Length"); NotifyPropertyChanged("MaxLength"); }
        }
        public int MaxLength
        {
            get { return _length * 2; }
        }

        public override void Accept(IMetaFieldVisitor visitor)
        {
            visitor.VisitRawData(this);
        }

        public override MetaField CloneValue()
        {
            return new RawData(Name, Offset, FieldAddress, _document.Text, _length, base.PluginLine);
        }
    }
}
