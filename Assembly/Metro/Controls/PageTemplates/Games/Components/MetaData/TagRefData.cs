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
        private TagEntry _value, _originalValue;
        private TagClass _class, _originalClass;
        private TagHierarchy _allTags;
        private bool _withClass;
        private Visibility _showJumpTo;

        public TagRefData(string name, uint offset, TagHierarchy allTags, Visibility showJumpTo, bool withClass, uint pluginLine)
            : base(name, offset, pluginLine)
        {
            _allTags = allTags;
            _withClass = withClass;
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

        public override MetaField DeepClone()
        {
            TagRefData result = new TagRefData(Name, Offset, _allTags, _showJumpTo, _withClass, base.PluginLine);
            result.Class = _class;
            result.Value = _value;
            result._originalValue = _originalValue;
            result._originalClass = _originalClass;
            return result;
        }

        public override bool HasChanged
        {
            get { return _value != _originalValue || _class != _originalClass; }
        }

        public override void Reset()
        {
            Class = _originalClass;
            Value = _originalValue;
        }

        public override void KeepChanges()
        {
            _originalValue = _value;
            _originalClass = _class;
        }
    }
}
