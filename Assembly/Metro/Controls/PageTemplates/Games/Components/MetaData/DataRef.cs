using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public class DataRef : ValueField
    {
        private string _value, _originalValue;
        private int _length, _maxLength;

        public DataRef(string name, uint offset, string value, uint pluginLine)
            : base(name, offset, pluginLine)
        {
            _value = value;
            _originalValue = value;
            _maxLength = _length * 2;
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
        public int MaxLength
        {
            get { return _maxLength; }
            set { _maxLength = value; NotifyPropertyChanged("MaxLength"); }
        }

        public uint DataMemoryAddress { get; set; }
        public uint DataCacheOffset { get; set; }

        public override void Accept(IMetaFieldVisitor visitor)
        {
            visitor.VisitDataRef(this);
        }

        public override MetaField DeepClone()
        {
            DataRef result = new DataRef(Name, Offset, _value, base.PluginLine);
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
