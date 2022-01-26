using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Resources;
using System.Windows.Threading;
using Assembly.Helpers;
using Assembly.Helpers.Native;
using Assembly.Helpers.Net;
using Assembly.Metro.Controls.PageTemplates;
using Assembly.Metro.Controls.PageTemplates.Games;
using Assembly.Metro.Controls.PageTemplates.Tools;
using Assembly.Metro.Controls.PageTemplates.Tools.Halo4;
using Assembly.Metro.Dialogs;
using Xceed.Wpf.AvalonDock.Layout;
using Blamite.Blam.ThirdGen;
using Blamite.IO;
using Microsoft.Win32;
using XboxChaos.Models;
using XBDMCommunicator;
using Xceed.Wpf.AvalonDock.Controls;
using Assembly.Helpers.Net.Sockets;

namespace Assembly.Windows
{
	/// <summary>
	///     Interaction logic for Home.xaml
	/// </summary>
	public partial class Home
	{
		public Home()
		{
			InitializeComponent();

			DwmDropShadow.DropShadowToWindow(this);

			UpdateTitleText("");
			UpdateStatusText("Ready...");

			//Window_StateChanged(null, null);
			ClearTabs();

			if (App.AssemblyStorage.AssemblySettings.StartpageShowOnLoad)
				AddTabModule(TabGenre.StartPage);

			// Do sidebar Loading stuff
			//SwitchXBDMSidebarLocation(App.AssemblyStorage.AssemblySettings.applicationXBDMSidebarLocation);
			//XBDMSidebarTimerEvent();

			// Set width/height/state from last session
			if (!double.IsNaN(App.AssemblyStorage.AssemblySettings.ApplicationSizeHeight) &&
			    App.AssemblyStorage.AssemblySettings.ApplicationSizeHeight > MinHeight)
				Height = App.AssemblyStorage.AssemblySettings.ApplicationSizeHeight;
			if (!double.IsNaN(App.AssemblyStorage.AssemblySettings.ApplicationSizeWidth) &&
			    App.AssemblyStorage.AssemblySettings.ApplicationSizeWidth > MinWidth)
				Width = App.AssemblyStorage.AssemblySettings.ApplicationSizeWidth;

			WindowState = App.AssemblyStorage.AssemblySettings.ApplicationSizeMaximize
				? WindowState.Maximized
				: WindowState.Normal;
			Window_StateChanged(null, null);

			AllowDrop = true;
			App.AssemblyStorage.AssemblySettings.HomeWindow = this;
		}

		protected override async void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			IntPtr handle = (new WindowInteropHelper(this)).Handle;
			HwndSource hwndSource = HwndSource.FromHwnd(handle);
			if (hwndSource != null)
				hwndSource.AddHook(WindowProc);

			ProcessCommandLineArgs(Environment.GetCommandLineArgs());

			if (App.AssemblyStorage.AssemblySettings.ApplicationUpdateOnStartup)
				await CheckForUpdates();
		}

		private async Task CheckForUpdates()
		{
			// Grab JSON Update package from the server
			try
			{
				var result = await Updater.GetBranchInfo();
				if (result == null)
					return;
				if (Updater.UpdateAvailable(result))
					MetroUpdateDialog.Show(result, true);
			}
			catch
			{
			}
		}

		private void LayoutRoot_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var activeContent = ((LayoutRoot)sender).ActiveContent;

			if (activeContent == null)
			{
				UpdateTitleText("");
				return;
			}

