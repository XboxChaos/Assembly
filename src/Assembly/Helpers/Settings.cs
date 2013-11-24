using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using Assembly.Properties;
using Assembly.Windows;
using Microsoft.Win32;
using System.Web.Script.Serialization;
using Assembly.Metro.Dialogs;
using Newtonsoft.Json;
using XBDMCommunicator;
using Blamite.Flexibility;
using Blamite.Flexibility.Settings;
using System.ComponentModel;

namespace Assembly.Helpers
{
	/// <summary>
	/// 
	/// </summary>
	public class Storage : INotifyPropertyChanged
	{
		/// <summary>
		/// 
		/// </summary>
		public Storage()
		{
			Load();
		}

		/// <summary>
		/// 
		/// </summary>
		public Settings AssemblySettings
		{
			get { return _assemblySettings; }
			set
			{
				// Set Data
				SetField(ref _assemblySettings, value, "AssemblySettings");

				// Write Changes
				var jsonData = JsonConvert.SerializeObject(value);

				// Get File Path
				File.WriteAllText("AssemblySettings.ason", jsonData);

				// Update Accent
				_assemblySettings.UpdateAssemblyAccent();

				// Update File Defaults
				FileDefaults.UpdateFileDefaults();
			}
		}
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
			catch(JsonSerializationException)
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
	}

	/// <summary>
	/// 
	/// </summary>
	public class Settings : INotifyPropertyChanged
	{
		/// <summary>
		/// 
		/// </summary>
		public void UpdateAssemblyAccent()
		{
			var theme = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Enum.Parse(typeof(Accents), ApplicationAccent.ToString()).ToString());
			try
			{
				Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Metro/Themes/" + theme + ".xaml", UriKind.Relative) });
			}
			catch
			{
				Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Metro/Themes/Blue.xaml", UriKind.Relative) });
			}
		}

		/// <summary>
		/// The Accent colour the user has selected. Defaults to Assembly Blue.
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
		private Accents _applicationAccent = Accents.Blue;

		/// <summary>
		/// Wether Assebembly checks for updates at the startup. Defaults to True.
		/// </summary>
		public bool ApplicationUpdateOnStartup
		{
			get { return _applicationUpdateOnStartup; }
			set { SetField(ref _applicationUpdateOnStartup, value, "ApplicationUpdateOnStartup"); }
		}
		private bool _applicationUpdateOnStartup = true;

		/// <summary>
		/// 
		/// </summary>
		public bool ApplicationEasterEggs
		{
			get { return _applicationEasterEggs; }
			set { SetField(ref _applicationEasterEggs, value, "ApplicationEasterEggs"); }
		}
		private bool _applicationEasterEggs = true;

		/// <summary>
		/// A list of Assembly's recently opened files.
		/// </summary>
		public List<RecentFileEntry> ApplicationRecents
		{
			get { return _applicationRecents; }
			set { SetField(ref _applicationRecents, value, "ApplicationRecents"); }
		}
		private List<RecentFileEntry> _applicationRecents = new List<RecentFileEntry>();

		/// <summary>
		/// 
		/// </summary>
		public double ApplicationSizeWidth
		{
			get { return _applicationSizeWidth; }
			set { SetField(ref _applicationSizeWidth, value, "ApplicationSizeWidth"); }
		}
		private double _applicationSizeWidth = 1100;

		/// <summary>
		/// 
		/// </summary>
		public double ApplicationSizeHeight
		{
			get { return _applicationSizeHeight; }
			set { SetField(ref _applicationSizeHeight, value, "ApplicationSizeHeight"); }
		}
		private double _applicationSizeHeight = 600;

		/// <summary>
		/// 
		/// </summary>
		public bool ApplicationSizeMaximize
		{
			get { return _applicationSizeMaximize; }
			set { SetField(ref _applicationSizeMaximize, value, "ApplicationSizeMaximize"); }
		}
		private bool _applicationSizeMaximize;

		/// <summary>
		/// 
		/// </summary>
		public bool StartpageShowOnLoad
		{
			get { return _startpageShowOnLoad; }
			set { SetField(ref _startpageShowOnLoad, value, "StartpageShowOnLoad"); }
		}
		private bool _startpageShowOnLoad = true;

		/// <summary>
		/// 
		/// </summary>
		public bool StartpageHideOnLaunch
		{
			get { return _startpageHideOnLaunch; }
			set { SetField(ref _startpageHideOnLaunch, value, "StartpageHideOnLaunch"); }
		}
		private bool _startpageHideOnLaunch;

		/// <summary>
		/// 
		/// </summary>
		public bool StartpageShowRecentsMap
		{
			get { return _startpageShowRecentsMap; }
			set { SetField(ref _startpageShowRecentsMap, value, "StartpageShowRecentsMap"); }
		}
		private bool _startpageShowRecentsMap = true;

