using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public class DataRef : ValueField
    {
        private string _value;
        private uint _address;
        private int _maxLength;

        public DataRef(string name, uint offset, uint address, string value, uint pluginLine)
            : base(name, offset, pluginLine)
        {
            _value = value;
            _maxLength = _value.Length;
        }

        public string Value
        {
            get { return _value; }
            set { _value = value; NotifyPropertyChanged("Value"); }
        }

        public uint Address
        {
            get { return _address; }
            set { _address = value; NotifyPropertyChanged("Address"); }
        }

        public int MaxLength
        {
            get { return _maxLength; }
            set { _maxLength = value; NotifyPropertyChanged("MaxLength"); }
        }

        public override void Accept(IMetaFieldVisitor visitor)
        {
            visitor.VisitDataRef(this);
        }

        public override MetaField CloneValue()
        {
            DataRef result = new DataRef(Name, Offset, _address, _value, base.PluginLine);
            return result;
        }
    }
}
