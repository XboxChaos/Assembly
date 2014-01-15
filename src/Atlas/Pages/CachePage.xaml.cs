using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Atlas.Dialogs;
using Atlas.Helpers.Tags;
using Atlas.Models;
using Atlas.ViewModels;
using XBDMCommunicator;

namespace Atlas.Pages
{
	/// <summary>
	/// Interaction logic for CachePage.xaml
	/// </summary>
	public partial class CachePage : IAssemblyPage
	{
		public readonly CachePageViewModel ViewModel;

		public CachePage(string cacheLocation)
		{
			ViewModel = new CachePageViewModel(this);
			ViewModel.LoadCache(cacheLocation);
			DataContext = ViewModel;
			InitializeComponent();

			// Set Datacontext's
			TagExplorerMetroContainer.DataContext = TagExplorerSearchTextbox.DataContext = ViewModel.CacheFile.InternalName;
			TagTreeView.DataContext = ViewModel.ActiveHierarchy;
			CacheInformationPropertyGrid.SelectedObject = ViewModel.CacheHeaderInformation;
			XdkIpAddressTextBox.DataContext = App.Storage.Settings;
		}

		public bool Close()
		{
			// Ask for user permission to close
			throw new NotImplementedException();
		}

		private void OpenTagContextMenu_OnClick(object sender, RoutedEventArgs e)
		{
			var tagHierarchyNode = NodeFromContextMenu(sender);
			if (tagHierarchyNode == null) return;

			ViewModel.LoadTagEditor(tagHierarchyNode);
		}
		private void ExtractTagContextMenu_OnClick(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}
		private void RenameNodeContextMenu_OnClick(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}

		#region Helpers

		public TagHierarchyNode NodeFromContextMenu(object sender)
		{
			var menuItem = sender as MenuItem;
			if (menuItem == null) return null;

			var contextMenu = menuItem.Parent as ContextMenu;
			if (contextMenu == null) return null;

			return contextMenu.DataContext as TagHierarchyNode;
		}

		#endregion

		#region Xbdm Toolbar

		private void XbdmToolbarFreezeButton_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.FreezeConsole();
		}

		private void XbdmToolbarUnfreezeButton_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.UnfreezeConsole();
		}

		private void XbdmToolbarScreenshotButton_OnClick(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}

		private void XbdmToolbarColdRebootButton_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.RebootConsole(Xbdm.RebootType.Cold);
		}

		private void XbdmToolbarTitleRebootButton_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.RebootConsole(Xbdm.RebootType.Title);
		}

		private void XbdmToolbarActiveTitleRebootButton_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.RebootConsole(Xbdm.RebootType.ActiveTitle);
		}

		#endregion

		#region Xbdm Quick Options

		private void QuickOptionButton_OnClick(object sender, RoutedEventArgs e)
		{
			var element = (FrameworkElement)sender;
			if (element == null) return;
			var quickOption = element.DataContext as EngineMemory.EngineVersion.QuickOption;
			if (quickOption == null) return;

			if (quickOption.CarefulMode)
				MetroMessageBox.Show("careful", "this could fuck shit up.");

			byte value = 0x01;
			if (quickOption.IsToggle)
			{
				var toggleButton = element as ToggleButton;
				if (toggleButton != null && !(toggleButton.IsChecked ?? true))
					value = 0x00;
			}
			ViewModel.PokeByte(quickOption.Address, value);
		}

		#endregion
	}
}
