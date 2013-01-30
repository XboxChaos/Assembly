using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam;
using System.Windows;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public class TagRefData : ValueField
    {
        private TagEntry _value;
        private TagClass _class;
        private TagHierarchy _allTags;
        private bool _withClass;
        private Visibility _showJumpTo;

        public TagRefData(string name, uint offset, uint address, TagHierarchy allTags, Visibility showJumpTo, bool withClass, uint pluginLine)
            : base(name, offset, address, pluginLine)
        {
            _allTags = allTags;
            _withClass = withClass;
            _showJumpTo = showJumpTo;
        }

        public TagEntry Value
        {
            get { return _value; }
            set { _value = value; NotifyPropertyChanged("Value"); }
        }

        public Visibility ShowJumpTo
        {
            get { return _showJumpTo; }
            set { _showJumpTo = value; NotifyPropertyChanged("ShowJumpTo"); }
        }

        public TagClass Class
        {
            get { return _class; }
            set { _class = value; NotifyPropertyChanged("Class"); }
        }

        public bool WithClass
        {
            get { return _withClass; }
        }

        public TagHierarchy Tags
        {
            get { return _allTags; }
        }

        public override void Accept(IMetaFieldVisitor visitor)
        {
            visitor.VisitTagRef(this);
        }

        public override MetaField CloneValue()
        {
            TagRefData result = new TagRefData(Name, Offset, FieldAddress, _allTags, _showJumpTo, _withClass, base.PluginLine);
            result.Class = _class;
            result.Value = _value;
            return result;
        }
    }
}
