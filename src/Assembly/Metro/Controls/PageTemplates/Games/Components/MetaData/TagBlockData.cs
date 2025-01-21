using System;
using System.Collections.ObjectModel;
using System.Globalization;
using Assembly.Helpers;
using Blamite.IO;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public class TagBlockPage : PropertyChangeNotifier
	{
		private readonly MetaField[] _fields;
		private int _index;

		public TagBlockPage(int index, int size)
		{
			_index = index;
			_fields = new MetaField[size];
		}

		public int Index
		{
			get { return _index; }
			set
			{
				_index = value;
				NotifyPropertyChanged("Index");
			}
		}

		public MetaField[] Fields
		{
			get { return _fields; }
		}

		public void CloneChanges(ObservableCollection<MetaField> changedFields, FieldChangeTracker tracker,
			FieldChangeSet changes)
		{
			for (int i = 0; i < changedFields.Count; i++)
			{
				MetaField field = changedFields[i];
				var block = field as TagBlockData;
				bool changed = changes.HasChanged(field);
				if (field != null && (changed || block != null))
				{
					if (_fields[i] == null)
					{
						_fields[i] = field.CloneValue();
						tracker.AttachTo(_fields[i]);
						if (changed)
							tracker.MarkChanged(_fields[i]);
						tracker.MarkUnchanged(field);

						if (block != null)
							block.ResetPages();
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
		private const double MinWidth = 602; // The minimum width that a block can have
		private readonly FileSegmentGroup _metaArea;
		private readonly ObservableCollection<TagBlockPage> _pages = new ObservableCollection<TagBlockPage>();
		private readonly ObservableCollection<MetaField> _template = new ObservableCollection<MetaField>();
		private int _currentIndex;
		private uint _elementSize;
		private bool _expanded;
		private long _firstElementAddr;
		private double _width = MinWidth;
		private TagDataCommandState _tagCommandState;
		private static string _allocateTooltip = "Opens the Tag Block Reallocator tool to properly add or remove elements for this block.";
		private static string _isolateTooltip = "Isolates this block from other shared instances by copying to a new address.";

		public TagBlockData(string name, uint offset, long address, uint elementSize, int align,
			bool sort, uint pluginLine, string tooltip, FileSegmentGroup metaArea, TagDataCommandState tagCommandState)
			: base(name, offset, address, pluginLine, tooltip)
		{
			_elementSize = elementSize;
			_metaArea = metaArea;
			_expanded = true;
			Align = align;
			Sort = sort;
			_tagCommandState = tagCommandState;
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

		public int LastElementIndex
		{
			get { return _pages.Count - 1; }
		}

		public uint ElementSize
		{
			get { return _elementSize; }
			set
			{
				_elementSize = value;
				NotifyPropertyChanged("ElementSize");
			}
		}

		public int Align { get; private set; }
		public bool Sort { get; private set; }

		public long FirstElementAddress
		{
			get { return _firstElementAddr; }
			set
			{
				if (value != 0 && !_metaArea.ContainsPointer(value))
					throw new ArgumentException("Invalid address for this cache file.");

				_firstElementAddr = value;
				NotifyPropertyChanged("FirstElementAddress");
				NotifyPropertyChanged("FirstElementAddressHex");
			}
		}

		public string FirstElementAddressHex
		{
			get { return "0x" + FirstElementAddress.ToString("X"); }
			set
			{
				if (value.StartsWith("0x"))
					value = value.Substring(2);
				FirstElementAddress = long.Parse(value, NumberStyles.HexNumber);
			}
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
			get { return (Pages.Count > 0 && Template.Count > 0); }
		}

		public bool IsExpanded
		{
			get { return _expanded; }
			set
			{
				_expanded = value;
				NotifyPropertyChanged("IsExpanded");
			}
		}

		public ObservableCollection<TagBlockPage> Pages
		{
			get { return _pages; }
		}

		public ObservableCollection<MetaField> Template
		{
			get { return _template; }
		}

		public string AllocateTooltip
		{
			get
			{
				if (_tagCommandState == TagDataCommandState.None)
					return _allocateTooltip;
				else
					return TagDataCommandStateResolver.GetStateDescription(_tagCommandState);
			}
		}

		public string IsolateTooltip
		{
			get
			{
				if (_tagCommandState == TagDataCommandState.None)
					return _isolateTooltip;
				else
					return TagDataCommandStateResolver.GetStateDescription(_tagCommandState);
			}
		}

		/// <summary>
		///     Recalculates the block's width.
		/// </summary>
		public void UpdateWidth()
		{
			var calc = new WidthCalculator();
			calc.Add(Template);
			_width = Math.Max(MinWidth, calc.TotalWidth);
			NotifyPropertyChanged("Width");
		}

		/// <summary>
		///     Throws out any fields that have been cached by the block.
		/// </summary>
		public void ResetPages()
		{
			foreach (TagBlockPage page in _pages)
				page.Reset();
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitTagBlock(this);
		}

		public event EventHandler<TagBlockClonedEventArgs> Cloned;

		public override MetaField CloneValue()
		{
			var result = new TagBlockData(Name, Offset, FieldAddress, ElementSize, Align, Sort, PluginLine, ToolTip, _metaArea, _tagCommandState);
			result._expanded = _expanded;
			result._width = _width;
			result._currentIndex = _currentIndex;
			result._firstElementAddr = _firstElementAddr;
			foreach (MetaField field in _template)
				result._template.Add(field);
			foreach (TagBlockPage page in _pages)
				result._pages.Add(page.CloneValue());
			if (Cloned != null)
				Cloned(this, new TagBlockClonedEventArgs(this, result));
			return result;
		}

		public override string AsString()
		{
			//have to iterate children via metaeditor as that contains the flattener and other such things that allow elements to be read
			return string.Format("block | {0} | [{1}]", Name, Length);
		}

		/// <summary>
		///     Changes the size of the block, adding or removing pages as necessary.
		/// </summary>
		/// <param name="count">The new size of the block.</param>
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
			NotifyPropertyChanged("Length");
			NotifyPropertyChanged("LastElementIndex");
			NotifyPropertyChanged("HasChildren");
		}
	}
}