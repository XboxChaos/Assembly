using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public class CommentData : MetaField
    {
        private string _name, _text;

        public CommentData(string name, string text)
        {
            _name = name;
            _text = text;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; NotifyPropertyChanged("Name"); }
        }

        public string Text
        {
            get { return _text; }
            set { _text = value; NotifyPropertyChanged("Text"); }
        }

        public override void Accept(IMetaFieldVisitor visitor)
        {
            visitor.VisitComment(this);
        }

        public override MetaField DeepClone()
        {
            return new CommentData(_name, _text);
        }

        public override bool HasChanged
        {
            get { return false; }
        }

        public override void Reset()
        {
        }

        public override void KeepChanges()
        {
        }
    }
}
