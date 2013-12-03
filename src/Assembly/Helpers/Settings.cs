﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using Assembly.Metro.Dialogs;
using Assembly.Windows;
using Blamite.Flexibility;
using Blamite.Flexibility.Settings;
using Newtonsoft.Json;
using XBDMCommunicator;

namespace Assembly.Helpers
{
	/// <summary>
	/// </summary>
	public class Storage : INotifyPropertyChanged
	{
		private Settings _assemblySettings = new Settings();

		#region Helpers

		public void Load()
		{
			#region Settings

			// Get File Path
			string jsonString = null;
			if (File.Exists("AssemblySettings.ason"))
				jsonString = File.ReadAllText("AssemblySettings.ason");

			try
			{
				if (jsonString == null)
					_assemblySettings = new Settings();
				else
					_assemblySettings = JsonConvert.DeserializeObject<Settings>(jsonString) ?? new Settings();
			}
			catch (JsonSerializationException)
			{
				_assemblySettings = new Settings();
			}

			// Update Accent
			_assemblySettings.UpdateAssemblyAccent();

			#endregion
		}

		#endregion

		#region Interface

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		protected bool SetField<T>(ref T field, T value, string propertyName)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
				return false;

			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		#endregion

		/// <summary>
		/// </summary>
		public Storage()
		{
			Load();
		}

		/// <summary>
		/// </summary>
		public Settings AssemblySettings
		{
			get { return _assemblySettings; }
			set
			{
				// Set Data
				SetField(ref _assemblySettings, value, "AssemblySettings");

				// Write Changes
				string jsonData = JsonConvert.SerializeObject(value);

				// Get File Path
				File.WriteAllText("AssemblySettings.ason", jsonData);

				// Update Accent
				_assemblySettings.UpdateAssemblyAccent();

				// Update File Defaults
				FileDefaults.UpdateFileDefaults();
			}
		}
	}

	/// <summary>
	/// </summary>
	public class Settings : INotifyPropertyChanged
	{
		private Accents _applicationAccent = Accents.Blue;
		private bool _applicationEasterEggs = true;
		private ObservableCollection<RecentFileEntry> _applicationRecents = new ObservableCollection<RecentFileEntry>();
		private double _applicationSizeHeight = 600;
		private bool _applicationSizeMaximize;
		private double _applicationSizeWidth = 1100;
		private bool _applicationUpdateOnStartup = true;
		private bool _defaultAmp;
		private bool _defaultBlf;
		private EngineDatabase _defaultDatabase = XMLEngineDatabaseLoader.LoadDatabase("Formats/Engines.xml");
		private bool _defaultMap;
		private bool _defaultMif;
		private LastMetaEditorType _halomapLastSelectedMetaEditor = LastMetaEditorType.Info;
		private MapInfoDockSide _halomapMapInfoDockSide = MapInfoDockSide.Left;
		private bool _halomapOnlyShowBookmarkedTags;
		private bool _halomapShowEmptyClasses;
		private TagOpenMode _halomapTagOpenMode = TagOpenMode.NewTab;
		private TagSort _halomapTagSort = TagSort.TagClass;
		private Home _homeWindow;
		private bool _pluginsShowComments = true;
		private bool _pluginsShowInvisibles;
		private bool _startpageHideOnLaunch;
		private bool _startpageShowOnLoad = true;
		private bool _startpageShowRecentsBlf = true;
		private bool _startpageShowRecentsMap = true;
		private bool _startpageShowRecentsMapInfo = true;
		private bool _startpageShowRecentsCampaign = true;
		private Xbdm _xbdm;
		private bool _xdkAutoSave;
		private string _xdkNameIp = "192.168.0.1";
		private bool _xdkResizeImages;
		private int _xdkResizeScreenshotHeight = 1080;
		private int _xdkResizeScreenshotWidth = 1920;
		private bool _xdkScreenshotFreeze = true;
		private bool _xdkScreenshotGammaCorrect = true;
		private double _xdkScreenshotGammaModifier = 0.5;
		private string _xdkScreenshotPath = "";

		#region Enums

		/// <summary>
		/// </summary>
		public enum Accents
		{
			Blue,
			Purple,
			Orange,
			Green
		}

		/// <summary>
		/// </summary>
		public enum LastMetaEditorType
		{
			Info,
			MetaEditor,
			PluginEditor,
			Sound,
			Model,
			Bsp
		}

