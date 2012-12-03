using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public class WrappedReflexiveEntry : MetaField
    {
        private ObservableCollection<MetaField> _visibleItems;
        private double _width;
        private int _index;
        private MetaField _wrappedField;
        private bool _last;

        public WrappedReflexiveEntry(ObservableCollection<MetaField> visibleItems, int index, double width, bool last)
        {
            _index = index;
            _width = width;
            _last = last;
            _wrappedField = visibleItems[index];
            _visibleItems = visibleItems;
            visibleItems.CollectionChanged += visibleItems_CollectionChanged;
        }

        void visibleItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if ((e.Action == NotifyCollectionChangedAction.Replace || e.Action == NotifyCollectionChangedAction.Add) && e.NewStartingIndex == _index)
                WrappedField = (MetaField)e.NewItems[0];
        }

        public MetaField WrappedField
        {
            get { return _wrappedField; }
            private set { _wrappedField = value; NotifyPropertyChanged("WrappedField"); }
        }

        public bool IsLast
        {
            get { return _last; }
            set { _last = value; NotifyPropertyChanged("IsLast"); }
        }

        public double Width
        {
            get { return _width; }
        }

        public override void Accept(IMetaFieldVisitor visitor)
        {
            visitor.VisitReflexiveEntry(this);
        }

        public override MetaField DeepClone()
        {
            return new WrappedReflexiveEntry(_visibleItems, _index, _width, _last);
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
