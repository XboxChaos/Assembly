using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public class StringData : ValueField
    {
        private string _value, _originalValue;
        private int _length;

        public StringData(string name, uint offset, string value, int length, uint pluginLine)
            : base(name, offset, pluginLine)
        {
            _value = value;
            _originalValue = value;
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

        public override MetaField DeepClone()
        {
            StringData result = new StringData(Name, Offset, _value, _length, base.PluginLine);
            result._originalValue = _originalValue;
            return result;
        }

        public override bool HasChanged
        {
            get { return _value != _originalValue; }
        }

        public override void Reset()
        {
            Value = _originalValue;
        }

        public override void KeepChanges()
        {
            _originalValue = _value;
        }
    }
}