		/// <summary>
		/// </summary>
		public enum MapInfoDockSide
		{
			Left,
			Right
		}

		/// <summary>
		/// </summary>
		public enum RecentFileType
		{
			Cache,
			MapInfo,
			Blf,
			Campaign
		}

		/// <summary>
		/// </summary>
		public enum TagOpenMode
		{
			NewTab,
			ExistingTab
		}

		/// <summary>
		/// </summary>
		public enum TagSort
		{
			TagClass,
			ObjectHierarchy,
			PathHierarchy
		}

		#endregion

		#region Classes

		/// <summary>
		/// </summary>
		public class RecentFileEntry
		{
			public string FileName { get; set; }
			public RecentFileType FileType { get; set; }
			public string FileGame { get; set; }
			public string FilePath { get; set; }
		}

		#endregion

		#region Methods

		public Settings()
		{
			ApplicationRecents.CollectionChanged += (sender, args) =>
			{
				SetField(ref _applicationRecents, sender as ObservableCollection<RecentFileEntry>, "ApplicationRecents", true);
			};
		}

		#endregion

		#region Interface

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		protected bool SetField<T>(ref T field, T value, string propertyName, bool overrideChecks = false)
		{
			if (!overrideChecks)
				if (EqualityComparer<T>.Default.Equals(field, value))
					return false;

			field = value;
			OnPropertyChanged(propertyName);

			// Write Changes
			var jsonData = JsonConvert.SerializeObject(this);
			File.WriteAllText("AssemblySettings.ason", jsonData);

			return true;
		}

		#endregion

		/// <summary>
		///     The Accent colour the user has selected. Defaults to Assembly Blue.
		/// </summary>
		public Accents ApplicationAccent
		{
			get { return _applicationAccent; }
			set
			{
				SetField(ref _applicationAccent, value, "ApplicationAccent");
				UpdateAssemblyAccent();
			}
		}

		/// <summary>
		///     Wether Assebembly checks for updates at the startup. Defaults to True.
		/// </summary>
		public bool ApplicationUpdateOnStartup
		{
			get { return _applicationUpdateOnStartup; }
			set { SetField(ref _applicationUpdateOnStartup, value, "ApplicationUpdateOnStartup"); }
		}

		/// <summary>
		/// </summary>
		public bool ApplicationEasterEggs
		{
			get { return _applicationEasterEggs; }
			set { SetField(ref _applicationEasterEggs, value, "ApplicationEasterEggs"); }
		}

		/// <summary>
		///     A list of Assembly's recently opened files.
		/// </summary>
		public ObservableCollection<RecentFileEntry> ApplicationRecents
		{
			get { return _applicationRecents; }
			set { SetField(ref _applicationRecents, value, "ApplicationRecents"); }
		}

		/// <summary>
		/// </summary>
		public double ApplicationSizeWidth
		{
			get { return _applicationSizeWidth; }
			set { SetField(ref _applicationSizeWidth, value, "ApplicationSizeWidth"); }
		}

		/// <summary>
		/// </summary>
		public double ApplicationSizeHeight
		{
			get { return _applicationSizeHeight; }
			set { SetField(ref _applicationSizeHeight, value, "ApplicationSizeHeight"); }
		}

		/// <summary>
		/// </summary>
		public bool ApplicationSizeMaximize
		{
			get { return _applicationSizeMaximize; }
			set { SetField(ref _applicationSizeMaximize, value, "ApplicationSizeMaximize"); }
		}

		/// <summary>
		/// </summary>
		public bool StartpageShowOnLoad
		{
			get { return _startpageShowOnLoad; }
			set { SetField(ref _startpageShowOnLoad, value, "StartpageShowOnLoad"); }
		}

		/// <summary>
		/// </summary>
		public bool StartpageHideOnLaunch
		{
			get { return _startpageHideOnLaunch; }
			set { SetField(ref _startpageHideOnLaunch, value, "StartpageHideOnLaunch"); }
		}

		/// <summary>
		/// </summary>
		public bool StartpageShowRecentsMap
		{
			get { return _startpageShowRecentsMap; }
			set { SetField(ref _startpageShowRecentsMap, value, "StartpageShowRecentsMap"); }
		}

		/// <summary>
		/// </summary>
		public bool StartpageShowRecentsBlf
		{
			get { return _startpageShowRecentsBlf; }
			set { SetField(ref _startpageShowRecentsBlf, value, "StartpageShowRecentsBlf"); }
		}

