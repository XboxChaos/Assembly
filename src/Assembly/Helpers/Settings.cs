using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using Assembly.Windows;
using Microsoft.Win32;
using System.Web.Script.Serialization;
using Assembly.Metro.Dialogs;
using XBDMCommunicator;

namespace Assembly.Helpers
{
    public static class Settings
    {
        /// <summary>
        /// Raised whenever the settings are loaded or saved.
        /// </summary>
        public static event EventHandler SettingsChanged;

        public static void LoadSettings(bool applyThemeAswell = false)
        {
            // Declare Registry
			var keyApp = Registry.CurrentUser.CreateSubKey("Software\\Xeraxic\\Assembly\\ApplicationSettings\\");
            // Create a JSON Seralizer
            var jss = new JavaScriptSerializer();

	        if (keyApp != null)
	        {
		        applicationAccent = (Accents)keyApp.GetValue("accent", 0);
				applicationEasterEggs = Convert.ToBoolean(keyApp.GetValue("easterEggs", true));
				applicationUpdateOnStartup = Convert.ToBoolean(keyApp.GetValue("CheckUpdatesOnStartup", true));
				if (applyThemeAswell)
					ApplyAccent();

		        applicationRecents = jss.Deserialize<List<RecentFileEntry>>(keyApp.GetValue("RecentFiles", "").ToString());
		        applicationSizeWidth = Convert.ToSingle(keyApp.GetValue("SizeWidth", 1100));
		        applicationSizeHeight = Convert.ToSingle(keyApp.GetValue("SizeHeight", 600));
		        applicationSizeMaximize = Convert.ToBoolean(keyApp.GetValue("SizeMaxamize", false));

		        XDKNameIP = keyApp.GetValue("XDKNameIP", "192.168.1.0").ToString();
				if (xbdm != null)
				{
					xbdm.UpdateDeviceIdent(XDKNameIP);
					//try { xbdm.Connect(); } catch { }
				}
		        XDKAutoSave = Convert.ToBoolean(keyApp.GetValue("XDKAutoSave", true));
		        XDKScreenshotPath = keyApp.GetValue("XDKScreenshotPath", VariousFunctions.GetApplicationLocation() + @"Saved Images\").ToString();
		        XDKResizeImages = Convert.ToBoolean(keyApp.GetValue("XDKScreenshotResize", true));
		        XDKResizeScreenshotHeight = Convert.ToInt16(keyApp.GetValue("XDKScreenshotHeight", 1080));
		        XDKResizeScreenshotWidth = Convert.ToInt16(keyApp.GetValue("XDKScreenshotWidth", 1920));
		        XDKScreenshotGammaCorrect = Convert.ToBoolean(keyApp.GetValue("XDKScreenGammaCorrect", true));
		        XDKScreenshotGammaModifier = Convert.ToDouble(keyApp.GetValue("XDKScreenModifier", 0.5));
		        XDKScreenshotFreeze = Convert.ToBoolean(keyApp.GetValue("XDKScreenFreeze", false));

		        startpageShowOnLoad = Convert.ToBoolean(keyApp.GetValue("ShowStartPageOnLoad", true));
		        startpageHideOnLaunch = Convert.ToBoolean(keyApp.GetValue("HideStartPageOnLaunch", false));
		        startpageShowRecentsMap = Convert.ToBoolean(keyApp.GetValue("ShowRecentsMap", true));
		        startpageShowRecentsBLF = Convert.ToBoolean(keyApp.GetValue("ShowRecentsBLF", true));
		        startpageShowRecentsMapInfo = Convert.ToBoolean(keyApp.GetValue("ShowRecentsMapInfo", true));

		        halomapTagSort = (TagSort)keyApp.GetValue("TagSorting", 0);
				halomapTagOpenMode = (TagOpenMode)keyApp.GetValue("TagOpeningMode", 0);
		        halomapShowEmptyClasses = Convert.ToBoolean(keyApp.GetValue("ShowEmptyClasses", false));
				halomapOnlyShowBookmarkedTags = Convert.ToBoolean(keyApp.GetValue("OnlyShowBookmarkedTags", false));
		        halomapLastSelectedMetaEditor = (LastMetaEditorType)keyApp.GetValue("LastSelectedMetaEditor", 0);
		        halomapMapInfoDockSide = (MapInfoDockSide)keyApp.GetValue("MapInfoDockSide", 0);

		        pluginsShowInvisibles = Convert.ToBoolean(keyApp.GetValue("ShowInvisibles", false));
		        pluginsShowComments = Convert.ToBoolean(keyApp.GetValue("ShowComments", true));

		        defaultMAP = Convert.ToBoolean(keyApp.GetValue("DefaultMAPEditor", true));
		        defaultBLF = Convert.ToBoolean(keyApp.GetValue("DefaultBLFEditor", false));
		        defaultMIF = Convert.ToBoolean(keyApp.GetValue("DefaultMIFEditor", false));
		        defaultAMP = Convert.ToBoolean(keyApp.GetValue("DefaultAMPEditor", true));
	        }

	        OnSettingsChanged();
        }
        public static void UpdateSettings(bool applyThemeAswell = false)
        {
            // Declare Registry
			var keyApp = Registry.CurrentUser.CreateSubKey("Software\\Xeraxic\\Assembly\\ApplicationSettings\\");
            // Create a JSON Seralizer
			var jss = new JavaScriptSerializer();

			if (keyApp != null)
			{
				keyApp.SetValue("accent", (int) applicationAccent);
				keyApp.SetValue("easterEggs", applicationEasterEggs);
				keyApp.SetValue("CheckUpdatesOnStartup", applicationUpdateOnStartup);
				if (applyThemeAswell)
					ApplyAccent();

                if (applicationRecents != null && applicationRecents.Count > 10)
                    applicationRecents.RemoveRange(10, applicationRecents.Count - 10);

				keyApp.SetValue("RecentFiles", jss.Serialize(applicationRecents));
				keyApp.SetValue("SizeWidth", applicationSizeWidth);
				keyApp.SetValue("SizeHeight", applicationSizeHeight);
				keyApp.SetValue("SizeMaxamize", applicationSizeMaximize);

				keyApp.SetValue("XDKNameIP", XDKNameIP);
				if (xbdm != null)
				{
					xbdm.UpdateDeviceIdent(XDKNameIP);
					//try { xbdm.Connect(); } catch { }
				}

				keyApp.SetValue("XDKAutoSave", XDKAutoSave);
				keyApp.SetValue("XDKScreenshotPath", XDKScreenshotPath);
				keyApp.SetValue("XDKScreenshotResize", XDKResizeImages);
				keyApp.SetValue("XDKScreenshotHeight", XDKResizeScreenshotHeight);
				keyApp.SetValue("XDKScreenshotWidth", XDKResizeScreenshotWidth);
				keyApp.SetValue("XDKScreenGammaCorrect", XDKScreenshotGammaCorrect);
				keyApp.SetValue("XDKScreenModifier", XDKScreenshotGammaModifier);
				keyApp.SetValue("XDKScreenFreeze", XDKScreenshotFreeze);

				keyApp.SetValue("ShowStartPageOnLoad", startpageShowOnLoad);
				keyApp.SetValue("HideStartPageOnLaunch", startpageHideOnLaunch);
				keyApp.SetValue("ShowRecentsMap", startpageShowRecentsMap);
				keyApp.SetValue("ShowRecentsBLF", startpageShowRecentsBLF);
				keyApp.SetValue("ShowRecentsMapInfo", startpageShowRecentsMapInfo);

				keyApp.SetValue("TagSorting", (int)halomapTagSort);
				keyApp.SetValue("TagOpeningMode", (int)halomapTagOpenMode);
				keyApp.SetValue("MapInfoDockSide", (int) halomapMapInfoDockSide);

				keyApp.SetValue("ShowEmptyClasses", halomapShowEmptyClasses);
				keyApp.SetValue("OnlyShowBookmarkedTags", halomapOnlyShowBookmarkedTags);
				keyApp.SetValue("LastSelectedMetaEditor", (int) halomapLastSelectedMetaEditor);

				keyApp.SetValue("ShowInvisibles", pluginsShowInvisibles);
				keyApp.SetValue("ShowComments", pluginsShowComments);

				keyApp.SetValue("DefaultMAPEditor", defaultMAP);
				keyApp.SetValue("DefaultBLFEditor", defaultBLF);
				keyApp.SetValue("DefaultMIFEditor", defaultMIF);
				keyApp.SetValue("DefaultAMPEditor", defaultAMP);
			}

            // Update File Defaults
            FileDefaults.UpdateFileDefaults();

            OnSettingsChanged();
        }

        private static void OnSettingsChanged()
        {
            if (SettingsChanged != null)
                SettingsChanged(null, EventArgs.Empty);
        }

        public static void ApplyAccent()
        {
            var theme = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Enum.Parse(typeof(Accents), applicationAccent.ToString()).ToString());
            try
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Metro/Themes/" + theme + ".xaml", UriKind.Relative) });
            }
            catch
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Metro/Themes/Blue.xaml", UriKind.Relative) });
            }
        }
        public static Accents applicationAccent = Accents.Blue;
	    public static bool applicationUpdateOnStartup = true;
        public static bool applicationEasterEggs = true;

        public static List<RecentFileEntry> applicationRecents = new List<RecentFileEntry>();

        public static double applicationSizeWidth = 1100;
        public static double applicationSizeHeight = 600;
        public static bool applicationSizeMaximize = false;

        public static bool startpageShowOnLoad = true;
        public static bool startpageHideOnLaunch = false;
        public static bool startpageShowRecentsMap = true;
        public static bool startpageShowRecentsBLF = true;
        public static bool startpageShowRecentsMapInfo = true;

        public static string XDKNameIP = "";
        public static bool XDKAutoSave = false;
        public static string XDKScreenshotPath = "";
        public static bool XDKResizeImages = false;
        public static int XDKResizeScreenshotHeight = 1080;
        public static int XDKResizeScreenshotWidth = 1920;
        public static bool XDKScreenshotGammaCorrect = true;
        public static double XDKScreenshotGammaModifier = 0.5;
        public static bool XDKScreenshotFreeze = true;

        public static TagSort halomapTagSort = TagSort.TagClass;
	    public static TagOpenMode halomapTagOpenMode = TagOpenMode.NewTab;
        public static bool halomapShowEmptyClasses = false;
	    public static bool halomapOnlyShowBookmarkedTags = false;
        public static MapInfoDockSide halomapMapInfoDockSide = MapInfoDockSide.Left;
        public static LastMetaEditorType halomapLastSelectedMetaEditor = LastMetaEditorType.Info;

        public static bool pluginsShowInvisibles = false;
        public static bool pluginsShowComments = true;

        public static bool defaultMAP = true;
        public static bool defaultBLF = false;
		public static bool defaultMIF = false;
		public static bool defaultAMP = false;

        public static Home homeWindow = null;
        public static Xbdm xbdm = null;

		public enum TagOpenMode
		{
			NewTab,
			ExistingTab
		}
        public enum Accents
        {
            Blue,
            Purple,
            Orange,
            Green
        }
        public enum RecentFileType
        {
            Cache,
            MapInfo,
            BLF
        }
        public enum TagSort
        {
            TagClass,
            ObjectHierarchy,
            PathHierarchy
        }
        public enum MapInfoDockSide
        {
            Left,
            Right
        }
        public enum LastMetaEditorType
        {
            Info,
            //Reflex,
            //Ident,
            //String,
            MetaEditor,
            PluginEditor
        }
        public class RecentFileEntry
        {
            public string FileName { get; set; }
            public RecentFileType FileType { get; set; }
            public string FileGame { get; set; }
            public string FilePath { get; set; }
        }
    }

    public class TempStorage
    {
        public static MetroMessageBox.MessageBoxResult MessageBoxButtonStorage;

	    public static KeyValuePair<string, int> TagBookmarkSaver;

	    public static string MessageBoxInputStorage;
    }

    public class RecentFiles
    {
        public static void AddNewEntry(string filename, string filepath, string game, Settings.RecentFileType type)
        {
            Settings.RecentFileEntry alreadyExistsEntry = null;

            if (Settings.applicationRecents == null)
                Settings.applicationRecents = new List<Settings.RecentFileEntry>();

            foreach (var entry in Settings.applicationRecents.Where(entry => entry.FileName == filename && entry.FilePath == filepath && entry.FileGame == game))
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
                Settings.applicationRecents.Insert(0, newEntry);
            }
            else
            {
                // Move existing Entry
                Settings.applicationRecents.Remove(alreadyExistsEntry);
                Settings.applicationRecents.Insert(0, alreadyExistsEntry);
            }

            Settings.UpdateSettings();
            JumpLists.UpdateJumplists();
        }

        public static void RemoveEntry(Settings.RecentFileEntry entry)
        {
            Settings.applicationRecents.Remove(entry);
            Settings.UpdateSettings();
        }
    }
}