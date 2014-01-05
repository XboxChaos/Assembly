namespace Atlas.Models
{
	/// <summary>
	/// Model for storing and displaying avaiable editors in the Tag Heirarchy.
	/// </summary>
	public class CacheEditorNode : Base
	{
		private string _name;
		private string _description;
		private CacheEditorType _type;

		public CacheEditorNode(CacheEditorType type)
		{
			Type = type;

			switch (type)
			{
				case CacheEditorType.Model:
					Name = "Model Editor";
					Description = "Edits Models yo";
					break;

				case CacheEditorType.Sound:
					Name = "Sound Editor";
					Description = "Edits Sounds yo";
					break;
			}
		}

		public string Name
		{
			get { return _name; }
			private set { SetField(ref _name, value); }
		}

		public string Description
		{
			get { return _description; }
			private set { SetField(ref _description, value); }
		}

		public CacheEditorType Type
		{
			get { return _type; }
			private set { SetField(ref _type, value); }
		}
	}

	public enum CacheEditorType
	{
		Sound,
		Model
	}
}
