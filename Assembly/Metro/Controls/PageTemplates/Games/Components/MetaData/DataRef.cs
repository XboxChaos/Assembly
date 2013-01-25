using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.Document;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public class DataRef : RawData
    {
        private uint _address;

        public DataRef(string name, uint offset, uint address, string value, int length, uint pluginLine)
            : base(name, offset, value, length, pluginLine)
        {
            _address = address;
        }

        public uint Address
        {
            get { return _address; }
            set { _address = value; NotifyPropertyChanged("Address"); }
        }

        public override void Accept(IMetaFieldVisitor visitor)
        {
            visitor.VisitDataRef(this);
        }

        public override MetaField CloneValue()
        {
            DataRef result = new DataRef(Name, Offset, _address, Value, Length, base.PluginLine);
            return result;
        }
    }
}
