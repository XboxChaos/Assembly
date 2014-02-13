using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	public class WrappedTagBlockEntry : TagDataField
	{
		private readonly int _index;
		private readonly ObservableCollection<TagDataField> _visibleItems;
		private readonly double _width;
		private bool _last;
		private TagDataField _wrappedField;

		public WrappedTagBlockEntry(ObservableCollection<TagDataField> visibleItems, int index, double width, bool last)
		{
			_index = index;
			_width = width;
			_last = last;
			_wrappedField = visibleItems[index];
			_visibleItems = visibleItems;
			visibleItems.CollectionChanged += visibleItems_CollectionChanged;
		}

		public TagDataField WrappedField
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
				WrappedField = (TagDataField) e.NewItems[0];
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitReflexiveEntry(this);
		}

		public override TagDataField CloneValue()
		{
			return new WrappedTagBlockEntry(_visibleItems, _index, _width, _last);
		}
	}
}