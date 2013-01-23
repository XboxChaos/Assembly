using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    /// <summary>
    /// Base class for raw data.
    /// </summary>
    public class RawData : ValueField
    {
        private string _value;
        private int _length;

        public RawData(string name, uint offset, string value, int length, uint pluginLine)
            : base(name, offset, pluginLine)
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
            return new RawData(Name, Offset, _value, _length, base.PluginLine);
        }
    }
}
