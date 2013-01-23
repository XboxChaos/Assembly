using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public class ReflexivePage : PropertyChangeNotifier
    {
        private int _index;
        private MetaField[] _fields;

        public ReflexivePage(int index, int size)
        {
            _index = index;
            _fields = new MetaField[size];
        }

        public int Index
        {
            get { return _index; }
            set { _index = value; NotifyPropertyChanged("Index"); }
        }

        public void CloneChanges(ObservableCollection<MetaField> changedFields, FieldChangeTracker tracker, FieldChangeSet changes)
        {
            for (int i = 0; i < changedFields.Count; i++)
            {
                MetaField field = changedFields[i];
                ReflexiveData reflexive = field as ReflexiveData;
                if (field != null && (changes.HasChanged(field) || reflexive != null))
                {
                    if (_fields[i] == null)
                    {
                        _fields[i] = field.CloneValue();
                        tracker.AttachTo(_fields[i]);
                        tracker.MarkUnchanged(field);
                        tracker.MarkChanged(_fields[i]);

                        if (reflexive != null)
                        {
                            foreach (ReflexivePage page in reflexive.Pages)
                                page.Reset();
                        }
                    }
                    else if (field != _fields[i])
                    {
                        throw new InvalidOperationException("Cannot cache changes in a meta field from a different page");
                    }
                }
            }
        }

        public void Reset()
        {
            for (int i = 0; i < _fields.Length; i++)
                _fields[i] = null;
        }

        public ReflexivePage CloneValue()
        {
            ReflexivePage result = new ReflexivePage(_index, _fields.Length);
            Array.Copy(_fields, result._fields, _fields.Length);
            return result;
        }

        public MetaField[] Fields
        {
            get { return _fields; }
        }
    }

    public class ReflexiveClonedEventArgs : EventArgs
    {
        public ReflexiveClonedEventArgs(ReflexiveData old, ReflexiveData clone)
        {
            Old = old;
            Clone = clone;
        }

        public ReflexiveData Old { get; private set; }
        public ReflexiveData Clone { get; private set; }
    }

    public class ReflexiveData : ValueField
    {
        private uint _entrySize;
        private uint _firstEntryOffset = 0;
        private int _currentIndex = 0;
        private bool _expanded = false;
        private double _width = MinWidth;
        private ObservableCollection<ReflexivePage> _pages = new ObservableCollection<ReflexivePage>();
        private ObservableCollection<MetaField> _template = new ObservableCollection<MetaField>();

        private const double MinWidth = 500;

        public ReflexiveData(string name, uint offset, uint entrySize, uint pluginLine)
            : base(name, offset, pluginLine)
        {
            _entrySize = entrySize;
            _expanded = true;
        }

        public void UpdateWidth()
        {
            WidthCalculator calc = new WidthCalculator();
            calc.Add(Template);
            _width = Math.Max(MinWidth, calc.TotalWidth);
            NotifyPropertyChanged("Width");
        }

        public double Width
        {
            get { return _width + 22; }
        }

        public int Length
        {
            get { return _pages.Count; }
            set { Resize(value); }
        }

        public int LastChunkIndex
        {
            get { return _pages.Count - 1; }
        }

        public uint EntrySize
        {
            get { return _entrySize; }
            set { _entrySize = value; NotifyPropertyChanged("EntrySize"); }
        }

        public uint FirstEntryOffset
        {
            get { return _firstEntryOffset; }
            set { _firstEntryOffset = value; NotifyPropertyChanged("FirstEntryOffset"); }
        }

        public int CurrentIndex
        {
            get { return _currentIndex; }
            set
            {
                _currentIndex = value;
                NotifyPropertyChanged("CurrentIndex");
                NotifyPropertyChanged("HasChildren");
            }
        }

        public bool HasChildren
        {
            get { return Pages.Count > 0; }
        }

        public bool IsExpanded
        {
            get { return _expanded; }
            set { _expanded = value; NotifyPropertyChanged("IsExpanded"); }
        }

        public ObservableCollection<ReflexivePage> Pages
        {
            get { return _pages; }
        }

        public ObservableCollection<MetaField> Template
        {
            get { return _template; }
        }

        public override void Accept(IMetaFieldVisitor visitor)
        {
            visitor.VisitReflexive(this);
        }

        public event EventHandler<ReflexiveClonedEventArgs> Cloned;

        public override MetaField CloneValue()
        {
            ReflexiveData result = new ReflexiveData(Name, Offset, EntrySize, base.PluginLine);
            result._expanded = _expanded;
            result._width = _width;
            result._currentIndex = _currentIndex;
            result._firstEntryOffset = _firstEntryOffset;
            foreach (MetaField field in _template)
                result._template.Add(field);
            foreach (ReflexivePage page in _pages)
                result._pages.Add(page.CloneValue());
            if (Cloned != null)
                Cloned(this, new ReflexiveClonedEventArgs(this, result));
            return result;
        }

        /// <summary>
        /// Changes the size of the reflexive, adding or removing pages as necessary.
        /// </summary>
        /// <param name="count">The new size of the reflexive.</param>
        private void Resize(int count)
        {
            if (count == _pages.Count && count > 0)
                return;

            if (count <= 0)
            {
                CurrentIndex = -1;
                _pages.Clear();
                IsExpanded = false;
            }
            else if (count < _pages.Count)
            {
                // Remove pages from the end
                CurrentIndex = Math.Min(CurrentIndex, count - 1);
                for (int i = _pages.Count - 1; i >= count; i--)
                    _pages.RemoveAt(i);
            }
            else
            {
                // Add new pages
                for (int i = _pages.Count; i < count; i++)
                    _pages.Add(new ReflexivePage(i, _template.Count));
                if (CurrentIndex < 0)
                    CurrentIndex = 0;
            }
            NotifyPropertyChanged("Length");
            NotifyPropertyChanged("LastChunkIndex");
            NotifyPropertyChanged("HasChildren");
        }
    }
}
