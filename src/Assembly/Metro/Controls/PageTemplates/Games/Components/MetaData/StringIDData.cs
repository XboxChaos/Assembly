using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam;
using Blamite.Util;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public class StringIDData : ValueField
    {
        private string _value;
        private Trie _autocompleteTrie;

        public StringIDData(string name, uint offset, uint address, string val, Trie autocompleteTrie, uint pluginLine)
            : base(name, offset, address, pluginLine)
        {
            _value = val;
            _autocompleteTrie = autocompleteTrie;
        }

        public string Value
        {
            get { return _value; }
            set { _value = value; NotifyPropertyChanged("Value"); }
        }

        public Trie AutocompleteTrie
        {
            get { return _autocompleteTrie; }
            set { _autocompleteTrie = value; NotifyPropertyChanged("AutocompleteTrie"); }
        }

        public override void Accept(IMetaFieldVisitor visitor)
        {
            visitor.VisitStringID(this);
        }

        public override MetaField CloneValue()
        {
            return new StringIDData(Name, Offset, FieldAddress, _value, _autocompleteTrie, base.PluginLine);
        }
    }
}
