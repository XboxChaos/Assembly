using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using Assembly.Windows;
using Microsoft.Win32;
using System.Web.Script.Serialization;
using Assembly.Metro.Dialogs;
using System.Windows.Shell;
using XBDMCommunicator;
using Assembly.Metro.Controls.PageTemplates.Games;

namespace Assembly.Backend
{
    public class Settings
    {
        public static void LoadSettings(bool applyThemeAswell = false)
        {
            // Declare Registry
            RegistryKey keyApp = Registry.CurrentUser.CreateSubKey("Software\\Xeraxic\\Assembly\\ApplicationSettings\\");
            // Create a JSON Seralizer
            JavaScriptSerializer jss = new JavaScriptSerializer();

            applicationAccent = (Accents)keyApp.GetValue("accent", 0);
            applicationEasterEggs = Convert.ToBoolean(keyApp.GetValue("easterEggs", true));
            if (applyThemeAswell)
                ApplyAccent();

            applicationRecents = jss.Deserialize<List<RecentFileEntry>>(keyApp.GetValue("RecentFiles", "").ToString());
            applicationXBDMSidebarLocation = (Home.XBDMSidebarLocations)keyApp.GetValue("XBDMSidebarLocation", 0);
            applicationSizeWidth = Convert.ToSingle(keyApp.GetValue("SizeWidth", 1100));
            applicationSizeHeight = Convert.ToSingle(keyApp.GetValue("SizeHeight", 600));
            applicationSizeMaximize = Convert.ToBoolean(keyApp.GetValue("SizeMaxamize", false));

            XDKNameIP = keyApp.GetValue("XDKNameIP", "192.168.1.0").ToString();
            if (xbdm != null)
            {
                xbdm.UpdateDeviceIdent(XDKNameIP);
                try { xbdm.Connect(); } catch { }
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
            halomapShowEmptyClasses = Convert.ToBoolean(keyApp.GetValue("ShowEmptyClasses", false));
            halomapLastSelectedMetaEditor = (LastMetaEditorType)keyApp.GetValue("LastSelectedMetaEditor", 0);
            halomapMapInfoDockSide = (MapInfoDockSide)keyApp.GetValue("MapInfoDockSide", 0);

            pluginsShowInvisibles = Convert.ToBoolean(keyApp.GetValue("ShowInvisibles", false));
            pluginsShowComments = Convert.ToBoolean(keyApp.GetValue("ShowComments", true));

            defaultMAP = Convert.ToBoolean(keyApp.GetValue("DefaultMAPEditor", true));
            defaultBLF = Convert.ToBoolean(keyApp.GetValue("DefaultBLFEditor", false));
            defaultMIF = Convert.ToBoolean(keyApp.GetValue("DefaultMIFEditor", false));
        }
        public static void UpdateSettings(bool applyThemeAswell = false)
        {
            // Declare Registry
            RegistryKey keyApp = Registry.CurrentUser.CreateSubKey("Software\\Xeraxic\\Assembly\\ApplicationSettings\\");
            // Create a JSON Seralizer
            JavaScriptSerializer jss = new JavaScriptSerializer();

            keyApp.SetValue("accent", (int)applicationAccent);
            keyApp.SetValue("easterEggs", applicationEasterEggs);
            if (applyThemeAswell)
                ApplyAccent();

            keyApp.SetValue("RecentFiles", jss.Serialize(applicationRecents));
            keyApp.SetValue("XBDMSidebarLocation", (int)applicationXBDMSidebarLocation);
            keyApp.SetValue("SizeWidth", (double)applicationSizeWidth);
            keyApp.SetValue("SizeHeight", (double)applicationSizeHeight);
            keyApp.SetValue("SizeMaxamize", applicationSizeMaximize);

            keyApp.SetValue("XDKNameIP", XDKNameIP);
            if (xbdm != null)
            {
                xbdm.UpdateDeviceIdent(XDKNameIP);
                try { xbdm.Connect(); } catch { }
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
            keyApp.SetValue("MapInfoDockSide", (int)halomapMapInfoDockSide);

            keyApp.SetValue("ShowEmptyClasses", halomapShowEmptyClasses);
            keyApp.SetValue("LastSelectedMetaEditor", (int)halomapLastSelectedMetaEditor);

            keyApp.SetValue("ShowInvisibles", pluginsShowInvisibles);
            keyApp.SetValue("ShowComments", pluginsShowComments);

            keyApp.SetValue("DefaultMAPEditor", defaultMAP);
            keyApp.SetValue("DefaultBLFEditor", defaultBLF);
            keyApp.SetValue("DefaultMIFEditor", defaultMIF);

            // Update Windows 7/8 Jumplists
            JumpLists.UpdateJumplists();

            // Update File Defaults
            FileDefaults.UpdateFileDefaults();
        }


        public static void ApplyAccent()
        {
            string theme = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Enum.Parse(typeof(Accents), applicationAccent.ToString()).ToString());
            try
            {
                App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Metro/Themes/" + theme + ".xaml", UriKind.Relative) });
            }
            catch
            {
                App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Metro/Themes/Blue.xaml", UriKind.Relative) });
            }
        }
        public static Accents applicationAccent = Accents.Blue;
        public static bool applicationEasterEggs = true;

        public static List<RecentFileEntry> applicationRecents = new List<RecentFileEntry>();
        public static Home.XBDMSidebarLocations applicationXBDMSidebarLocation = Home.XBDMSidebarLocations.Sidebar;

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
        public static bool halomapShowEmptyClasses = false;
        public static MapInfoDockSide halomapMapInfoDockSide = MapInfoDockSide.Left;
        public static LastMetaEditorType halomapLastSelectedMetaEditor = LastMetaEditorType.Reflex;

        public static bool pluginsShowInvisibles = false;
        public static bool pluginsShowComments = true;

        public static bool defaultMAP = true;
        public static bool defaultBLF = false;
        public static bool defaultMIF = false;

        public static Home homeWindow = null;
        public static HaloMap selectedHaloMap = null;
        public static XBDM xbdm = null;

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
            Reflex,
            Ident,
            String,
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
        public static MetroMessageBox.MessageBoxResults msgBoxButtonStorage;
    }

    public class RecentFiles
    {
        public static void AddNewEntry(string filename, string filepath, string game, Settings.RecentFileType type)
        {
            Settings.RecentFileEntry alreadyExistsEntry = null;

            if (Settings.applicationRecents == null)
                Settings.applicationRecents = new List<Settings.RecentFileEntry>();

            foreach (Settings.RecentFileEntry entry in Settings.applicationRecents)
                if (entry.FileName == filename && entry.FilePath == filepath && entry.FileGame == game)
                    alreadyExistsEntry = entry;

            if (alreadyExistsEntry == null)
            {
                // Add New Entry
                Settings.RecentFileEntry newEntry = new Settings.RecentFileEntry()
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
        }

        public static void RemoveEntry(Settings.RecentFileEntry entry)
        {
            Settings.applicationRecents.Remove(entry);
            Settings.UpdateSettings();
        }
    }
}