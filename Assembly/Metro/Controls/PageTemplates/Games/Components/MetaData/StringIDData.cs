using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public class StringIDData : ValueField
    {
        private int _value, _originalValue;

        public StringIDData(string name, uint offset, int index, uint pluginLine)
            : base(name, offset, pluginLine)
        {
            _value = index;
            _originalValue = index;
        }

        public int Value
        {
            get { return _value; }
            set { _value = value; NotifyPropertyChanged("Value"); }
        }

        public override void Accept(IMetaFieldVisitor visitor)
        {
            visitor.VisitStringID(this);
        }

        public override MetaField DeepClone()
        {
            StringIDData result = new StringIDData(Name, Offset, _value, base.PluginLine);
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
