namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public class CommentData : MetaField
	{
		private string _name, _text;

		public CommentData(string name, string text, uint pluginLine)
		{
			_name = name;
			_text = text;
			PluginLine = pluginLine;
		}

		public string Name
		{
			get { return _name; }
			set
			{
				_name = value;
				NotifyPropertyChanged("Name");
			}
		}

		public string Text
		{
			get { return _text; }
			set
			{
				_text = value;
				NotifyPropertyChanged("Text");
			}
		}

		public bool TextExists
		{
			get { return !string.IsNullOrEmpty(_text); }
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitComment(this);
		}

		public override MetaField CloneValue()
		{
			return new CommentData(_name, _text, PluginLine);
		}

		public override string AsString()
		{
			return string.Format("comment | {0} ", Name);
		}
	}
}