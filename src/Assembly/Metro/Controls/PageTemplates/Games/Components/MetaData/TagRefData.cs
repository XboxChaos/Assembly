using System.Windows;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public class TagRefData : ValueField
	{
		private readonly TagHierarchy _allTags;
		private readonly bool _withGroup;
		private TagGroup _group;
		private Visibility _showTagOptions;
		private TagEntry _value;

		public TagRefData(string name, uint offset, long address, TagHierarchy allTags, Visibility showTagOptions, bool withGroup,
			uint pluginLine)
			: base(name, offset, address, pluginLine)
		{
			_allTags = allTags;
			_withGroup = withGroup;
			_showTagOptions = showTagOptions;
		}

		public TagEntry Value
		{
			get { return _value; }
			set
			{
				_value = value;
				NotifyPropertyChanged("Value");
			}
		}

		public Visibility ShowTagOptions
		{
			get { return _showTagOptions; }
			set
			{
				_showTagOptions = value;
				NotifyPropertyChanged("ShowTagOptions");
			}
		}

		public TagGroup Group
		{
			get { return _group; }
			set
			{
				_group = value;
				NotifyPropertyChanged("Group");
			}
		}

		public bool WithGroup
		{
			get { return _withGroup; }
		}

		public TagHierarchy Tags
		{
			get { return _allTags; }
		}

		public bool CanJump
		{
			get { return _value != null && !_value.IsNull; }
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitTagRef(this);
		}

		public override MetaField CloneValue()
		{
			var result = new TagRefData(Name, Offset, FieldAddress, _allTags, _showTagOptions, _withGroup, base.PluginLine);
			result.Group = _group;
			result.Value = _value;
			return result;
		}
	}
}