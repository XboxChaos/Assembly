using System.Collections.Generic;
using System.Windows;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public class TagRefData : ValueField
	{
		private readonly TagHierarchy _allTags;
		private readonly bool _withGroup;
		private TagGroup _group;
		private bool _showButtons;
		private TagEntry _value;

		public TagRefData(string name, uint offset, long address, TagHierarchy allTags, bool showButtons, bool withGroup,
			uint pluginLine, string tooltip)
			: base(name, offset, address, pluginLine, tooltip)
		{
			_allTags = allTags;
			_withGroup = withGroup;
			_showButtons = showButtons;
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

		public bool ShowButtons
		{
			get { return _showButtons; }
			set
			{
				_showButtons = value;
				NotifyPropertyChanged("ShowButtons");
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
			var result = new TagRefData(Name, Offset, FieldAddress, _allTags, _showButtons, _withGroup, PluginLine, ToolTip);
			result.Group = _group;
			result.Value = _value;
			return result;
		}

		public override string AsString()
		{
			return string.Format("tagref | {0} | {1} {2}", Name, Value?.GroupName ?? "null", Value?.TagFileName ?? "null");
		}

		public override object GetAsJson()
		{
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict["GroupName"] = Value?.GroupName ?? "NONE";
			dict["Path"] = Value?.TagFileName ?? "";

			return dict;
		}
	}
}