		/// <summary>
		/// 
		/// </summary>
		public bool StartpageShowRecentsBlf
		{
			get { return _startpageShowRecentsBlf; }
			set { SetField(ref _startpageShowRecentsBlf, value, "StartpageShowRecentsBlf"); }
		}
		private bool _startpageShowRecentsBlf = true;

		/// <summary>
		/// 
		/// </summary>
		public bool StartpageShowRecentsMapInfo
		{
			get { return _startpageShowRecentsMapInfo; }
			set { SetField(ref _startpageShowRecentsMapInfo, value, "StartpageShowRecentsMapInfo"); }
		}
		private bool _startpageShowRecentsMapInfo = true;

		/// <summary>
		/// 
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
		private string _xdkNameIp = "192.168.0.1";

		/// <summary>
		/// 
		/// </summary>
		public bool XdkAutoSave
		{
			get { return _xdkAutoSave; }
			set { SetField(ref _xdkAutoSave, value, "XdkAutoSave"); }
		}
		private bool _xdkAutoSave;

		/// <summary>
		/// 
		/// </summary>
		public string XdkScreenshotPath
		{
			get { return _xdkScreenshotPath; }
			set { SetField(ref _xdkScreenshotPath, value, "XdkScreenshotPath"); }
		}
		private string _xdkScreenshotPath = "";

		/// <summary>
		/// 
		/// </summary>
		public bool XdkResizeImages
		{
			get { return _xdkResizeImages; }
			set { SetField(ref _xdkResizeImages, value, "XdkResizeImages"); }
		}
		private bool _xdkResizeImages;

		/// <summary>
		/// 
		/// </summary>
		public int XdkResizeScreenshotHeight
		{
			get { return _xdkResizeScreenshotHeight; }
			set { SetField(ref _xdkResizeScreenshotHeight, value, "XdkResizeScreenshotHeight"); }
		}
		private int _xdkResizeScreenshotHeight = 1080;

		/// <summary>
		/// 
		/// </summary>
		public int XdkResizeScreenshotWidth
		{
			get { return _xdkResizeScreenshotWidth; }
			set { SetField(ref _xdkResizeScreenshotWidth, value, "XdkResizeScreenshotWidth"); }
		}
		private int _xdkResizeScreenshotWidth = 1920;

		/// <summary>
		/// 
		/// </summary>
		public bool XdkScreenshotGammaCorrect
		{
			get { return _xdkScreenshotGammaCorrect; }
			set { SetField(ref _xdkScreenshotGammaCorrect, value, "XdkScreenshotGammaCorrect"); }
		}
		private bool _xdkScreenshotGammaCorrect = true;

		/// <summary>
		/// 
		/// </summary>
		public double XdkScreenshotGammaModifier
		{
			get { return _xdkScreenshotGammaModifier; }
			set { SetField(ref _xdkScreenshotGammaModifier, value, "XdkScreenshotGammaModifier"); }
		}
		private double _xdkScreenshotGammaModifier = 0.5;

		/// <summary>
		/// 
		/// </summary>
		public bool XdkScreenshotFreeze
		{
			get { return _xdkScreenshotFreeze; }
			set { SetField(ref _xdkScreenshotFreeze, value, "XdkScreenshotFreeze"); }
		}
		private bool _xdkScreenshotFreeze = true;

		/// <summary>
		/// 
		/// </summary>
		public TagSort HalomapTagSort
		{
			get { return _halomapTagSort; }
			set { SetField(ref _halomapTagSort, value, "HalomapTagSort"); }
		}
		private TagSort _halomapTagSort = TagSort.TagClass;

		/// <summary>
		/// 
		/// </summary>
		public TagOpenMode HalomapTagOpenMode
		{
			get { return _halomapTagOpenMode; }
			set { SetField(ref _halomapTagOpenMode, value, "HalomapTagOpenMode"); }
		}
		private TagOpenMode _halomapTagOpenMode = TagOpenMode.NewTab;

		/// <summary>
		/// 
		/// </summary>
		public bool HalomapShowEmptyClasses
		{
			get { return _halomapShowEmptyClasses; }
			set { SetField(ref _halomapShowEmptyClasses, value, "HalomapShowEmptyClasses"); }
		}
		private bool _halomapShowEmptyClasses;

		/// <summary>
		/// 
		/// </summary>
		public bool HalomapOnlyShowBookmarkedTags
		{
			get { return _halomapOnlyShowBookmarkedTags; }
			set { SetField(ref _halomapOnlyShowBookmarkedTags, value, "HalomapOnlyShowBookmarkedTags"); }
		}
		private bool _halomapOnlyShowBookmarkedTags;

		/// <summary>
		/// 
		/// </summary>
		public MapInfoDockSide HalomapMapInfoDockSide
		{
			get { return _halomapMapInfoDockSide; }
			set { SetField(ref _halomapMapInfoDockSide, value, "HalomapMapInfoDockSide"); }
		}
		private MapInfoDockSide _halomapMapInfoDockSide = MapInfoDockSide.Left;