			if (e.PropertyName == "ActiveContent")
			{
				if (activeContent.Content != null)
					UpdateTitleText(activeContent.Title.Replace("__", "_")
						.Replace(".mapinfo", "").Replace(".map", "").Replace(".campaign", "").Replace(".blf", ""));

				if (activeContent != null && activeContent.Title == "Start Page")
					((StartPage)activeContent.Content).UpdateRecents();

				if (activeContent != null && activeContent.Title == "Imgur History")
					((ImgurHistoryPage)activeContent.Content).UpdateHistory();
			}
		}

		internal void SessionManager_ClientConnected(object sender, ClientEventArgs e)
		{
			Dispatcher.InvokeAsync((Action)delegate
			{
				App.AssemblyStorage.AssemblyNetworkPoke.Clients.Add(e.ClientInfo);
			});
		}

		internal void SessionManager_ClientDisconnected(object sender, ClientEventArgs e)
		{
			Dispatcher.InvokeAsync((Action)delegate
			{
				App.AssemblyStorage.AssemblyNetworkPoke.Clients.Remove(e.ClientInfo);
			});
		}

		// File
		private void menuOpenFile_Click(object sender, RoutedEventArgs e)
		{
			OpenContentFile();
		}

		// Edit
		private void menuOpenSettings_Click(object sender, EventArgs e)
		{
			AddTabModule(TabGenre.Settings);
		}

		// Tools
		private void menuToolHalo4VoxelConverter_Click(object sender, RoutedEventArgs e)
		{
			AddTabModule(TabGenre.VoxelConverter);
		}

		private void menuCompress_Click(object sender, RoutedEventArgs e)
		{
			AddTabModule(TabGenre.MapCompressor, true);
		}

		internal void SessionManager_SessionDied(object sender, SessionDiedEventArgs e)
		{
			Dispatcher.InvokeAsync((Action)delegate
			{
				App.AssemblyStorage.AssemblyNetworkPoke.Clients.Clear();
				App.AssemblyStorage.AssemblyNetworkPoke.NetworkRteProvider.Kill();
				App.AssemblyStorage.AssemblyNetworkPoke.IsConnected = false;
				App.AssemblyStorage.AssemblyNetworkPoke.IsServer = false;
				App.AssemblyStorage.AssemblyNetworkPoke.NetworkRteProvider = null;
				App.AssemblyStorage.AssemblyNetworkPoke.PokeSessionManager = null;
				if (e != null && !(e.Error is IOException))
				{
					MetroException.Show(e.Error);
				}
				MetroMessageBox.Show("Group Poking Killed", "Peer poking session has stopped.  Reverting to local poking...");
			});
		}

		internal void ServerSessionManager_SessionActivated(object sender, EventArgs e)
		{
			Dispatcher.InvokeAsync((Action)delegate
			{
				App.AssemblyStorage.AssemblyNetworkPoke.Clients.Clear();
				App.AssemblyStorage.AssemblyNetworkPoke.NetworkRteProvider = new SocketRTEProvider(App.AssemblyStorage.AssemblyNetworkPoke.PokeSessionManager);
				App.AssemblyStorage.AssemblyNetworkPoke.IsConnected = true;
				App.AssemblyStorage.AssemblyNetworkPoke.IsServer = true;
			});
		}

		internal void ClientSessionManager_SessionActivated(object sender, EventArgs e)
		{
			Dispatcher.InvokeAsync((Action)delegate
			{
				App.AssemblyStorage.AssemblyNetworkPoke.NetworkRteProvider = new SocketRTEProvider(App.AssemblyStorage.AssemblyNetworkPoke.PokeSessionManager);
				App.AssemblyStorage.AssemblyNetworkPoke.IsConnected = true;
				App.AssemblyStorage.AssemblyNetworkPoke.IsServer = false;
			});
		}

		// View
		private void menuViewStartPage_Click(object sender, RoutedEventArgs e)
		{
			AddTabModule(TabGenre.StartPage);
		}

		private void menuViewImgurHistoryPage_Click(object sender, RoutedEventArgs e)
		{
			AddTabModule(TabGenre.ImgurHistory);
		}

		private void menuPatches_Click(object sender, RoutedEventArgs e)
		{
			AddPatchTabModule();
		}

		private void menuPostGenerator_Click(object sender, RoutedEventArgs e)
		{
			AddTabModule(TabGenre.PostGenerator, false);
		}

		private void menuPluginGeneration_Click(object sender, RoutedEventArgs e)
		{
			AddTabModule(TabGenre.PluginGenerator);
		}

		private void menuPluginConverter_Click(object sender, RoutedEventArgs e)
		{
			AddTabModule(TabGenre.PluginConverter);
		}

		private void menuNetworkPoking_Click(object sender, RoutedEventArgs e)
		{
			AddTabModule(TabGenre.NetworkPoking);
		}

		//xbdm
		private void menuScreenshot_Click(object sender, RoutedEventArgs e)
		{
			var screenshotFileName = Path.GetTempFileName();
			try
			{
				if (App.AssemblyStorage.AssemblySettings.Xbdm.GetScreenshot(screenshotFileName))
					App.AssemblyStorage.AssemblySettings.HomeWindow.AddScrenTabModule(screenshotFileName);
				else
					MetroMessageBox.Show("Not Connected", "You are not connected to a debug Xbox 360.");
			}
			finally
			{
				File.Delete(screenshotFileName);
			}
		}

		private void menuFreeze_Click(object sender, RoutedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.Xbdm.Freeze();
		}

		private void menuUnfreeze_Click(object sender, RoutedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.Xbdm.Unfreeze();
		}

		private void menuRebootTitle_Click(object sender, RoutedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.Xbdm.Reboot(Xbdm.RebootType.Title);
		}

		private void menuRebootCold_Click(object sender, RoutedEventArgs e)
		{
			App.AssemblyStorage.AssemblySettings.Xbdm.Reboot(Xbdm.RebootType.Cold);
		}

		// Help
		private void menuHelpMapNames_Click(object sender, RoutedEventArgs e)
		{
			AddTabModule(TabGenre.MapNames);
		}

		private void menuHelpAbout_Click(object sender, RoutedEventArgs e)
		{
			MetroAbout.Show();
		}

		private async void menuHelpUpdater_Click(object sender, RoutedEventArgs e)
		{
			await Updater.BeginUpdateProcess();
		}

		// Goodbye Sweet Evelyn
		private void menuCloseApplication_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void ShowCheatingDialog()
		{
			MetroMessageBox.Show("Assembly",
				"Assembly is not a cheating tool. While you will never be prevented from using it to give yourself an unfair advantage on Xbox Live, do not expect to receive help if you ask how to do so.\n\nThis dialog will only show once.");
		}

		#region Waste of Space, idk man

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

		#region More WPF Annoyance

		private void ResizeDrop_DragDelta(object sender, DragDeltaEventArgs e)
		{
			double yadjust = Height + e.VerticalChange;
			double xadjust = Width + e.HorizontalChange;

			if (xadjust > MinWidth)
				Width = xadjust;
			if (yadjust > MinHeight)
				Height = yadjust;
		}

		private void ResizeRight_DragDelta(object sender, DragDeltaEventArgs e)
		{
			double xadjust = Width + e.HorizontalChange;

			if (xadjust > MinWidth)
				Width = xadjust;
		}

		private void ResizeBottom_DragDelta(object sender, DragDeltaEventArgs e)
		{
			double yadjust = Height + e.VerticalChange;

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
					btnActionMaxamize.Visibility =
						ResizeDropVector.Visibility =
							ResizeDrop.Visibility = ResizeRight.Visibility = ResizeBottom.Visibility = Visibility.Visible;
					break;
				case WindowState.Maximized:
					borderFrame.BorderThickness = new Thickness(0, 0, 0, 23);
					btnActionRestore.Visibility = Visibility.Visible;
					btnActionMaxamize.Visibility =
						ResizeDropVector.Visibility =
							ResizeDrop.Visibility = ResizeRight.Visibility = ResizeBottom.Visibility = Visibility.Collapsed;
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
			var mmi = (Monitor_Workarea.MINMAXINFO) Marshal.PtrToStructure(lParam, typeof (Monitor_Workarea.MINMAXINFO));

			// Adjust the maximized size and position to fit the work area of the correct monitor
			const int monitorDefaulttonearest = 0x00000002;
			IntPtr monitor = Monitor_Workarea.MonitorFromWindow(hwnd, monitorDefaulttonearest);

			if (monitor != IntPtr.Zero)
			{
				var monitorInfo = new Monitor_Workarea.MONITORINFO();
				Monitor_Workarea.GetMonitorInfo(monitor, monitorInfo);
				Monitor_Workarea.RECT rcWorkArea = monitorInfo.rcWork;
				Monitor_Workarea.RECT rcMonitorArea = monitorInfo.rcMonitor;
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

		#region Content Management

		private delegate void ContentFileHandler(Home home, string path);

		private readonly Dictionary<string, ContentFileHandler> _contentFileHandlers = new Dictionary
			<string, ContentFileHandler>
		{
			{ ".map", (home, path) => home.AddCacheTabModule(path) },
			{ ".blf", (home, path) => home.AddImageTabModule(path) },
			{ ".mapinfo", (home, path) => home.AddInfooTabModule(path) },
			{ ".campaign", (home, path) => home.AddCampaignTabModule(path) },
			{ ".asmp", (home, path) => home.AddPatchTabModule(path) },
			{ ".ascpatch", (home, path) => home.AddPatchTabModule(path) },
			{ ".patchdat", (home, path) => home.AddPatchTabModule(path) },
			{ ".module", (home, path) => home.AddCacheTabModule(path) }
		};

		/// <summary>
		///     Open a new Blam Engine File
		/// </summary>
		public void OpenContentFile()
		{
			var filter = "Blam Files|" + _contentFileHandlers.Keys.Select(e => "*" + e).Aggregate((f, n) => f + ";" + n);
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Open File",
				Multiselect = true,
				Filter = filter
			};

			if (!(bool) ofd.ShowDialog(this)) return;

			foreach (string file in ofd.FileNames)
				ProcessContentFile(file);
		}

		public void ProcessContentFile(string file)
		{
			if (!File.Exists(file))
			{
				MetroMessageBox.Show("Unable to find file", "The selected file could no longer be found");
				return;
			}

			var extension = (Path.GetExtension(file) ?? "").ToLowerInvariant();
			ContentFileHandler handler;
			if (!_contentFileHandlers.TryGetValue(extension, out handler))
			{
				MetroMessageBox.Show("Assembly - Unsupported File Type",
					"\"" + file + "\" cannot be opened because its extension is not recognized.");
				return;
			}
			handler(this, file);
		}

		#endregion

		#region Tab Manager

		public enum TabGenre
		{
			StartPage,
			Settings,
			PluginGenerator,
			Welcome,
			PluginConverter,
			ImgurHistory,

			MemoryManager,
			VoxelConverter,
			PostGenerator,
			MapNames,
			NetworkPoking,
			MapCompressor
		}

		public void ExternalTabClose(TabGenre tabGenre)
		{
			string tabHeader = "";
			switch (tabGenre)
			{
				case TabGenre.StartPage:
					tabHeader = "Start Page";
					break;
				case TabGenre.Settings:
					tabHeader = "Settings";
					break;
			}
			LayoutDocument toRemove = null;
			foreach (LayoutContent tab in documentManager.Children.Where(tab => tab.Title == tabHeader && tab is LayoutDocument))
				toRemove = (LayoutDocument) tab;

			if (toRemove != null)
				documentManager.Children.Remove(toRemove);
		}

		public void ExternalTabClose(LayoutDocument tab)
		{
			documentManager.Children.Remove(tab);

			if (documentManager.Children.Count > 0)
				documentManager.SelectedContentIndex = documentManager.Children.Count - 1;
		}

		public void ClearTabs()
		{
			documentManager.Children.Clear();
		}

		/// <summary>
		///     Add a new Blam Cache Editor Container
		/// </summary>
		/// <param name="cacheLocation">Path to the Blam Cache File</param>
		public void AddCacheTabModule(string cacheLocation)
		{
			if (ContentModuleExists(cacheLocation))
				return;

			var newCacheTab = new LayoutDocument
			{
				ContentId = cacheLocation,
				Title = "",
				ToolTip = cacheLocation
			};
			newCacheTab.Content = new HaloMap(cacheLocation, newCacheTab, App.AssemblyStorage.AssemblySettings.HalomapTagSort);
			newCacheTab.Closing += HaloMap_Closing;
			documentManager.Children.Add(newCacheTab);
			documentManager.SelectedContentIndex = documentManager.IndexOfChild(newCacheTab);
		}

		/// <summary>
		///     Add a new XBox Screenshot Editor Container
		/// </summary>
		/// <param name="tempImageLocation">Path to the temporary location of the image</param>
		public void AddScrenTabModule(string tempImageLocation)
		{
			var newScreenshotTab = new LayoutDocument
			{
				ContentId = tempImageLocation,
				Title = "Screenshot",
				ToolTip = tempImageLocation
			};
			newScreenshotTab.Content = new HaloScreenshot(tempImageLocation, newScreenshotTab);
			documentManager.Children.Add(newScreenshotTab);
			documentManager.SelectedContentIndex = documentManager.IndexOfChild(newScreenshotTab);
		}

		/// <summary>
		///     Add a new BLF Editor Container
		/// </summary>
		/// <param name="imageLocation">Path to the BLF file</param>
		public void AddImageTabModule(string imageLocation)
		{
			if (ContentModuleExists(imageLocation))
				return;

			var newMapImageTab = new LayoutDocument
			{
				ContentId = imageLocation,
				Title = "Image",
				ToolTip = imageLocation
			};
			newMapImageTab.Content = new HaloImage(imageLocation, newMapImageTab);
			documentManager.Children.Add(newMapImageTab);
			documentManager.SelectedContentIndex = documentManager.IndexOfChild(newMapImageTab);
		}

		/// <summary>
		///     Add a new MapInfo Editor Container
		/// </summary>
		/// <param name="infooLocation">Path to the MapInfo file</param>
		public void AddInfooTabModule(string infooLocation)
		{
			if (ContentModuleExists(infooLocation))
				return;

			var newMapInfoTab = new LayoutDocument
			{
				ContentId = infooLocation,
				Title = "Info",
				ToolTip = infooLocation
			};
			newMapInfoTab.Content = new HaloInfo(infooLocation, newMapInfoTab);
			documentManager.Children.Add(newMapInfoTab);
			documentManager.SelectedContentIndex = documentManager.IndexOfChild(newMapInfoTab);
		}

		/// <summary>
		///     Add a new Campaign Editor Container
		/// </summary>
		/// <param name="campaignLocation">Path to the Campaign file</param>
		public void AddCampaignTabModule(string campaignLocation)
		{
			if (ContentModuleExists(campaignLocation))
				return;

			var newCampaignTab = new LayoutDocument
			{
				ContentId = campaignLocation,
				Title = "Campaign",
				ToolTip = campaignLocation
			};
			newCampaignTab.Content = new HaloCampaign(campaignLocation, newCampaignTab);
			documentManager.Children.Add(newCampaignTab);
			documentManager.SelectedContentIndex = documentManager.IndexOfChild(newCampaignTab);
		}

		/// <summary>
		///     Add a new Patch Control
		/// </summary>
		/// <param name="patchLocation">Path to the Patch file</param>
		public void AddPatchTabModule(string patchLocation = null)
		{
			var newPatchTab = new LayoutDocument
			{
				Title = "Patcher",
				Content = (patchLocation != null) ? new PatchControl(patchLocation) : new PatchControl()
			};
			documentManager.Children.Add(newPatchTab);
			newPatchTab.Closing += PatchControl_Closing;
			documentManager.SelectedContentIndex = documentManager.IndexOfChild(newPatchTab);
		}

		/// <summary>
		/// Iterates all tabs and child windows for an instance of the given file.
		/// </summary>
		/// <param name="contentId">The file path to check for.</param>
		/// <param name="focus">If a windor or tab is found, should it be brought to focus? Default is true.</param>
		/// <returns>Whether the file is currently open.</returns>
		public bool ContentModuleExists(string contentId, bool focus = true)
		{
			// Check module isn't already open in the main window
			foreach (LayoutContent tab in documentManager.Children.Where(tab => tab.ContentId == contentId))
			{
				//focus existing
				if (focus)
					documentManager.SelectedContentIndex = documentManager.IndexOfChild(tab);
				return true;
			}
			// Check module isn't already open as a floating window
			foreach (LayoutDocumentFloatingWindowControl windo in dockManager.FloatingWindows)
			{
				LayoutDocumentFloatingWindow mod = (LayoutDocumentFloatingWindow)windo.Model;
				if (mod.RootDocument.ContentId == contentId)
				{
					//focus existing
					if (focus)
					{
						mod.RootDocument.IsActive = true;
						windo.Focus();
					}
					return true;
				}
			}

			return false;
		}

		public void AddTabModule(TabGenre tabG, bool singleInstance = true)
		{
			var tab = new LayoutDocument();

			switch (tabG)
			{
				case TabGenre.StartPage:
					tab.Title = "Start Page";
					tab.Content = new StartPage();
					break;
				case TabGenre.Welcome:
					tab.Title = "Welcome";
					tab.Content = new WelcomePage();
					break;
				case TabGenre.Settings:
					tab.Title = "Settings";
					tab.Content = new SettingsPage();
					break;
				case TabGenre.PluginGenerator:
					tab.Title = "Plugin Generator";
					tab.Content = new HaloPluginGenerator();
					break;
				case TabGenre.PluginConverter:
					tab.Title = "Plugin Converter";
					tab.Content = new HaloPluginConverter();
					break;
				case TabGenre.ImgurHistory:
					tab.Title = "Imgur History";
					tab.Content = new ImgurHistoryPage();
					break;


				case TabGenre.MemoryManager:
					tab.Title = "Memory Manager";
					tab.Content = new MemoryManager();
					break;
				case TabGenre.VoxelConverter:
					tab.Title = "Voxel Converter";
					tab.Content = new VoxelConverter();
					break;
				case TabGenre.PostGenerator:
					tab.Title = "Post Generator";
					tab.Content = new PostGenerator();
					break;

				case TabGenre.NetworkPoking:
					tab.Title = "Group Poking";
					tab.Content = new NetworkPoking();
					break;

				case TabGenre.MapNames:
					tab.Title = "Map Names";
					tab.Content = new MapNames();
					break;

				case TabGenre.MapCompressor:
					tab.Title = "Map Compressor";
					tab.Content = new MapCompressor();
					break;
			}

			if (singleInstance)
				foreach (LayoutContent tabb in documentManager.Children.Where(tabb => tabb.Title == tab.Title))
				{
					documentManager.SelectedContentIndex = documentManager.IndexOfChild(tabb);
					return;
				}

			documentManager.Children.Add(tab);
			documentManager.SelectedContentIndex = documentManager.IndexOfChild(tab);
		}

		#endregion

		#region Public Access Modifiers

		private readonly DispatcherTimer _statusUpdateTimer = new DispatcherTimer();

		/// <summary>
		///     Set the title text of Assembly
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
		///     Set the status text of Assembly
		/// </summary>
		/// <param name="status">Current Status of Assembly</param>
		public void UpdateStatusText(string status)
		{
			Status.Text = status;

			_statusUpdateTimer.Stop();
			_statusUpdateTimer.Interval = new TimeSpan(0, 0, 0, 4);
			_statusUpdateTimer.Tick += statusUpdateCleaner_Clear;
			_statusUpdateTimer.Start();
		}

		private void statusUpdateCleaner_Clear(object sender, EventArgs e)
		{
			Status.Text = "Ready...";
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
			// Win7 master race lol
			string[] draggedFiles = (string[])e.Data.GetData(DataFormats.FileDrop, true);
			this.Focus();

			foreach (string file in draggedFiles)
				ProcessContentFile(file);
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

					case "postupdate":
						var version = VersionInfo.GetUserFriendlyVersion();
						if (version != null)
						{
							MetroMessageBox.Show("Update successful!",
								"Assembly has been updated to version " + version +
								".\r\n\r\nYour old plugins and layouts were copied to a \"Backup\" folder.\r\nBe sure to merge any changes you have made on your own.");
						}
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
				ProcessContentFile(path);
			}
			catch (Exception ex)
			{
				MetroException.Show(ex);
			}
		}

		#endregion

		private void HaloMap_Closing(object sender, CancelEventArgs e)
		{
			LayoutDocument ld = (LayoutDocument)sender;
			HaloMap mp = (HaloMap)ld.Content;

			mp.Dispose();
		}

		private void PatchControl_Closing(object sender, CancelEventArgs e)
		{
			LayoutDocument ld = (LayoutDocument)sender;
			PatchControl mp = (PatchControl)ld.Content;

			mp.Dispose();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (!App.AssemblyStorage.AssemblySettings.ShownCheatingDialog)
			{
				ShowCheatingDialog();
				App.AssemblyStorage.AssemblySettings.ShownCheatingDialog = true;
			}
		}
	}
}