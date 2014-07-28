using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Atlas.ViewModels;
using Atlas.ViewModels.Cache;
using Blamite.Blam.Scripting;

namespace Atlas.Views.Cache
{
	/// <summary>
	/// Interaction logic for ScriptEditor.xaml
	/// </summary>
	public partial class ScriptEditor : ICacheEditor
	{
		public ScriptEditorViewModel ViewModel { get; private set; }

		public string EditorTitle
		{
			get { return _editorTitle; }
			private set { SetField(ref _editorTitle, value); }
		}
		private string _editorTitle;
		private const string EditorFormat = "Script - {0}";

		public bool IsSingleInstance { get { return true; } }

		public ScriptEditor(CachePageViewModel cachePageViewModel, IScriptFile scriptFile)
		{
			InitializeComponent();

			DataContext = ViewModel = new ScriptEditorViewModel(cachePageViewModel, scriptFile);
			EditorTitle = String.Format(EditorFormat, scriptFile.Name);
		}

		public bool Close()
		{
			App.Storage.HomeWindowViewModel.AddRecentEditor(ViewModel.CachePageViewModel, this, ViewModel.ScriptFile, ViewModel.ScriptText);
			return true;
		}

		#region Inpc Helpers

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		public virtual bool SetField<T>(ref T field, T value,
			[CallerMemberName] string propertyName = "")
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		#endregion
	}
}
