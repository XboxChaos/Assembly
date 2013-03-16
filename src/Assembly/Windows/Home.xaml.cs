﻿using System;
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
using Assembly.Metro.Controls.PageTemplates.Tools;
using Assembly.Metro.Controls.PageTemplates.Tools.Halo4;
using Assembly.Metro.Controls.Sidebar;
using Assembly.Metro.Dialogs;
using Assembly.Helpers.Native;
using CloseableTabItemDemo;
using Blamite.Blam.ThirdGen;
using Blamite.IO;
using Microsoft.Win32;
using System.ComponentModel;
using Assembly.Helpers.Net;

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

		void InitalizeContextMenusCloseItems(ItemCollection menuItems)
		{
			((MenuItem)(menuItems[ menuItems.Add(new MenuItem { Header = "Close" }) ])).Click += contextMenuClose_Click;
			((MenuItem)(menuItems[ menuItems.Add(new MenuItem { Header = "Close All" }) ])).Click += contextMenuCloseAll_Click;
			((MenuItem)(menuItems[ menuItems.Add(new MenuItem { Header = "Close All But This" }) ])).Click += contextMenuCloseAllButThis_Click;
			((MenuItem)(menuItems[ menuItems.Add(new MenuItem { Header = "Close Tabs To The Left" }) ])).Click += contextMenuCloseToLeft_Click;
			((MenuItem)(menuItems[ menuItems.Add(new MenuItem { Header = "Close Tabs To The Right" }) ])).Click += contextMenuCloseToRight_Click;
		}
		/// <summary>
		/// Really hacky, but i didn't want to re-do the TabControl to make it DataBinded...
		/// </summary>
		private void InitalizeContextMenus()
		{
			// Create Lame Context Menu
			BaseContextMenu = new ContextMenu();
			var menu_items = BaseContextMenu.Items;
			InitalizeContextMenusCloseItems(menu_items);

			// Create Fun Context Menu
			FilesystemContextMenu = new ContextMenu();
			menu_items = FilesystemContextMenu.Items;
			InitalizeContextMenusCloseItems(menu_items);
			menu_items.Add(new Separator());
			((MenuItem)(menu_items[ menu_items.Add(new MenuItem { Header = "Copy File Path" }) ])).Click += contextMenuCopyFilePath_Click;
			((MenuItem)(menu_items[ menu_items.Add(new MenuItem { Header = "Open Containing Folder" }) ])).Click += contextMenuOpenContainingFolder_Click;
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

            UpdateTitleText("");
            UpdateStatusText("Ready...");

            //Window_StateChanged(null, null);
            ClearTabs();
            
            if (Settings.startpageShowOnLoad)
                AddTabModule(TabGenre.StartPage);

            // Do sidebar Loading stuff
            SwitchXBDMSidebarLocation(Settings.applicationXBDMSidebarLocation);
            XBDMSidebarTimerEvent();

            // Set width/height/state from last session
            if (!double.IsNaN(Settings.applicationSizeHeight) && Settings.applicationSizeHeight > MinHeight)
                Height = Settings.applicationSizeHeight;
			if (!double.IsNaN(Settings.applicationSizeWidth) && Settings.applicationSizeWidth > MinWidth)
                Width = Settings.applicationSizeWidth;

            WindowState = Settings.applicationSizeMaximize ? WindowState.Maximized : WindowState.Normal;
            Window_StateChanged(null, null);

			AllowDrop = true;
			Settings.homeWindow = this;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var handle = (new WindowInteropHelper(this)).Handle;
	        var hwndSource = HwndSource.FromHwnd(handle);
	        if (hwndSource != null)
		        hwndSource.AddHook(WindowProc);

            ProcessCommandLineArgs(Environment.GetCommandLineArgs());

			if (Settings.applicationUpdateOnStartup)
				StartUpdateCheck();
        }

        private void StartUpdateCheck()
        {
            var worker = new BackgroundWorker();
            worker.DoWork += CheckForUpdates;
            worker.RunWorkerCompleted += UpdateCheckCompleted;
            worker.RunWorkerAsync();
        }

        void CheckForUpdates(object sender, DoWorkEventArgs e)
        {
            // Grab JSON Update package from the server
            e.Result = Updates.GetUpdateInfo();
        }

        void UpdateCheckCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
                return;

            var updateInfo = (UpdateInfo)e.Result;
            if (Updater.UpdateAvailable(updateInfo))
                MetroUpdateDialog.Show(updateInfo, true);
        }

        #region Content Management
        public enum ContentTypes
        {
            Map,
            MapInfo,
            MapImage
        }
		class ContentFileHandler
		{
			public string Title { get; set; }
			public string Filter { get; set; }
			public bool AllowMultipleFiles { get; set; }
			public Action<Home, string> FileHandler;

			public ContentFileHandler(string title, string filter, Action<Home, string> handler, bool allowMultipleFiles = true)
			{
				Title = title;
				Filter = filter;
				AllowMultipleFiles = allowMultipleFiles;
				FileHandler = handler;
			}
		};

		Dictionary<ContentTypes, ContentFileHandler> contentFileHandlers = new Dictionary<ContentTypes, ContentFileHandler> {
			{ContentTypes.Map, new ContentFileHandler(
				"Assembly - Open Blam Cache File",
				"Blam Cache File (*.map)|*.map", 
				(home, file)=> home.AddCacheTabModule(file)) },
			{ContentTypes.MapImage, new ContentFileHandler(
				"Assembly - Open Blam Map Image File",
				"Blam Map Image File (*.blf)|*.blf", 
				(home, file)=> home.AddImageTabModule(file)) },
			{ContentTypes.MapInfo, new ContentFileHandler(
				"Assembly - Open Blam Map Info File",
				"Blam Map Info File (*.mapinfo)|*.mapinfo", 
				(home, file)=> home.AddInfooTabModule(file)) },
		};
        /// <summary>
        /// Open a new Blam Engine File
        /// </summary>
        /// <param name="contentType">Type of content to open</param>
        public void OpenContentFile(ContentTypes contentType)
        {
			ContentFileHandler handler;
			if (contentFileHandlers.TryGetValue(contentType, out handler))
			{
				var ofd = new OpenFileDialog
				{
					Title = handler.Title,
					Filter = handler.Filter,
					Multiselect = handler.AllowMultipleFiles,
				};

				if ((bool)ofd.ShowDialog(this))
				{
					if (handler.AllowMultipleFiles)
						foreach (var file in ofd.FileNames)
							handler.FileHandler(this, file);
					else
						handler.FileHandler(this, ofd.FileName);
				}
			}
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
			newCacheTab.Content = new HaloMap(cacheLocation, newCacheTab, Settings.halomapTagSort);

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
		/// <summary>
		/// Add a new Patch Control
		/// </summary>
		/// <param name="patchLocation">Path to the Patch file</param>
		public void AddPatchTabModule(string patchLocation = null)
		{
			var newInfooTab = new CloseableTabItem
				                  {
					                  Tag = patchLocation,
					                  Header = new ContentControl
						                           {
							                           Content = "Patcher",
							                           ContextMenu = FilesystemContextMenu
						                           },
					                  Content = patchLocation == null ? new PatchControl() : new PatchControl(patchLocation)
				                  };

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
			VoxelConverter,
			PostGenerator
        }
        public void AddTabModule(TabGenre tabG, bool singleInstance = true)
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
				case TabGenre.PostGenerator:
					tab.Header = new ContentControl
					{
						Content = "Post Generator",
						ContextMenu = BaseContextMenu
					};
					tab.Content = new PostGenerator();
					break;
            }

			if (singleInstance)
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
            string suffix = "Assembly";
            if (!string.IsNullOrWhiteSpace(title))
                suffix = " - " + suffix;

            Title = title + suffix;
            lblTitle.Text = title + suffix;
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
	        Settings.applicationXBDMSidebarLocation = location;
			Settings.UpdateSettings();

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
            OpacityRect.Visibility = Visibility.Visible;
        }
        public void HideMask()
        {
            OpacityIndex--;

            if (OpacityIndex == 0) 
                OpacityRect.Visibility = Visibility.Collapsed;
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
            if (args != null && args.Count > 1)
            {
                string[] commandArgs = args.Skip(1).ToArray();
                if (commandArgs[0].StartsWith("assembly://"))
                    commandArgs[0] = commandArgs[0].Substring(11).Trim('/');
                
                // Decide what to do
                Activate();
                switch (commandArgs[0].ToLower())
                {
                    case "open":
                        // Determine type of file, and start it up, yo
                        if (commandArgs.Length > 1)
                            StartupDetermineType(commandArgs[1]);
                        break;

                    case "update":
                        // Show Update
                        menuHelpUpdater_Click(null, null);
                        break;

                    case "about": 
                        // Show About
                        menuHelpAbout_Click(null, null);
                        break;

                    case "settings":
                        // Show Settings
                        menuOpenSettings_Click(null, null);
                        break;

                    default:
                        return true;
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

                    switch (stream.ReadAscii(0x04).ToLower())
                    {
                        case "head":
                            // Map File
                            stream.Close();
                            AddCacheTabModule(path);
                            return;

						case "asmp":
							// Patch File
							stream.Close();
							AddPatchTabModule(path);
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
            {
                homeTabControl.SelectedIndex = 0;
                UpdateTitleText("");
            }
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
        private void menuPatches_Click(object sender, RoutedEventArgs e)					{ AddPatchTabModule(); }
        private void menuNetworkPoking_Click(object sender, RoutedEventArgs e)				{ AddTabModule(TabGenre.NetworkPoking); }
		private void menuPostGenerator_Click(object sender, RoutedEventArgs e) { AddTabModule(TabGenre.PostGenerator, false); }
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