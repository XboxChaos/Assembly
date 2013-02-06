using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Assembly.Helpers;
using Assembly.Metro.Controls.PageTemplates;
using Assembly.Metro.Controls.PageTemplates.Games;
using Assembly.Metro.Controls.PageTemplates.Tools.Halo4;
using Assembly.Metro.Controls.Sidebar;
using Assembly.Metro.Dialogs;
using Assembly.Metro.Native;
using CloseableTabItemDemo;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.IO;
using Microsoft.Win32;

namespace Assembly.Windows
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home
	{
		#region ContextMenus
		public ContextMenu BaseContextMenu;
	    public ContextMenu FilesystemContextMenu;

		/// <summary>
		/// Really hacky, but i didn't want to re-do the TabControl to make it DataBinded...
		/// </summary>
		private void InitalizeContextMenus()
		{
			// Create Lame Context Menu
			BaseContextMenu = new ContextMenu();
			BaseContextMenu.Items.Add(new MenuItem { Header = "Close" }); ((MenuItem)BaseContextMenu.Items[0]).Click += contextMenuClose_Click;
			BaseContextMenu.Items.Add(new MenuItem { Header = "Close All" }); ((MenuItem)BaseContextMenu.Items[1]).Click += contextMenuCloseAll_Click;
			BaseContextMenu.Items.Add(new MenuItem { Header = "Close All But This" }); ((MenuItem)BaseContextMenu.Items[2]).Click += contextMenuCloseAllButThis_Click;
			BaseContextMenu.Items.Add(new MenuItem { Header = "Close Tabs To The Left" }); ((MenuItem)BaseContextMenu.Items[3]).Click += contextMenuCloseToLeft_Click;
			BaseContextMenu.Items.Add(new MenuItem { Header = "Close Tabs To The Right" }); ((MenuItem)BaseContextMenu.Items[4]).Click += contextMenuCloseToRight_Click;

			// Create Fun Context Menu
			FilesystemContextMenu = new ContextMenu();
			FilesystemContextMenu.Items.Add(new MenuItem { Header = "Close" }); ((MenuItem)FilesystemContextMenu.Items[0]).Click += contextMenuClose_Click;
			FilesystemContextMenu.Items.Add(new MenuItem { Header = "Close All" }); ((MenuItem)FilesystemContextMenu.Items[1]).Click += contextMenuCloseAll_Click;
			FilesystemContextMenu.Items.Add(new MenuItem { Header = "Close All But This" }); ((MenuItem)FilesystemContextMenu.Items[2]).Click += contextMenuCloseAllButThis_Click;
			FilesystemContextMenu.Items.Add(new MenuItem { Header = "Close Tabs To The Left" }); ((MenuItem)FilesystemContextMenu.Items[3]).Click += contextMenuCloseToLeft_Click;
			FilesystemContextMenu.Items.Add(new MenuItem { Header = "Close Tabs To The Right" }); ((MenuItem)FilesystemContextMenu.Items[4]).Click += contextMenuCloseToRight_Click;
			FilesystemContextMenu.Items.Add(new Separator());
			FilesystemContextMenu.Items.Add(new MenuItem { Header = "Copy File Path" }); ((MenuItem)FilesystemContextMenu.Items[6]).Click += contextMenuCopyFilePath_Click;
			FilesystemContextMenu.Items.Add(new MenuItem { Header = "Open Containing Folder" }); ((MenuItem)FilesystemContextMenu.Items[7]).Click += contextMenuOpenContainingFolder_Click;
		}

		private void contextMenuClose_Click(object sender, RoutedEventArgs routedEventArgs)
	    {
			var target = sender as FrameworkElement;
			while (target is ContextMenu == false)
			{
				Debug.Assert(target != null, "target != null");
				target = target.Parent as FrameworkElement;
			}
			var tabitem = ((ContentControl)(target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;
			ExternalTabClose(tabitem);
	    }
		private void contextMenuCloseAll_Click(object sender, RoutedEventArgs routedEventArgs)
		{
			var toDelete = homeTabControl.Items.OfType<CloseableTabItem>().Cast<TabItem>().ToList();

			ExternalTabsClose(toDelete);
		}
	    private void contextMenuCloseAllButThis_Click(object sender, RoutedEventArgs routedEventArgs)
		{
			var target = sender as FrameworkElement;
			while (target is ContextMenu == false)
			{
				Debug.Assert(target != null, "target != null");
				target = target.Parent as FrameworkElement;
			}
			var tabitem = ((ContentControl)(target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;

		    var toDelete = homeTabControl.Items.OfType<CloseableTabItem>().Where(tab => !Equals(tab, tabitem)).Cast<TabItem>().ToList();

			ExternalTabsClose(toDelete, false);
		}
		private void contextMenuCloseToLeft_Click(object sender, RoutedEventArgs routedEventArgs)
		{
			var target = sender as FrameworkElement;
			while (target is ContextMenu == false)
			{
				Debug.Assert(target != null, "target != null");
				target = target.Parent as FrameworkElement;
			}
			var tabitem = ((ContentControl)(target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;
			var selectedIndexOfTab = GetSelectedIndex(tabitem);

			var toDelete = new List<TabItem>();
			for(var i = 0; i < selectedIndexOfTab; i++)
				toDelete.Add((TabItem)homeTabControl.Items[i]);

			ExternalTabsClose(toDelete, false);
		}
		private void contextMenuCloseToRight_Click(object sender, RoutedEventArgs routedEventArgs)
		{
			var target = sender as FrameworkElement;
			while (target is ContextMenu == false)
			{
				Debug.Assert(target != null, "target != null");
				target = target.Parent as FrameworkElement;
			}
			var tabitem = ((ContentControl)(target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;
			var selectedIndexOfTab = GetSelectedIndex(tabitem);

			var toDelete = new List<TabItem>();
			for (var i = selectedIndexOfTab + 1; i < homeTabControl.Items.Count; i++)
				toDelete.Add((TabItem)homeTabControl.Items[i]);

			ExternalTabsClose(toDelete, false);
		}

		private static void contextMenuCopyFilePath_Click(object sender, RoutedEventArgs routedEventArgs)
		{
			var target = sender as FrameworkElement;
			while (target is ContextMenu == false)
			{
				Debug.Assert(target != null, "target != null");
				target = target.Parent as FrameworkElement;
			}
			var tabitem = ((ContentControl)(target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;
			if (tabitem != null) Clipboard.SetText(tabitem.Tag.ToString());
		}
		private static void contextMenuOpenContainingFolder_Click(object sender, RoutedEventArgs routedEventArgs)
		{
			var target = sender as FrameworkElement;
			while (target is ContextMenu == false)
			{
				Debug.Assert(target != null, "target != null");
				target = target.Parent as FrameworkElement;
			}
			var tabitem = ((ContentControl)(target as ContextMenu).PlacementTarget).Parent as CloseableTabItem;
			if (tabitem == null) return;

			var filepathArgument = "/select, \""  + tabitem.Tag + "\"";
			Process.Start("explorer.exe", filepathArgument);
		}
	    #endregion

		public Home()
        {
            InitializeComponent();

			// Setup Context Menus
			InitalizeContextMenus();

            DwmDropShadow.DropShadowToWindow(this);
            AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(CloseTab));
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
            if (!double.IsNaN(Settings.applicationSizeHeight))
                Height = Settings.applicationSizeHeight;
            if (!double.IsNaN(Settings.applicationSizeWidth))
                Width = Settings.applicationSizeWidth;
            WindowState = Settings.applicationSizeMaximize ? WindowState.Maximized : WindowState.Normal;
            Window_StateChanged(null, null);

            AllowDrop = true;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var handle = (new WindowInteropHelper(this)).Handle;
	        var hwndSource = HwndSource.FromHwnd(handle);
	        if (hwndSource != null)
		        hwndSource.AddHook(WindowProc);
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
	        switch (contentType)
			{
#pragma warning disable 168
		        case ContentTypes.Map:
			        {
				        var ofd = new OpenFileDialog
					                  {
						                  Title = "Assembly - Open Blam Cache File",
						                  Filter = "Blam Cache File (*.map)|*.map",
						                  Multiselect = true
					                  };
				        if ((bool)ofd.ShowDialog())
							foreach (var file in ofd.FileNames)
						        AddCacheTabModule(ofd.FileName);
			        }
			        break;
		        case ContentTypes.MapImage:
			        {
						var ofd = new OpenFileDialog
							          {
								          Title = "Assembly - Open Blam Map Image File",
								          Filter = "Blam Map Image File (*.blf)|*.blf",
								          Multiselect = true
							          };
				        if ((bool)ofd.ShowDialog())
							foreach (var file in ofd.FileNames)
						        AddImageTabModule(ofd.FileName);
			        }
			        break;
		        case ContentTypes.MapInfo:
			        {
						var ofd = new OpenFileDialog
							          {
								          Title = "Assembly - Open Blam Map Info File",
								          Filter = "Blam Map Info File (*.mapinfo)|*.mapinfo",
								          Multiselect = true
							          };
				        if ((bool)ofd.ShowDialog())
							foreach (var file in ofd.FileNames)
						        AddInfooTabModule(ofd.FileName);
			        }
			        break;
			}
#pragma warning restore 168
        }

	    #endregion

        #region Tab Manager
        public void ClearTabs()
        {
            homeTabControl.Items.Clear();
        }

        private static void CloseTab(object source, RoutedEventArgs args)
        {
            var tabItem = args.Source as TabItem;
	        if (tabItem == null) return;

	        dynamic tabContent = tabItem.Content;
	        if (!tabContent.Close()) return;

			var tabControl = tabItem.Parent as TabControl;
	        if (tabControl != null)
		        tabControl.Items.Remove(tabItem);
        }

        public void ExternalTabClose(TabItem tab, bool updateFocus = true)
        {
            homeTabControl.Items.Remove(tab);

			if (!updateFocus) return;

			foreach (var datTab in homeTabControl.Items.Cast<TabItem>().Where(datTab => ((ContentControl)datTab.Header).Content.ToString() == "Start Page"))
            {
	            homeTabControl.SelectedItem = datTab;
	            return;
            }

            if (homeTabControl.Items.Count > 0)
                homeTabControl.SelectedIndex = homeTabControl.Items.Count - 1;
        }
		public void ExternalTabsClose(List<TabItem> tab, bool updateFocus = true)
		{
			foreach (var tabItem in tab)
				homeTabControl.Items.Remove(tabItem);

			if (!updateFocus) return;

			foreach (var datTab in homeTabControl.Items.Cast<TabItem>().Where(datTab => ((ContentControl)datTab.Header).Content.ToString() == "Start Page"))
			{
				homeTabControl.SelectedItem = datTab;
				return;
			}

			if (homeTabControl.Items.Count > 0)
				homeTabControl.SelectedIndex = homeTabControl.Items.Count - 1;
		}
		public void ExternalTabClose(TabGenre tabGenre, bool updateFocus = true)
        {
			var tabHeader = "";
            switch (tabGenre)
            {
	            case TabGenre.StartPage:
		            tabHeader = "Start Page";
		            break;
	            case TabGenre.Settings:
		            tabHeader = "Settings Page";
		            break;
            }

            TabItem toRemove = null;
			foreach (var tab in homeTabControl.Items.Cast<TabItem>().Where(tab => ((ContentControl)tab.Header).Content.ToString() == tabHeader))
	            toRemove = tab;

            if (toRemove != null)
                homeTabControl.Items.Remove(toRemove);
        }

		public int GetSelectedIndex(TabItem selectedTab)
		{
			var index = 0;
			foreach (var tab in homeTabControl.Items)
			{
				if (Equals(tab, selectedTab))
					return index;

				index++;
			}

			throw new Exception();
		}

        /// <summary>
        /// Add a new Blam Cache Editor Container
        /// </summary>
        /// <param name="cacheLocation">Path to the Blam Cache File</param>
        public void AddCacheTabModule(string cacheLocation)
        {
            // Check the map isn't already open
            foreach (var tab in homeTabControl.Items.Cast<TabItem>().Where(tab => cacheLocation == (string)tab.Tag))
            {
	            // Show Message Telling user map is already open
	            MetroMessageBox.Show("Cache Already Open!", "The selected Blam Cache File is already open in Assembly. Let us take you there now.");
	            homeTabControl.SelectedItem = tab;
	            return;
            }

			var newCacheTab = new CloseableTabItem
				                  {
					                  Tag = cacheLocation, 
									  Header = new ContentControl
										           {
											           Content = "",
													   ContextMenu = FilesystemContextMenu
										           }
				                  };
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
			var newImageTab = new CloseableTabItem
				                  {
					                  Tag = tempImageLocation, 
									  Header = new ContentControl
										           {
											           Content = "Screenshot",
													   ContextMenu = BaseContextMenu
										           }
				                  };
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
			foreach (var tab in homeTabControl.Items.Cast<TabItem>().Where(tab => imageLocation == (string)tab.Tag))
            {
	            // Show Message Telling user map image is already open
	            MetroMessageBox.Show("Map Image Already Open!", "The selected Blam Engine File is already open in Assembly. Let us take you there now.");
	            homeTabControl.SelectedItem = tab;
	            return;
            }

			var newImageTab = new CloseableTabItem
				                  {
					                  Tag = imageLocation, 
									  Header = new ContentControl
										           {
											           Content = "",
													   ContextMenu = FilesystemContextMenu
										           }
				                  };
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
			foreach (var tab in homeTabControl.Items.Cast<TabItem>().Where(tab => infooLocation == (string)tab.Tag))
            {
	            // Show Message Telling user map image is already open
	            MetroMessageBox.Show("Map Info Already Open!", "The selected Blam Engine File is already open in Assembly. Let us take you there now.");
	            homeTabControl.SelectedItem = tab;
	            return;
            }

			var newInfooTab = new CloseableTabItem
				                  {
					                  Tag = infooLocation,
									  Header = new ContentControl
									  {
										  Content = "",
										  ContextMenu = FilesystemContextMenu
									  }
				                  };
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
            Welcome,
			PluginConverter,

			MemoryManager,
			VoxelConverter
        }
        public void AddTabModule(TabGenre tabG)
        {
			var tab = new CloseableTabItem
				          {
					          HorizontalAlignment = HorizontalAlignment.Stretch,
					          VerticalAlignment = VerticalAlignment.Stretch
				          };

	        switch(tabG)
            {
                case TabGenre.StartPage:
		            tab.Header = new ContentControl
			        {
				        Content = "Start Page",
				        ContextMenu = BaseContextMenu
			        };
                    tab.Content = new StartPage();
                    break;
                case TabGenre.Welcome:
					tab.Header = new ContentControl
					{
						Content = "Welcome",
						ContextMenu = BaseContextMenu
					};
                    tab.Content = new WelcomePage();
                    break;
                case TabGenre.Settings:
					tab.Header = new ContentControl
					{
						Content = "Settings",
						ContextMenu = BaseContextMenu
					};
                    tab.Content = new SettingsPage();
                    break;
                case TabGenre.NetworkPoking:
					tab.Header = new ContentControl
					{
						Content = "Network Poking",
						ContextMenu = BaseContextMenu
					};
                    tab.Content = new NetworkGrouping();
                    break;
                case TabGenre.PluginGenerator:
					tab.Header = new ContentControl
					{
						Content = "Plugin Generator",
						ContextMenu = BaseContextMenu
					};
                    tab.Content = new HaloPluginGenerator();
                    break;
                case TabGenre.Patches:
					tab.Header = new ContentControl
					{
						Content = "Patcher",
						ContextMenu = BaseContextMenu
					};
                    tab.Content = new PatchControl();
                    break;
				case TabGenre.PluginConverter:
					tab.Header = new ContentControl
					{
						Content = "Plugin Converter",
						ContextMenu = BaseContextMenu
					};
                    tab.Content = new HaloPluginConverter();
		            break;


				case TabGenre.MemoryManager:
					tab.Header = new ContentControl
					{
						Content = "Memory Manager",
						ContextMenu = BaseContextMenu
					};
					tab.Content = new MemoryManager();
					break;
				case TabGenre.VoxelConverter:
		            tab.Header = new ContentControl
					{
						Content = "Voxel Converter",
						ContextMenu = BaseContextMenu
					};
					tab.Content = new VoxelConverter();
					break;
            }

			foreach (var tabb in homeTabControl.Items.Cast<TabItem>().Where(tabb => ((ContentControl)tabb.Header).Content == ((ContentControl)tab.Header).Content))
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
            Title = title + " - Assembly α";
            lblTitle.Text = title + " - Assembly α";
        }

        /// <summary>
        /// Set the status text of Assembly
        /// </summary>
        /// <param name="status">Current Status of Assembly</param>
        public void UpdateStatusText(string status)
        {
            Status.Text = status;

            statusUpdateTimer.Stop();
            statusUpdateTimer.Interval = new TimeSpan(0, 0, 0, 4);
            statusUpdateTimer.Tick += statusUpdateCleaner_Clear;
            statusUpdateTimer.Start();
        }
        private void statusUpdateCleaner_Clear(object sender, EventArgs e)
        {
            Status.Text = "Ready...";
        }
        private readonly DispatcherTimer statusUpdateTimer = new DispatcherTimer();
        #endregion

        #region XBDM Sidebar
        public readonly XBDMSidebar XBDMSidebar = new XBDMSidebar();
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
                    xbdmCoverContent.Visibility = Visibility.Collapsed;
                    _isXBDMSidebarShowing = false;
                }
                else
                {
                    // Show Sidebar
                    xbdmCoverContent.Visibility = Visibility.Visible;
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
                    xbdmSidebarButton.Visibility = Visibility.Visible;
                    xbdmCoverContent.Visibility = Visibility.Visible;
                    homeTabControl.Margin = new Thickness(0, 0, 30, 0);

                    xbdmCoverContent.Children.Clear();
                    xbdmContent.Children.Clear();
                    xbdmCoverContent.Children.Add(XBDMSidebar);
                    break;
                case XBDMSidebarLocations.Docked:
                    XBDMSideBarCol.Width = new GridLength(275);

                    xbdmSidebarButton.IsEnabled = false;
                    xbdmSidebarButton.Visibility = Visibility.Collapsed;
                    xbdmCoverContent.Visibility = Visibility.Collapsed;
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
	    public int OpacityIndex;
        public void ShowMask()
        {
            OpacityIndex++;
            OpacityMask.Visibility = Visibility.Visible;
        }
        public void HideMask()
        {
            OpacityIndex--;

            if (OpacityIndex == 0) 
                OpacityMask.Visibility = Visibility.Collapsed;
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
					var stream = new EndianStream(new FileStream(path, FileMode.Open), Endian.BigEndian);
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
                            var blf = new PureBLF(path);
                            blf.Close();
                            if (blf.BLFChunks.Count > 2)
                            {
                                switch (blf.BLFChunks[1].ChunkMagic)
                                {
	                                case "levl":
		                                AddInfooTabModule(path);
		                                return;
	                                case "mapi":
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

	            MetroMessageBox.Show("Unable to find file", "The selected file could no longer be found");
            }
            catch (Exception ex) { MetroException.Show(ex); }
        }
        #endregion

        private void homeTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
			var tab = (TabItem)homeTabControl.SelectedItem;

            if (tab != null)
                UpdateTitleText(((ContentControl)tab.Header).Content.ToString().Replace("__", "_").Replace(".map", ""));

			if (tab != null && ((ContentControl)tab.Header).Content.ToString() == "Start Page")
                ((StartPage)tab.Content).UpdateRecents();

            // Check if the tab is a HaloMap
            if (tab != null && tab.Content != null && tab.Content is HaloMap)
                Settings.selectedHaloMap = (HaloMap)tab.Content;
            else
                Settings.selectedHaloMap = null;

			if (tab == null)
				homeTabControl.SelectedIndex = 0;
        }

        #region More WPF Annoyance
        private void ResizeDrop_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var yadjust = Height + e.VerticalChange;
			var xadjust = Width + e.HorizontalChange;

            if (xadjust > MinWidth)
                Width = xadjust;
            if (yadjust > MinHeight)
                Height = yadjust;
        }
        private void ResizeRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
			var xadjust = Width + e.HorizontalChange;

            if (xadjust > MinWidth)
                Width = xadjust;
        }
        private void ResizeBottom_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var yadjust = Height + e.VerticalChange;

            if (yadjust > MinHeight)
                Height = yadjust;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
	        switch (WindowState)
	        {
		        case WindowState.Normal:
			        borderFrame.BorderThickness = new Thickness(1, 1, 1, 23);
			        btnActionRestore.Visibility = Visibility.Collapsed;
			        btnActionMaxamize.Visibility = ResizeDropVector.Visibility = ResizeDrop.Visibility = ResizeRight.Visibility = ResizeBottom.Visibility = Visibility.Visible;
			        break;
		        case WindowState.Maximized:
			        borderFrame.BorderThickness = new Thickness(0, 0, 0, 23);
			        btnActionRestore.Visibility = Visibility.Visible;
			        btnActionMaxamize.Visibility = ResizeDropVector.Visibility = ResizeDrop.Visibility = ResizeRight.Visibility = ResizeBottom.Visibility = Visibility.Collapsed;
			        break;
	        }
	        /*
             * ResizeDropVector
             * ResizeDrop
             * ResizeRight
             * ResizeBottom
             */
        }

	    private void btnActionSupport_Click(object sender, RoutedEventArgs e)
        {
            // Load support page?
        }
        private void btnActionMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        private void btnActionRestore_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
        }
        private void btnActionMaxamize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
        }
        private void btnActionClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #region Maximize Workspace Workarounds
        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }
            return IntPtr.Zero;
        }

        private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
			var mmi = (Monitor_Workarea.MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(Monitor_Workarea.MINMAXINFO));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            const int MONITOR_DEFAULTTONEAREST = 0x00000002;
            var monitor = Monitor_Workarea.MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
				var monitorInfo = new Monitor_Workarea.MONITORINFO();
                Monitor_Workarea.GetMonitorInfo(monitor, monitorInfo);
				var rcWorkArea = monitorInfo.rcWork;
				var rcMonitorArea = monitorInfo.rcMonitor;
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

		// File
        private void menuOpenCacheFile_Click(object sender, RoutedEventArgs e)				{ OpenContentFile(ContentTypes.Map); }
        private void menuOpenCacheInfomation_Click(object sender, RoutedEventArgs e)		{ OpenContentFile(ContentTypes.MapInfo); }
        private void menuOpenCacheImage_Click(object sender, RoutedEventArgs e)				{ OpenContentFile(ContentTypes.MapImage); }
        
		// Edit
        private void menuOpenSettings_Click(object sender, EventArgs e)						{ AddTabModule(TabGenre.Settings); }

		// Tools
		private void menuMemoryManager_Click(object sender, RoutedEventArgs e)				{ AddTabModule(TabGenre.MemoryManager); }
		private void menuToolHalo4VoxelConverter_Click(object sender, RoutedEventArgs e)	{ AddTabModule(TabGenre.VoxelConverter); }

		// View
        private void menuViewStartPage_Click(object sender, RoutedEventArgs e)				{ AddTabModule(TabGenre.StartPage); }
        private void menuPatches_Click(object sender, RoutedEventArgs e)					{ AddTabModule(TabGenre.Patches); }
        private void menuNetworkPoking_Click(object sender, RoutedEventArgs e)				{ AddTabModule(TabGenre.NetworkPoking); }
        private void menuPluginGeneration_Click(object sender, RoutedEventArgs e)			{ AddTabModule(TabGenre.PluginGenerator); }
		private void menuPluginConverter_Click(object sender, RoutedEventArgs e)			{ AddTabModule(TabGenre.PluginConverter); }
        
		// Help
        private void menuHelpAbout_Click(object sender, RoutedEventArgs e)					{ MetroAbout.Show(); }
		private void menuHelpUpdater_Click(object sender, RoutedEventArgs e)				{ var thrd = new Thread(Updater.BeginUpdateProcess); thrd.Start(); }

		// Goodbye Sweet Evelyn
        private void menuCloseApplication_Click(object sender, RoutedEventArgs e)			{ Application.Current.Shutdown(); }


		#region Waste of Space, idk man
		private void Home_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //var app = (App)Application.Current;
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
		#endregion
	}
}