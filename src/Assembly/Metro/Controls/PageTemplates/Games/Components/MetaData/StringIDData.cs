using Blamite.Util;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public class StringIDData : ValueField
	{
		protected Trie _autocompleteTrie;
		protected string _value;
		protected string _type;

		public StringIDData(string name, uint offset, long address, string val, Trie autocompleteTrie, uint pluginLine, string tooltip)
			: base(name, offset, address, pluginLine, tooltip)
		{
			_value = val;
			_autocompleteTrie = autocompleteTrie;
			_type = "stringid";
		}

		public string Type
		{
			get { return _type; }
			set
			{
				_type = value;
				NotifyPropertyChanged("Type");
			}
		}

		public string Value
		{
			get { return _value; }
			set
			{
				_value = value;
				NotifyPropertyChanged("Value");
			}
		}

		public Trie AutocompleteTrie
		{
			get { return _autocompleteTrie; }
			set
			{
				_autocompleteTrie = value;
				NotifyPropertyChanged("AutocompleteTrie");
			}
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitStringID(this);
		}

		public override MetaField CloneValue()
		{
			return new StringIDData(Name, Offset, FieldAddress, _value, _autocompleteTrie, PluginLine, ToolTip);
		}

		public override string AsString()
		{
			return string.Format("stringid | {0} | {1}", Name, Value);
		}

		public override object GetAsJson()
		{
			return Value;
		}
	}

	public class OldStringIDData : StringIDData
	{
		public OldStringIDData(string name, uint offset, long address, string val, Trie autocompleteTrie, uint pluginLine, string tooltip)
			: base(name, offset, address, val, autocompleteTrie, pluginLine, tooltip)
		{
			_type = "oldstringid";
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitOldStringID(this);
		}

		public override MetaField CloneValue()
		{
			return new OldStringIDData(Name, Offset, FieldAddress, _value, _autocompleteTrie, PluginLine, ToolTip);
		}

		public override string AsString()
		{
			return string.Format("oldstringid | {0} | {1}", Name, Value);
		}
	}
}