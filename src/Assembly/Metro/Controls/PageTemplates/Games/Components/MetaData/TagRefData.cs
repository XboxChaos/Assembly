using System.Windows;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public class TagRefData : ValueField
	{
		private readonly TagHierarchy _allTags;
		private readonly bool _withClass;
		private TagClass _class;
		private Visibility _showTagOptions;
		private TagEntry _value;

		public TagRefData(string name, uint offset, long address, TagHierarchy allTags, Visibility showTagOptions, bool withClass,
			uint pluginLine)
			: base(name, offset, address, pluginLine)
		{
			_allTags = allTags;
			_withClass = withClass;
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

		public TagClass Class
		{
			get { return _class; }
			set
			{
				_class = value;
				NotifyPropertyChanged("Class");
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
			var result = new TagRefData(Name, Offset, FieldAddress, _allTags, _showTagOptions, _withClass, base.PluginLine);
			result.Class = _class;
			result.Value = _value;
			return result;
		}
	}
}