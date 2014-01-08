using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Atlas.Models;
using Blamite.Flexibility;
using Blamite.Flexibility.Settings;
using Newtonsoft.Json;

namespace Atlas.Helpers
{
	public class Settings : Base
	{
		private EngineDatabase _defaultDatabase = XMLEngineDatabaseLoader.LoadDatabase("Formats/Engines.xml");

		private TagSort _cacheEditorTagSortMethod = TagSort.PathHierarchy;
		private bool _showPluginInformation = true;
		private string _xsdPath = "";

		[JsonIgnore]
		public bool Loaded { get; set; }

		#region Misc

		/// <summary>
		///     
		/// </summary>
		[JsonIgnore]
		public EngineDatabase DefaultDatabase
		{
			get { return _defaultDatabase; }
			set { SetField(ref _defaultDatabase, value); }
		}

		#endregion

		#region Cache Editor

		public TagSort CacheEditorTagSortMethod
		{
			get { return _cacheEditorTagSortMethod; }
			set { SetField(ref _cacheEditorTagSortMethod, value); }
		}

		public bool ShowPluginInformation
		{
			get { return _showPluginInformation; }
			set { SetField(ref _showPluginInformation, value); }
		}

		public string XsdPath
		{
			get { return _xsdPath; }
			set { SetField(ref _xsdPath, value); }
		}

		#endregion

		#region Enums

		public enum TagSort
		{
			TagClass,
			PathHierarchy
		}

		#endregion

		public override bool SetField<T>(ref T field, T value, 
			[CallerMemberName] string propertyName = "")
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}
	}
}