		/// <summary>
		/// 
		/// </summary>
		public LastMetaEditorType HalomapLastSelectedMetaEditor
		{
			get { return _halomapLastSelectedMetaEditor; }
			set { SetField(ref _halomapLastSelectedMetaEditor, value, "HalomapLastSelectedMetaEditor"); }
		}
		private LastMetaEditorType _halomapLastSelectedMetaEditor = LastMetaEditorType.Info;

		/// <summary>
		/// 
		/// </summary>
		public bool PluginsShowInvisibles
		{
			get { return _pluginsShowInvisibles; }
			set { SetField(ref _pluginsShowInvisibles, value, "PluginsShowInvisibles"); }
		}
		private bool _pluginsShowInvisibles;

		/// <summary>
		/// 
		/// </summary>
		public bool PluginsShowComments
		{
			get { return _pluginsShowComments; }
			set { SetField(ref _pluginsShowComments, value, "PluginsShowComments"); }
		}
		private bool _pluginsShowComments = true;

		/// <summary>
		/// 
		/// </summary>
		public bool DefaultMap
		{
			get { return _defaultMap; }
			set { SetField(ref _defaultMap, value, "DefaultMap"); }
		}
		private bool _defaultMap;

		/// <summary>
		/// 
		/// </summary>
		public bool DefaultBlf
		{
			get { return _defaultBlf; }
			set { SetField(ref _defaultBlf, value, "DefaultBlf"); }
		}
		private bool _defaultBlf;

		/// <summary>
		/// 
		/// </summary>
		public bool DefaultMif
		{
			get { return _defaultMif; }
			set { SetField(ref _defaultMif, value, "DefaultMif"); }
		}
		private bool _defaultMif;

		/// <summary>
		/// 
		/// </summary>
		public bool DefaultAmp
		{
			get { return _defaultAmp; }
			set { SetField(ref _defaultAmp, value, "DefaultAmp"); }
		}
		private bool _defaultAmp;

		/// <summary>
		/// 
		/// </summary>
		[JsonIgnore]
		public Home HomeWindow
		{
			get { return _homeWindow; }
			set { SetField(ref _homeWindow, value, "HomeWindow"); }
		}
		private Home _homeWindow;

		/// <summary>
		/// 
		/// </summary>
		[JsonIgnore]
		public Xbdm Xbdm
		{
			get { return _xbdm; }
			set { SetField(ref _xbdm, value, "Xbdm"); }
		}
		private Xbdm _xbdm;

		/// <summary>
		/// 
		/// </summary>
		[JsonIgnore]
		public EngineDatabase DefaultDatabase
		{
			get { return _defaultDatabase; }
			set { SetField(ref _defaultDatabase, value, "DefaultDatabase"); }
		}
		private EngineDatabase _defaultDatabase = XMLEngineDatabaseLoader.LoadDatabase("Formats/Engines.xml");

		#region Enums

		/// <summary>
		/// 
		/// </summary>
		public enum TagOpenMode
		{
			NewTab,
			ExistingTab
		}

		/// <summary>
		/// 
		/// </summary>
		public enum Accents
		{
			Blue,
			Purple,
			Orange,
			Green
		}

		/// <summary>
		/// 
		/// </summary>
		public enum RecentFileType
		{
			Cache,
			MapInfo,
			Blf
		}

		/// <summary>
		/// 
		/// </summary>
		public enum TagSort
		{
			TagClass,
			ObjectHierarchy,
			PathHierarchy
		}

		/// <summary>
		/// 
		/// </summary>
		public enum MapInfoDockSide
		{
			Left,
			Right
		}

		/// <summary>
		/// 
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

		#endregion

		#region Classes

		/// <summary>
		/// 
		/// </summary>
		public class RecentFileEntry
		{
			public string FileName { get; set; }
			public RecentFileType FileType { get; set; }
			public string FileGame { get; set; }
			public string FilePath { get; set; }
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

			// Write Changes
			var jsonData = JsonConvert.SerializeObject(this);
			File.WriteAllText("AssemblySettings.ason", jsonData);

			return true;
		}

		#endregion
	}

	/// <summary>
	/// 
	/// </summary>
	public class TempStorage
	{
		public static MetroMessageBox.MessageBoxResult MessageBoxButtonStorage;

		public static KeyValuePair<string, int> TagBookmarkSaver;
	}

	/// <summary>
	/// 
	/// </summary>
	public class RecentFiles
	{
		public static void AddNewEntry(string filename, string filepath, string game, Settings.RecentFileType type)
		{
			Settings.RecentFileEntry alreadyExistsEntry = null;

			if (App.AssemblyStorage.AssemblySettings.ApplicationRecents == null)
				App.AssemblyStorage.AssemblySettings.ApplicationRecents = new List<Settings.RecentFileEntry>();

			foreach (var entry in App.AssemblyStorage.AssemblySettings.ApplicationRecents.Where(entry => entry.FileName == filename && entry.FilePath == filepath && entry.FileGame == game))
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