		/// <summary>
		/// </summary>
		public bool StartpageShowRecentsMapInfo
		{
			get { return _startpageShowRecentsMapInfo; }
			set { SetField(ref _startpageShowRecentsMapInfo, value, "StartpageShowRecentsMapInfo"); }
		}

		/// <summary>
		/// </summary>
		public bool StartpageShowRecentsCampaign
		{
			get { return _startpageShowRecentsCampaign; }
			set { SetField(ref _startpageShowRecentsCampaign, value, "StartpageShowRecentsCampaign"); }
		}

		/// <summary>
		/// </summary>
		public string XdkNameIp
		{
			get { return _xdkNameIp; }
			set
			{
				SetField(ref _xdkNameIp, value, "XdkNameIp");

				if (Xbdm != null)
					Xbdm.UpdateDeviceIdent(value);
			}
		}

		/// <summary>
		/// </summary>
		public bool XdkAutoSave
		{
			get { return _xdkAutoSave; }
			set { SetField(ref _xdkAutoSave, value, "XdkAutoSave"); }
		}

		/// <summary>
		/// </summary>
		public string XdkScreenshotPath
		{
			get { return _xdkScreenshotPath; }
			set { SetField(ref _xdkScreenshotPath, value, "XdkScreenshotPath"); }
		}

		/// <summary>
		/// </summary>
		public bool XdkResizeImages
		{
			get { return _xdkResizeImages; }
			set { SetField(ref _xdkResizeImages, value, "XdkResizeImages"); }
		}

		/// <summary>
		/// </summary>
		public int XdkResizeScreenshotHeight
		{
			get { return _xdkResizeScreenshotHeight; }
			set { SetField(ref _xdkResizeScreenshotHeight, value, "XdkResizeScreenshotHeight"); }
		}

		/// <summary>
		/// </summary>
		public int XdkResizeScreenshotWidth
		{
			get { return _xdkResizeScreenshotWidth; }
			set { SetField(ref _xdkResizeScreenshotWidth, value, "XdkResizeScreenshotWidth"); }
		}

		/// <summary>
		/// </summary>
		public bool XdkScreenshotGammaCorrect
		{
			get { return _xdkScreenshotGammaCorrect; }
			set { SetField(ref _xdkScreenshotGammaCorrect, value, "XdkScreenshotGammaCorrect"); }
		}

		/// <summary>
		/// </summary>
		public double XdkScreenshotGammaModifier
		{
			get { return _xdkScreenshotGammaModifier; }
			set { SetField(ref _xdkScreenshotGammaModifier, value, "XdkScreenshotGammaModifier"); }
		}

		/// <summary>
		/// </summary>
		public bool XdkScreenshotFreeze
		{
			get { return _xdkScreenshotFreeze; }
			set { SetField(ref _xdkScreenshotFreeze, value, "XdkScreenshotFreeze"); }
		}

		/// <summary>
		/// </summary>
		public TagSort HalomapTagSort
		{
			get { return _halomapTagSort; }
			set { SetField(ref _halomapTagSort, value, "HalomapTagSort"); }
		}

		/// <summary>
		/// </summary>
		public TagOpenMode HalomapTagOpenMode
		{
			get { return _halomapTagOpenMode; }
			set { SetField(ref _halomapTagOpenMode, value, "HalomapTagOpenMode"); }
		}

		/// <summary>
		/// </summary>
		public bool HalomapShowEmptyClasses
		{
			get { return _halomapShowEmptyClasses; }
			set { SetField(ref _halomapShowEmptyClasses, value, "HalomapShowEmptyClasses"); }
		}

		/// <summary>
		/// </summary>
		public bool HalomapOnlyShowBookmarkedTags
		{
			get { return _halomapOnlyShowBookmarkedTags; }
			set { SetField(ref _halomapOnlyShowBookmarkedTags, value, "HalomapOnlyShowBookmarkedTags"); }
		}

		/// <summary>
		/// </summary>
		public MapInfoDockSide HalomapMapInfoDockSide
		{
			get { return _halomapMapInfoDockSide; }
			set { SetField(ref _halomapMapInfoDockSide, value, "HalomapMapInfoDockSide"); }
		}

		/// <summary>
		/// </summary>
		public LastMetaEditorType HalomapLastSelectedMetaEditor
		{
			get { return _halomapLastSelectedMetaEditor; }
			set { SetField(ref _halomapLastSelectedMetaEditor, value, "HalomapLastSelectedMetaEditor"); }
		}

