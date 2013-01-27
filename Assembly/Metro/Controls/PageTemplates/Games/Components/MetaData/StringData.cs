using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public class StringData : ValueField
    {
        private string _value;
        private int _length;

        public StringData(string name, uint offset, uint address, string value, int length, uint pluginLine)
            : base(name, offset, address, pluginLine)
        {
            _value = value;
            _length = length;
        }

        public string Value
        {
            get { return _value; }
            set { _value = value; NotifyPropertyChanged("Value"); }
        }

        public int Length
        {
            get { return _length; }
            set { _length = value; NotifyPropertyChanged("Length"); }
        }

        public override void Accept(IMetaFieldVisitor visitor)
        {
            visitor.VisitString(this);
        }

        public override MetaField CloneValue()
        {
            return new StringData(Name, Offset, FieldAddress, _value, _length, base.PluginLine);
        }
    }
}
