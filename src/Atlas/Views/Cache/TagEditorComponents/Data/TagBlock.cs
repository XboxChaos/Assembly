using System;
using System.Collections.ObjectModel;
using System.Globalization;
using Atlas.Models;
using Blamite.IO;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	public class TagBlockPage : Base
	{
		private readonly TagDataField[] _fields;
		private int _index;

		public TagBlockPage(int index, int size)
		{
			_index = index;
			_fields = new TagDataField[size];
		}

		public int Index
		{
			get { return _index; }
			set
			{
				_index = value;
				OnPropertyChanged("Index");
			}
		}

		public TagDataField[] Fields
		{
			get { return _fields; }
		}

		public void CloneChanges(ObservableCollection<TagDataField> changedFields, FieldChangeTracker tracker,
			FieldChangeSet changes)
		{
			for (int i = 0; i < changedFields.Count; i++)
			{
				TagDataField field = changedFields[i];
				var tagBlock = field as TagBlockData;
				bool changed = changes.HasChanged(field);
				if (field != null && (changed || tagBlock != null))
				{
					if (_fields[i] == null)
					{
						_fields[i] = field.CloneValue();
						tracker.AttachTo(_fields[i]);
						if (changed)
							tracker.MarkChanged(_fields[i]);
						tracker.MarkUnchanged(field);

						if (tagBlock != null)
							tagBlock.ResetPages();
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

		public TagBlockPage CloneValue()
		{
			var result = new TagBlockPage(_index, _fields.Length);
			Array.Copy(_fields, result._fields, _fields.Length);
			return result;
		}
	}

	public class TagBlockClonedEventArgs : EventArgs
	{
		public TagBlockClonedEventArgs(TagBlockData old, TagBlockData clone)
		{
			Old = old;
			Clone = clone;
		}

		public TagBlockData Old { get; private set; }
		public TagBlockData Clone { get; private set; }
	}

	public class TagBlockData : ValueField
	{
		private const double MinWidth = 525; // The minimum width that a reflexive can have
		private readonly FileSegmentGroup _metaArea;
		private readonly ObservableCollection<TagBlockPage> _pages = new ObservableCollection<TagBlockPage>();
		private readonly ObservableCollection<TagDataField> _template = new ObservableCollection<TagDataField>();
		private int _currentIndex;
		private uint _entrySize;
		private bool _expanded;
		private uint _firstEntryAddr;
		private double _width = MinWidth;

		public TagBlockData(string name, uint offset, uint address, uint entrySize, uint pluginLine,
			FileSegmentGroup metaArea)
			: base(name, offset, address, pluginLine)
		{
			_entrySize = entrySize;
			_metaArea = metaArea;
			_expanded = true;
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
			set
			{
				_entrySize = value;
				OnPropertyChanged("EntrySize");
			}
		}

		public uint FirstEntryAddress
		{
			get { return _firstEntryAddr; }
			set
			{
				if (value != 0 && !_metaArea.ContainsPointer(value))
					throw new ArgumentException("Invalid pointer");

				_firstEntryAddr = value;
				OnPropertyChanged("FirstEntryAddress");
				OnPropertyChanged("FirstEntryAddressHex");
			}
		}

		public string FirstEntryAddressHex
		{
			get { return "0x" + FirstEntryAddress.ToString("X"); }
			set
			{
				if (value.StartsWith("0x"))
					value = value.Substring(2);
				FirstEntryAddress = uint.Parse(value, NumberStyles.HexNumber);
			}
		}

		public int CurrentIndex
		{
			get { return _currentIndex; }
			set
			{
				_currentIndex = value;
				OnPropertyChanged("CurrentIndex");
				OnPropertyChanged("HasChildren");
			}
		}

		public bool HasChildren
		{
			get { return (Pages.Count > 0 && Template.Count > 0); }
		}

		public bool IsExpanded
		{
			get { return _expanded; }
			set
			{
				_expanded = value;
				OnPropertyChanged("IsExpanded");
			}
		}

		public ObservableCollection<TagBlockPage> Pages
		{
			get { return _pages; }
		}

		public ObservableCollection<TagDataField> Template
		{
			get { return _template; }
		}

		/// <summary>
		///     Recalculates the tag block's width.
		/// </summary>
		public void UpdateWidth()
		{
			var calc = new WidthCalculator();
			calc.Add(Template);
			_width = Math.Max(MinWidth, calc.TotalWidth);
			OnPropertyChanged("Width");
		}

		/// <summary>
		///     Throws out any fields that have been cached by the tag blocks.
		/// </summary>
		public void ResetPages()
		{
			foreach (var page in _pages)
				page.Reset();
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitReflexive(this);
		}

		public event EventHandler<TagBlockClonedEventArgs> Cloned;

		public override TagDataField CloneValue()
		{
			var result = new TagBlockData(Name, Offset, FieldAddress, EntrySize, PluginLine, _metaArea)
			{
				_expanded = _expanded,
				_width = _width,
				_currentIndex = _currentIndex,
				_firstEntryAddr = _firstEntryAddr
			};
			foreach (var field in _template)
				result._template.Add(field);
			foreach (var page in _pages)
				result._pages.Add(page.CloneValue());
			if (Cloned != null)
				Cloned(this, new TagBlockClonedEventArgs(this, result));
			return result;
		}

		/// <summary>
		///     Changes the size of the reflexive, adding or removing pages as necessary.
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
					_pages.Add(new TagBlockPage(i, _template.Count));
				if (CurrentIndex < 0)
					CurrentIndex = 0;
			}
			OnPropertyChanged("Length");
			OnPropertyChanged("LastChunkIndex");
			OnPropertyChanged("HasChildren");
		}
	}
}