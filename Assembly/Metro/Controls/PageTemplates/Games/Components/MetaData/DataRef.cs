using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.Document;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public class DataRef : RawData
    {
        private uint _dataAddress;

        public DataRef(string name, uint offset, uint address, uint dataAddress, string value, int length, uint pluginLine)
            : base(name, offset, address, value, length, pluginLine)
        {
            _dataAddress = dataAddress;
        }

        public uint DataAddress
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
            DataRef result = new DataRef(Name, Offset, FieldAddress, _dataAddress, Value, Length, base.PluginLine);
            return result;
        }
    }
}
