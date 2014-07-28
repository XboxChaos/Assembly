using System;
using System.Windows.Input;

namespace Atlas.Models.Cache
{
	public class RecentEditor
		: Base
	{
		public string TagPath
		{
			get { return _tagPath; }
			set { SetField(ref _tagPath, value); }
		}
		private string _tagPath;

		public string InternalName
		{
			get { return _internalName; }
			set { SetField(ref _internalName, value); }
		}
		private string _internalName;

		public string EditorName
		{
			get { return _editorName; }
			set { SetField(ref _editorName, value); }
		}
		private string _editorName;

		public object EditorParamater
		{
			get { return _editorParamater; }
			set { SetField(ref _editorParamater, value); }
		}
		private object _editorParamater;

		public ICommand OpenCommand { get; set; }

		public string FriendlyName
		{
			get { return String.Format("{0} - {1}:\\\\{2}", EditorName, InternalName, TagPath); }
		}
	}
}
