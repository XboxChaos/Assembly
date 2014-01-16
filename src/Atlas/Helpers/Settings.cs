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
		private bool _tagEditorShowBlockInformation = true;
		private bool _tagEditorShowEnumIndex = true;
		private bool _tagEditorShowInvisibles;
		private bool _tagEditorShowComments = true;
		private string _xsdPath = "";

		private string _xdkIpAddress = "192.168.1.86";

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

		public bool TagEditorShowBlockInformation
		{
			get { return _tagEditorShowBlockInformation; }
			set { SetField(ref _tagEditorShowBlockInformation, value); }
		}

		public bool TagEditorShowEnumIndex
		{
			get { return _tagEditorShowEnumIndex; }
			set { SetField(ref _tagEditorShowEnumIndex, value); }
		}

		public bool TagEditorShowInvisibles
		{
			get { return _tagEditorShowInvisibles; }
			set { SetField(ref _tagEditorShowInvisibles, value); }
		}

		public bool TagEditorShowComments
		{
			get { return _tagEditorShowComments; }
			set { SetField(ref _tagEditorShowComments, value); }
		}

		public string XsdPath
		{
			get { return _xsdPath; }
			set { SetField(ref _xsdPath, value); }
		}

		#endregion

		#region XDK

		public string XdkIpAddress
		{
			get { return _xdkIpAddress; }
			set { SetField(ref _xdkIpAddress, value); }
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
