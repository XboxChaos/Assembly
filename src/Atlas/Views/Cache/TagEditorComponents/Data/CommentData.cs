namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	public class CommentData : TagDataField
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
				OnPropertyChanged("Name");
			}
		}

		public string Text
		{
			get { return _text; }
			set
			{
				_text = value;
				OnPropertyChanged("Text");
			}
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitComment(this);
		}

		public override TagDataField CloneValue()
		{
			return new CommentData(_name, _text, PluginLine);
		}
	}
}