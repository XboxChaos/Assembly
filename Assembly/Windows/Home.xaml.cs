using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Interop;
using Microsoft.Win32;
using System.Windows.Threading;

using Assembly.Metro.Native;
using Assembly.Metro.Controls.PageTemplates;
using Assembly.Metro.Dialogs;
using Assembly.Backend;
using Assembly.Metro.Controls.PageTemplates.Games;
using CloseableTabItemDemo;
using Assembly.Metro.Controls.Sidebar;
using ExtryzeDLL.IO;
using System.IO;
using ExtryzeDLL.Blam.ThirdGen;
using System.Threading;

namespace Assembly.Windows
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : Window
    {
        public Home()
        {
            InitializeComponent();
            DwmDropShadow.DropShadowToWindow(this);
            this.AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(this.CloseTab));
            Settings.homeWindow = this;

            UpdateTitleText("Empty");
            UpdateStatusText("Ready...");

            //Window_StateChanged(null, null);
            ClearTabs();
            
            if (Settings.startpageShowOnLoad)
                AddTabModule(TabGenre.StartPage);

            // Do sidebar Loading stuff
            SwitchXBDMSidebarLocation(Settings.applicationXBDMSidebarLocation);
            XBDMSidebarTimerEvent();

            // Set width/height/state from last session
            if (Settings.applicationSizeHeight != double.NaN)
                this.Height = Settings.applicationSizeHeight;
            if (Settings.applicationSizeWidth != double.NaN)
                this.Width = Settings.applicationSizeWidth;
            if (Settings.applicationSizeMaximize)
                this.WindowState = System.Windows.WindowState.Maximized;
            else
                this.WindowState = System.Windows.WindowState.Normal;
            Window_StateChanged(null, null);

            this.AllowDrop = true;
        }
        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            System.IntPtr handle = (new System.Windows.Interop.WindowInteropHelper(this)).Handle;
            System.Windows.Interop.HwndSource.FromHwnd(handle).AddHook(new System.Windows.Interop.HwndSourceHook(WindowProc));
        }

        #region Content Management
        public enum ContentTypes
        {
            Map,
            MapInfo,
            MapImage
        }

        /// <summary>
        /// Open a new Blam Engine File
        /// </summary>
        /// <param name="contentType">Type of content to open</param>
        public void OpenContentFile(ContentTypes contentType)
        {
            if (contentType == ContentTypes.Map)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Assembly - Open Blam Cache File";
                ofd.Filter = "Blam Cache File (*.map)|*.map";
                ofd.Multiselect = true;
                if ((bool)ofd.ShowDialog())
                    foreach(string file in ofd.FileNames)
                        AddCacheTabModule(ofd.FileName);
            }
            else if (contentType == ContentTypes.MapImage)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Assembly - Open Blam Map Image File";
                ofd.Filter = "Blam Map Image File (*.blf)|*.blf";
                ofd.Multiselect = true;
                if ((bool)ofd.ShowDialog())
                    foreach (string file in ofd.FileNames)
                        AddImageTabModule(ofd.FileName);
            }
            else if (contentType == ContentTypes.MapInfo)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Assembly - Open Blam Map Info File";
                ofd.Filter = "Blam Map Info File (*.mapinfo)|*.mapinfo";
                ofd.Multiselect = true;
                if ((bool)ofd.ShowDialog())
                    foreach (string file in ofd.FileNames)
                        AddInfooTabModule(ofd.FileName);
            }
        }
        #endregion

        #region Tab Manager
        public void ClearTabs()
        {
            homeTabControl.Items.Clear();
        }

        private void CloseTab(object source, RoutedEventArgs args)
        {
            TabItem tabItem = args.Source as TabItem;
            if (tabItem != null)
            {
                dynamic tabContent = tabItem.Content;

                if (tabContent.Close())
                {
                    TabControl tabControl = tabItem.Parent as TabControl;
                    if (tabControl != null)
                        tabControl.Items.Remove(tabItem);
                }
            }
        }

        public void ExternalTabClose(TabItem tab)
        {
            homeTabControl.Items.Remove(tab);

            foreach (TabItem datTab in homeTabControl.Items)
                if (datTab.Header.ToString() == "Start Page")
                {
                    homeTabControl.SelectedItem = datTab;
                    return;
                }

            if (homeTabControl.Items.Count > 0)
                homeTabControl.SelectedIndex = homeTabControl.Items.Count - 1;
        }
        public void ExternalTabClose(TabGenre tabGenre)
        {
            string tabHeader = "";
            if (tabGenre == TabGenre.StartPage)
                tabHeader = "Start Page";
            else if (tabGenre == TabGenre.Settings)
                tabHeader = "Settings Page";

            TabItem toRemove = null;
            foreach (TabItem tab in homeTabControl.Items)
                if (tab.Header.ToString() == tabHeader)
                    toRemove = tab;

            if (toRemove != null)
                homeTabControl.Items.Remove(toRemove);
        }

        /// <summary>
        /// Add a new Blam Cache Editor Container
        /// </summary>
        /// <param name="cacheLocation">Path to the Blam Cache File</param>
        public void AddCacheTabModule(string cacheLocation)
        {
            // Check the map isn't already open
            foreach (TabItem tab in homeTabControl.Items)
                if (cacheLocation == (string)tab.Tag)
                {
                    // Show Message Telling user map is already open
                    MetroMessageBox.Show("Cache Already Open!", "The selected Blam Cache File is already open in Assembly. Let us take you there now.");
                    homeTabControl.SelectedItem = tab;
                    return;
                }

            CloseableTabItem newCacheTab = new CloseableTabItem();
            newCacheTab.Tag = cacheLocation;
            newCacheTab.Header = "";
            //newCacheTab.ToolTip = cacheLocation;
            newCacheTab.Content = new HaloMap(cacheLocation, newCacheTab);

            homeTabControl.Items.Add(newCacheTab);
            homeTabControl.SelectedItem = newCacheTab;
        }
        /// <summary>
        /// Add a new XBox Screenshot Editor Container
        /// </summary>
        /// <param name="tempImageLocation">Path to the temporary location of the image</param>
        public void AddScrenTabModule(string tempImageLocation)
        {
            CloseableTabItem newImageTab = new CloseableTabItem();
            newImageTab.Tag = tempImageLocation;
            newImageTab.Header = "Screenshot";
            //newImageTab.ToolTip = imageLocation;
            newImageTab.Content = new HaloScreenshot(tempImageLocation, newImageTab);

            homeTabControl.Items.Add(newImageTab);
            homeTabControl.SelectedItem = newImageTab;
        }
        /// <summary>
        /// Add a new BLF Editor Container
        /// </summary>
        /// <param name="imageLocation">Path to the BLF file</param>
        public void AddImageTabModule(string imageLocation)
        {
            // Check the map image isn't already open
            foreach (TabItem tab in homeTabControl.Items)
                if (imageLocation == (string)tab.Tag)
                {
                    // Show Message Telling user map image is already open
                    MetroMessageBox.Show("Map Image Already Open!", "The selected Blam Engine File is already open in Assembly. Let us take you there now.");
                    homeTabControl.SelectedItem = tab;
                    return;
                }

            CloseableTabItem newImageTab = new CloseableTabItem();
            newImageTab.Tag = imageLocation;
            newImageTab.Header = "";
            //newImageTab.ToolTip = imageLocation;
            newImageTab.Content = new HaloImage(imageLocation, newImageTab);

            homeTabControl.Items.Add(newImageTab);
            homeTabControl.SelectedItem = newImageTab;
        }
        /// <summary>
        /// Add a new MapInfo Editor Container
        /// </summary>
        /// <param name="infooLocation">Path to the MapInfo file</param>
        public void AddInfooTabModule(string infooLocation)
        {
            // Check the map image isn't already open
            foreach (TabItem tab in homeTabControl.Items)
                if (infooLocation == (string)tab.Tag)
                {
                    // Show Message Telling user map image is already open
                    MetroMessageBox.Show("Map Info Already Open!", "The selected Blam Engine File is already open in Assembly. Let us take you there now.");
                    homeTabControl.SelectedItem = tab;
                    return;
                }

            CloseableTabItem newInfooTab = new CloseableTabItem();
            newInfooTab.Tag = infooLocation;
            newInfooTab.Header = "";
            //newInfooTab.ToolTip = infooLocation;
            newInfooTab.Content = new HaloInfo(infooLocation, newInfooTab);

            homeTabControl.Items.Add(newInfooTab);
            homeTabControl.SelectedItem = newInfooTab;
        }

        public enum TabGenre
        {
            StartPage,
            Settings,
            NetworkPoking,
            PluginGenerator,
            Patches,
            MemoryManager,
            Welcome
        }
        public void AddTabModule(TabGenre tabG)
        {
            CloseableTabItem tab = new CloseableTabItem();
            tab.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            tab.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;

            switch(tabG)
            {
                case TabGenre.StartPage:
                    tab.Header = "Start Page";
                    tab.Content = new StartPage();
                    break;
                case TabGenre.Welcome:
                    tab.Header = "Welcome";
                    tab.Content = new WelcomePage();
                    break;
                case TabGenre.Settings:
                    tab.Header = "Settings Page";
                    tab.Content = new SettingsPage();
                    break;
                case TabGenre.NetworkPoking:
                    tab.Header = "Network Poking";
                    tab.Content = new NetworkGrouping();
                    break;
                case TabGenre.PluginGenerator:
                    tab.Header = "Plugin Generator";
                    tab.Content = new HaloPluginGenerator();
                    break;
                case TabGenre.Patches:
                    tab.Header = "Patch Control";
                    tab.Content = new PatchControl();
                    break;
                case TabGenre.MemoryManager:
                    tab.Header = "Memory Manager";
                    tab.Content = new MemoryManager();
                    break;
            }

            foreach (TabItem tabb in homeTabControl.Items)
                if (tabb.Header == tab.Header)
                {
                    homeTabControl.SelectedItem = tabb;
                    return;
                }

            homeTabControl.Items.Add(tab);
            homeTabControl.SelectedItem = tab;
        }
        #endregion

        #region Public Access Modifiers
        /// <summary>
        /// Set the title text of Assembly
        /// </summary>
        /// <param name="title">Current Title, Assembly shall add the rest for you.</param>
        public void UpdateTitleText(string title)
        {
            this.Title = title + " - Assembly α";
            lblTitle.Text = title + " - Assembly α";
        }

        /// <summary>
        /// Set the status text of Assembly
        /// </summary>
        /// <param name="status">Current Status of Assembly</param>
        public void UpdateStatusText(string status)
        {
            this.Status.Text = status;

            statusUpdateTimer.Stop();
            statusUpdateTimer.Interval = new TimeSpan(0, 0, 0, 4);
            statusUpdateTimer.Tick += statusUpdateCleaner_Clear;
            statusUpdateTimer.Start();
        }
        private void statusUpdateCleaner_Clear(object sender, EventArgs e)
        {
            this.Status.Text = "Ready...";
        }
        private DispatcherTimer statusUpdateTimer = new DispatcherTimer();
        #endregion

        #region XBDM Sidebar
        public XBDMSidebar XBDMSidebar = new XBDMSidebar();
        private XBDMSidebarLocations _xbdmSidebar = XBDMSidebarLocations.Sidebar;
        private bool _isXBDMSidebarShowing = true;

        /// <summary>
        /// Show the XBDM Sidebar, I will check it is in a valid phase to show.
        /// </summary>
        public void XBDMSidebarTimerEvent()
        {
            if (_xbdmSidebar == XBDMSidebarLocations.Sidebar)
                if (_isXBDMSidebarShowing)
                {
                    // Hide Sidebar
                    xbdmCoverContent.Visibility = System.Windows.Visibility.Collapsed;
                    _isXBDMSidebarShowing = false;
                }
                else
                {
                    // Show Sidebar
                    xbdmCoverContent.Visibility = System.Windows.Visibility.Visible;
                    _isXBDMSidebarShowing = true;
                }
        }

        public enum XBDMSidebarLocations { Sidebar, Docked }
        /// <summary>
        /// Switch where the XBDM Sidebar is located
        /// </summary>
        /// <param name="location">The location to move it to</param>
        public void SwitchXBDMSidebarLocation(XBDMSidebarLocations location)
        {
            switch (location)
            {
                case XBDMSidebarLocations.Sidebar:
                    XBDMSideBarCol.Width = new GridLength(0);

                    xbdmSidebarButton.IsEnabled = true;
                    xbdmSidebarButton.Visibility = System.Windows.Visibility.Visible;
                    xbdmCoverContent.Visibility = System.Windows.Visibility.Visible;
                    homeTabControl.Margin = new Thickness(0, 0, 30, 0);

                    xbdmCoverContent.Children.Clear();
                    xbdmContent.Children.Clear();
                    xbdmCoverContent.Children.Add(XBDMSidebar);
                    break;
                case XBDMSidebarLocations.Docked:
                    XBDMSideBarCol.Width = new GridLength(275);

                    xbdmSidebarButton.IsEnabled = false;
                    xbdmSidebarButton.Visibility = System.Windows.Visibility.Collapsed;
                    xbdmCoverContent.Visibility = System.Windows.Visibility.Collapsed;
                    homeTabControl.Margin = new Thickness(0);

                    xbdmCoverContent.Children.Clear();
                    xbdmContent.Children.Clear();
                    xbdmContent.Children.Add(XBDMSidebar);
                    break;
            }

            _xbdmSidebar = location;
        }
        #endregion

        #region Opacity Masking
        public int OpacityIndex = 0;
        public void ShowMask()
        {
            OpacityIndex++;
            OpacityMask.Visibility = System.Windows.Visibility.Visible;
        }
        public void HideMask()
        {
            OpacityIndex--;

            if (OpacityIndex == 0) 
                OpacityMask.Visibility = System.Windows.Visibility.Collapsed;
        }
        #endregion

        #region Drag&Drop Support
        private void HomeWindow_Drop(object sender, DragEventArgs e)
        {
            // FIXME: Boot into Win7, to fix this. (Win8's UAC is so fucked up... No drag and drop on win8 it seems...)
            //string[] draggedFiles = (string[])e.Data.GetData(DataFormats.FileDrop, true);
        }
        #endregion

        #region Startup
        public bool ProcessCommandLineArgs(IList<string> args)
        {
            if (args == null || args.Count < 2)
                return true;

            if ((args.Count >= 2))
            {
                // Decide what to do
                switch (args[1].ToLower())
                {
                    case "open": case "assembly://open":
                        // Determine type of file, and start it up, yo
                        if (args.Count >= 2)
                            StartupDetermineType(args[2]);
                        break;

                    case "assembly://update/":
                        // Show Update
                        menuHelpUpdater_Click(null, null);
                        break;
                    case "assembly://about/": 
                        // Show About
                        menuHelpAbout_Click(null, null);
                        break;
                    case "assembly://settings/":
                        // Show Settings
                        menuOpenSettings_Click(null, null);
                        break;
                }
            }

            return true;
        }

        private void StartupDetermineType(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    // Magic Check
                    EndianStream stream = new EndianStream(new FileStream(path, FileMode.Open), Endian.BigEndian);
                    stream.SeekTo(0);

                    switch (stream.ReadAscii(0x04))
                    {
                        case "head":
                            // Map File
                            stream.Close();
                            AddCacheTabModule(path);
                            return;

                        case "_blf":
                            // BLF Container, needs more checking
                            stream.Close();
                            PureBLF blf = new PureBLF(path);
                            blf.Close();
                            if (blf.BLFChunks.Count > 2)
                            {
                                if (blf.BLFChunks[1].ChunkMagic == "levl")
                                {
                                    // Load MapInfo
                                    AddInfooTabModule(path);
                                    return;
                                }
                                else if (blf.BLFChunks[1].ChunkMagic == "mapi")
                                {
                                    // Load MapImage BLF
                                    AddImageTabModule(path);
                                    return;
                                }
                            }
                            MetroMessageBox.Show("Unsupported BLF Type", "The selected BLF file is not supported in assembly.");
                            return;

                        default:
                            MetroMessageBox.Show("Unsupported file type", "The selected file is not supported in assembly.");
                            return;
                    }
                }
                else
                    MetroMessageBox.Show("Unable to find file", "The selected file could no longer be found");
            }
            catch (Exception ex) { MetroException.Show(ex); }
        }
        #endregion

        private void homeTabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            TabItem tab = (TabItem)homeTabControl.SelectedItem;

            if (tab != null)
                UpdateTitleText(tab.Header.ToString().Replace("__", "_").Replace(".map", ""));

            if (tab != null && tab.Header.ToString() == "Start Page")
                ((StartPage)tab.Content).UpdateRecents();

            // Check if the tab is a HaloMap
            if (tab != null && tab.Content != null && tab.Content is HaloMap)
                Settings.selectedHaloMap = (HaloMap)tab.Content;
            else
                Settings.selectedHaloMap = null;
        }

        #region More WPF Annoyance
        private void headerThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Left = Left + e.HorizontalChange;
            Top = Top + e.VerticalChange;
        }

        private void ResizeDrop_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double yadjust = this.Height + e.VerticalChange;
            double xadjust = this.Width + e.HorizontalChange;

            if (xadjust > this.MinWidth)
                this.Width = xadjust;
            if (yadjust > this.MinHeight)
                this.Height = yadjust;
        }
        private void ResizeRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double xadjust = this.Width + e.HorizontalChange;

            if (xadjust > this.MinWidth)
                this.Width = xadjust;
        }
        private void ResizeBottom_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double yadjust = this.Height + e.VerticalChange;

            if (yadjust > this.MinHeight)
                this.Height = yadjust;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
            {
                borderFrame.BorderThickness = new Thickness(1, 1, 1, 23);
                Settings.applicationSizeMaximize = false;
                Settings.applicationSizeHeight = this.Height;
                Settings.applicationSizeWidth = this.Width;
                Settings.UpdateSettings();

                btnActionRestore.Visibility = System.Windows.Visibility.Collapsed;
                btnActionMaxamize.Visibility = ResizeDropVector.Visibility = ResizeDrop.Visibility = ResizeRight.Visibility = ResizeBottom.Visibility = System.Windows.Visibility.Visible;
            }
            else if (this.WindowState == System.Windows.WindowState.Maximized)
            {
                borderFrame.BorderThickness = new Thickness(0, 0, 0, 23);
                Settings.applicationSizeMaximize = true;
                Settings.UpdateSettings();

                btnActionRestore.Visibility = System.Windows.Visibility.Visible;
                btnActionMaxamize.Visibility = ResizeDropVector.Visibility = ResizeDrop.Visibility = ResizeRight.Visibility = ResizeBottom.Visibility = System.Windows.Visibility.Collapsed;
            }
            /*
             * ResizeDropVector
             * ResizeDrop
             * ResizeRight
             * ResizeBottom
             */
        }
        private void headerThumb_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
                this.WindowState = System.Windows.WindowState.Maximized;
            else if (this.WindowState == System.Windows.WindowState.Maximized)
                this.WindowState = System.Windows.WindowState.Normal;
        }
        private void btnActionSupport_Click(object sender, RoutedEventArgs e)
        {
            // Load support page?
        }
        private void btnActionMinimize_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }
        private void btnActionRestore_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Normal;
        }
        private void btnActionMaxamize_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Maximized;
        }
        private void btnActionClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #region Maximize Workspace Workarounds
        private System.IntPtr WindowProc(
              System.IntPtr hwnd,
              int msg,
              System.IntPtr wParam,
              System.IntPtr lParam,
              ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }

            return (System.IntPtr)0;
        }
        private void WmGetMinMaxInfo(System.IntPtr hwnd, System.IntPtr lParam)
        {
            Assembly.Metro.Native.Monitor_Workarea.MINMAXINFO mmi = (Assembly.Metro.Native.Monitor_Workarea.MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(Assembly.Metro.Native.Monitor_Workarea.MINMAXINFO));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            System.IntPtr monitor = Assembly.Metro.Native.Monitor_Workarea.MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != System.IntPtr.Zero)
            {
                System.Windows.Forms.Screen scrn = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);

                Assembly.Metro.Native.Monitor_Workarea.MONITORINFO monitorInfo = new Assembly.Metro.Native.Monitor_Workarea.MONITORINFO();
                Assembly.Metro.Native.Monitor_Workarea.GetMonitorInfo(monitor, monitorInfo);
                Assembly.Metro.Native.Monitor_Workarea.RECT rcWorkArea = monitorInfo.rcWork;
                Assembly.Metro.Native.Monitor_Workarea.RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);

                /*
                mmi.ptMaxPosition.x = Math.Abs(scrn.Bounds.Left - scrn.WorkingArea.Left);
                mmi.ptMaxPosition.y = Math.Abs(scrn.Bounds.Top - scrn.WorkingArea.Top);
                mmi.ptMaxSize.x = Math.Abs(scrn.Bounds.Right - scrn.WorkingArea.Left);
                mmi.ptMaxSize.y = Math.Abs(scrn.Bounds.Bottom - scrn.WorkingArea.Top);
                */
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }
        #endregion
        #endregion

        private void menuOpenCacheFile_Click(object sender, RoutedEventArgs e)          { OpenContentFile(ContentTypes.Map); }
        private void menuOpenCacheInfomation_Click(object sender, RoutedEventArgs e)    { OpenContentFile(ContentTypes.MapInfo); }
        private void menuOpenCacheImage_Click(object sender, RoutedEventArgs e)         { OpenContentFile(ContentTypes.MapImage); }
        
        private void menuOpenSettings_Click(object sender, EventArgs e)                 { AddTabModule(TabGenre.Settings); }

        private void menuViewStartPage_Click(object sender, RoutedEventArgs e)          { AddTabModule(TabGenre.StartPage); }
        private void menuMemoryManager_Click(object sender, RoutedEventArgs e)          { AddTabModule(TabGenre.MemoryManager); }
        private void menuPatches_Click(object sender, RoutedEventArgs e)                { AddTabModule(TabGenre.Patches); }
        private void menuNetworkPoking_Click(object sender, RoutedEventArgs e)          { AddTabModule(TabGenre.NetworkPoking); }
        private void menuPluginGeneration_Click(object sender, RoutedEventArgs e)     { AddTabModule(TabGenre.PluginGenerator); }
        
        private void menuHelpAbout_Click(object sender, RoutedEventArgs e)              { MetroAbout.Show(); }
        private void menuHelpUpdater_Click(object sender, RoutedEventArgs e)            { Thread thrd = new Thread(new ThreadStart(Updater.BeginUpdateProcess)); thrd.Start(); }

        private void menuCloseApplication_Click(object sender, RoutedEventArgs e)       { Application.Current.Shutdown(); }


        private void Home_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            App app = (App)Application.Current;
        }

        private void Home_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            _0xabad1dea.TragicSans.KeyDown(e.Key);
        }
        private void btnIWff_Click(object sender, RoutedEventArgs e)
        {
            _0xabad1dea.IWff.CleanUp();
        }

        private void Window_PreviewDrop_1(object sender, DragEventArgs e)
        {

        }
    }
}