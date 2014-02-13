using Atlas.Views.Cache.TagEditorComponents.Data;
using Blamite.Util;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	public class StringIDData : ValueField
	{
		private Trie _autocompleteTrie;
		private string _value;

		public StringIDData(string name, uint offset, uint address, string val, Trie autocompleteTrie, uint pluginLine)
			: base(name, offset, address, pluginLine)
		{
			_value = val;
			_autocompleteTrie = autocompleteTrie;
		}

		public string Value
		{
			get { return _value; }
			set
			{
				_value = value;
				OnPropertyChanged("Value");
			}
		}

		public Trie AutocompleteTrie
		{
			get { return _autocompleteTrie; }
			set
			{
				_autocompleteTrie = value;
				OnPropertyChanged("AutocompleteTrie");
			}
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitStringID(this);
		}

		public override TagDataField CloneValue()
		{
			return new StringIDData(Name, Offset, FieldAddress, _value, _autocompleteTrie, base.PluginLine);
		}
	}
}