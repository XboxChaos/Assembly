using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Atlas.Pages.CacheEditors.TagEditorComponents.Data
{
	public class WrappedReflexiveEntry : MetaField
	{
		private readonly int _index;
		private readonly ObservableCollection<MetaField> _visibleItems;
		private readonly double _width;
		private bool _last;
		private MetaField _wrappedField;

		public WrappedReflexiveEntry(ObservableCollection<MetaField> visibleItems, int index, double width, bool last)
		{
			_index = index;
			_width = width;
			_last = last;
			_wrappedField = visibleItems[index];
			_visibleItems = visibleItems;
			visibleItems.CollectionChanged += visibleItems_CollectionChanged;
		}

		public MetaField WrappedField
		{
			get { return _wrappedField; }
			private set
			{
				_wrappedField = value;
				OnPropertyChanged("WrappedField");
			}
		}

		public bool IsLast
		{
			get { return _last; }
			set
			{
				_last = value;
				OnPropertyChanged("IsLast");
			}
		}

		public double Width
		{
			get { return _width; }
		}

		private void visibleItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			// This is necessary in order for the OnPropertyChanged on WrappedField to work
			if ((e.Action == NotifyCollectionChangedAction.Replace || e.Action == NotifyCollectionChangedAction.Add) &&
			    e.NewStartingIndex == _index)
				WrappedField = (MetaField) e.NewItems[0];
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitReflexiveEntry(this);
		}

		public override MetaField CloneValue()
		{
			return new WrappedReflexiveEntry(_visibleItems, _index, _width, _last);
		}
	}
}