using System.Windows;
using Atlas.Helpers.Tags;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	public class TagRefData : ValueField
	{
		private readonly TagHierarchy _allTags;
		private readonly bool _withClass;
		private TagHierarchyNode _class;
		private Visibility _showJumpTo;
		private TagHierarchyNode _value;

		public TagRefData(string name, uint offset, uint address, TagHierarchy allTags, Visibility showJumpTo, bool withClass,
			uint pluginLine)
			: base(name, offset, address, pluginLine)
		{
			_allTags = allTags;
			_withClass = withClass;
			_showJumpTo = showJumpTo;
		}

		public TagHierarchyNode Value
		{
			get { return _value; }
			set
			{
				_value = value;
				OnPropertyChanged("Value");
			}
		}

		public Visibility ShowJumpTo
		{
			get { return _showJumpTo; }
			set
			{
				_showJumpTo = value;
				OnPropertyChanged("ShowJumpTo");
			}
		}

		public TagHierarchyNode Class
		{
			get { return _class; }
			set
			{
				_class = value;
				OnPropertyChanged("Class");
			}
		}

		public bool WithClass
		{
			get { return _withClass; }
		}

		public TagHierarchy Tags
		{
			get { return _allTags; }
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitTagRef(this);
		}

		public override TagDataField CloneValue()
		{
			var result = new TagRefData(Name, Offset, FieldAddress, _allTags, _showJumpTo, _withClass, PluginLine)
			{
				Class = _class,
				Value = _value
			};
			return result;
		}
	}
}