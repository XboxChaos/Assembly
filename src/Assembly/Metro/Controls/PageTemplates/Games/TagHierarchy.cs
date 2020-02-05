using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Blamite.Blam;

namespace Assembly.Metro.Controls.PageTemplates.Games
{
	public class TagHierarchy : INotifyPropertyChanged
	{
		private ObservableCollection<TagClass> _classes = new ObservableCollection<TagClass>();

		private List<TagEntry> _entries = new List<TagEntry>();

		public ObservableCollection<TagClass> Classes
		{
			get { return _classes; }
			set
			{
				_classes = value;
				NotifyPropertyChanged("Classes");
			}
		}

		public List<TagEntry> Entries
		{
			get { return _entries; }
			set
			{
				_entries = value;
				NotifyPropertyChanged("Entries");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
	}

	public class TagClass : INotifyPropertyChanged
	{
		private List<TagEntry> _children = new List<TagEntry>();
		private string _description = string.Empty;
		private ITagClass _rawClass;
		private string _tagClassMagic = string.Empty;
		private TagEntry _null = new TagEntry(null, null, "(null)");

		public TagClass(ITagClass baseClass, string name, string description)
		{
			RawClass = baseClass;
			TagClassMagic = name;
			Description = description;
			Children = new List<TagEntry>();
		}

		public string TagClassMagic
		{
			get { return _tagClassMagic; }
			set
			{
				_tagClassMagic = value;
				NotifyPropertyChanged("TagClassMagic");
			}
		}

		public string Description
		{
			get { return _description; }
			set
			{
				_description = value;
				NotifyPropertyChanged("Description");
			}
		}

		public ITagClass RawClass
		{
			get { return _rawClass; }
			set
			{
				_rawClass = value;
				NotifyPropertyChanged("RawClass");
			}
		}

		public List<TagEntry> Children
		{
			get { return _children; }
			internal set
			{
				_children = value;
				NotifyPropertyChanged("Children");
			}
		}

		public TagEntry NullTag
		{
			get { return _null; }
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
	}

	public class TagEntry : INotifyPropertyChanged
	{
		private string _className = string.Empty;
		private bool _isBookmark;
		private ITag _rawTag;
		private string _tagFileName = string.Empty;
		private bool _isNull;

		private string _tagToolTip = null;

		public TagEntry(ITag baseTag, string className, string name)
		{
			RawTag = baseTag;
			ClassName = className;
			TagFileName = name;

			if (baseTag != null)
			{
				if (baseTag.MetaLocation == null)
				{
					_isNull = true;
					TagToolTip = string.Format("{0}\r\nDatum Index: {1}\r\nThis tag refers to a shared cache.\r\nIt cannot be modified directly, only referenced.", _tagFileName, baseTag.Index);
				}
				else
				{
					TagToolTip = string.Format("{0}\r\nDatum Index: {1}\r\nMemory Address: 0x{2:X8}\r\nFile Offset: 0x{3:X}", _tagFileName, baseTag.Index, baseTag.MetaLocation.AsPointer(), baseTag.MetaLocation.AsOffset());
				}
			}
		}

		public bool IsNull
		{
			get { return _isNull; }
		}

		public bool IsBookmark
		{
			get { return _isBookmark; }
			internal set
			{
				_isBookmark = value;
				NotifyPropertyChanged("IsBookmark");
			}
		}

		public string ClassName
		{
			get { return _className; }
			internal set
			{
				_className = value;
				NotifyPropertyChanged("ClassName");
			}
		}

		public string TagFileName
		{
			get { return _tagFileName; }
			internal set
			{
				_tagFileName = value;
				NotifyPropertyChanged("TagFileName");
			}
		}

		public string TagToolTip
		{
			get { return _tagToolTip; }
			internal set
			{
				_tagToolTip = value;
				NotifyPropertyChanged("TagToolTip");
			}
		}

		public ITag RawTag
		{
			get { return _rawTag; }
			internal set
			{
				_rawTag = value;
				NotifyPropertyChanged("RawTag");
			}
		}

		public override string ToString()
		{
			return TagFileName;
		}
		
		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
	}
}