		/// <summary>
		/// </summary>
		public bool PluginsShowInvisibles
		{
			get { return _pluginsShowInvisibles; }
			set { SetField(ref _pluginsShowInvisibles, value, "PluginsShowInvisibles"); }
		}

		/// <summary>
		/// </summary>
		public bool PluginsShowComments
		{
			get { return _pluginsShowComments; }
			set { SetField(ref _pluginsShowComments, value, "PluginsShowComments"); }
		}

		/// <summary>
		/// </summary>
		public bool DefaultMap
		{
			get { return _defaultMap; }
			set { SetField(ref _defaultMap, value, "DefaultMap"); }
		}

		/// <summary>
		/// </summary>
		public bool DefaultBlf
		{
			get { return _defaultBlf; }
			set { SetField(ref _defaultBlf, value, "DefaultBlf"); }
		}

		/// <summary>
		/// </summary>
		public bool DefaultMif
		{
			get { return _defaultMif; }
			set { SetField(ref _defaultMif, value, "DefaultMif"); }
		}

		/// <summary>
		/// </summary>
		public bool DefaultAmp
		{
			get { return _defaultAmp; }
			set { SetField(ref _defaultAmp, value, "DefaultAmp"); }
		}

		/// <summary>
		/// </summary>
		[JsonIgnore]
		public Home HomeWindow
		{
			get { return _homeWindow; }
			set { SetField(ref _homeWindow, value, "HomeWindow"); }
		}

		/// <summary>
		/// </summary>
		[JsonIgnore]
		public Xbdm Xbdm
		{
			get { return _xbdm; }
			set { SetField(ref _xbdm, value, "Xbdm"); }
		}

		/// <summary>
		/// </summary>
		[JsonIgnore]
		public EngineDatabase DefaultDatabase
		{
			get { return _defaultDatabase; }
			set { SetField(ref _defaultDatabase, value, "DefaultDatabase"); }
		}

		/// <summary>
		/// </summary>
		public void UpdateAssemblyAccent()
		{
			string theme =
				CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
					Enum.Parse(typeof (Accents), ApplicationAccent.ToString()).ToString());
			try
			{
				Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
				{
					Source = new Uri("/Assembly;component/Metro/Themes/" + theme + ".xaml", UriKind.Relative)
				});
			}
			catch
			{
				Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
				{
					Source = new Uri("/Assembly;component/Metro/Themes/Blue.xaml", UriKind.Relative)
				});
			}
		}
	}

	/// <summary>
	/// </summary>
	public class TempStorage
	{
		public static MetroMessageBox.MessageBoxResult MessageBoxButtonStorage;

		public static KeyValuePair<string, int> TagBookmarkSaver;
	}

	/// <summary>
	/// </summary>
	public class RecentFiles
	{
		public static void AddNewEntry(string filename, string filepath, string game, Settings.RecentFileType type)
		{
			Settings.RecentFileEntry alreadyExistsEntry = null;

			if (App.AssemblyStorage.AssemblySettings.ApplicationRecents == null)
				App.AssemblyStorage.AssemblySettings.ApplicationRecents = new ObservableCollection<Settings.RecentFileEntry>();

			foreach (
				Settings.RecentFileEntry entry in
					App.AssemblyStorage.AssemblySettings.ApplicationRecents.Where(
						entry => entry.FileName == filename && entry.FilePath == filepath && entry.FileGame == game))
				alreadyExistsEntry = entry;

			if (alreadyExistsEntry == null)
			{
				// Add New Entry
				var newEntry = new Settings.RecentFileEntry
				{
					FileGame = game,
					FileName = filename,
					FilePath = filepath,
					FileType = type
				};
				App.AssemblyStorage.AssemblySettings.ApplicationRecents.Insert(0, newEntry);
			}
			else
			{
				// Move existing Entry
				App.AssemblyStorage.AssemblySettings.ApplicationRecents.Remove(alreadyExistsEntry);
				App.AssemblyStorage.AssemblySettings.ApplicationRecents.Insert(0, alreadyExistsEntry);
			}

			JumpLists.UpdateJumplists();
		}

		public static void RemoveEntry(Settings.RecentFileEntry entry)
		{
			App.AssemblyStorage.AssemblySettings.ApplicationRecents.Remove(entry);
		}
	}
}