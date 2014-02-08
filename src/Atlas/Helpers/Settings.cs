using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Atlas.Models;
using Blamite.Flexibility;
using Blamite.Flexibility.Settings;
using Newtonsoft.Json;

namespace Atlas.Helpers
{
	public class Settings : INotifyPropertyChanged
	{
		public Settings()
		{
			RecentFiles.CollectionChanged +=
				(sender, args) => SetFieldExplicit(ref _recentFiles, sender as ObservableCollection<RecentFile>, "RecentFiles", true);
		}

		// Misc
		private EngineDatabase _defaultDatabase = XMLEngineDatabaseLoader.LoadDatabase("Formats/Engines.xml");
		private ObservableCollection<RecentFile> _recentFiles = new ObservableCollection<RecentFile>(); 

		// UI
		private Accent _assemblyAccent = Accent.Blue;

		// Cache Editor
		private TagSort _cacheEditorTagSortMethod = TagSort.PathHierarchy;
		private bool _tagEditorShowBlockInformation = true;
		private bool _tagEditorShowEnumIndex = true;
		private bool _tagEditorShowInvisibles;
		private bool _tagEditorShowComments = true;
		private string _xsdPath = "";
		private GridLength _tagEditorGridLength = new GridLength(0.7, GridUnitType.Star);
		private GridLength _tagEditorPluginGridLength = new GridLength(0.3, GridUnitType.Star);

		// Xbox 360 XDK
		private string _xdkIpAddress = "192.168.1.86";

		[JsonIgnore]
		public bool Loaded { get; set; }

		[JsonIgnore]
		public bool Changed { get; set; }


		#region User Interface

		public Accent AssemblyAccent
		{
			get { return _assemblyAccent; }
			set
			{
				SetField(ref _assemblyAccent, value);

				// set accent
				var accent =
					CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Enum.Parse(typeof (Accent), _assemblyAccent.ToString()).ToString());

				try
				{
					Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
					{
						Source = new Uri("/Atlas;component/Metro/Accents/" + accent + ".xaml", UriKind.Relative)
					});
				}
				catch
				{
					Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
					{
						Source = new Uri("/Assembly;component/Metro/Accents/Blue.xaml", UriKind.Relative)
					});
				}
			}
		}

		#endregion

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

		/// <summary>
		/// 
		/// </summary>
		public ObservableCollection<RecentFile> RecentFiles
		{
			get { return _recentFiles; }
			set { SetField(ref _recentFiles, value); }
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

		public GridLength TagEditorGridLength
		{
			get { return _tagEditorGridLength; }
			set { SetField(ref _tagEditorGridLength, value); }
		}

		public GridLength TagEditorPluginGridLength
		{
			get { return _tagEditorPluginGridLength; }
			set { SetField(ref _tagEditorPluginGridLength, value); }
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

		public enum Accent
		{
			Blue,
			Green,
			Orange,
			Purple
		}

		#endregion

		#region Inpc

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public bool SetField<T>(ref T field, T value,
			[CallerMemberName] string propertyName = "", bool overrideChecks = false)
		{
			return SetFieldExplicit(ref field, value, propertyName, overrideChecks);
		}

		public bool SetFieldExplicit<T>(ref T field, T value, 
			string propertyName, bool overrideChecks)
		{
			if (!overrideChecks)
				if (EqualityComparer<T>.Default.Equals(field, value))
					return false;

			Changed = true;
			field = value;
			OnPropertyChanged(propertyName);

			if (!Loaded)
				return true;

			// Write Changes
			var jsonData = JsonConvert.SerializeObject(this);
			File.WriteAllText(Storage.SettingsPath, jsonData);

			return true;
		}

		#endregion
	}
}
