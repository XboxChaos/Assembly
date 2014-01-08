using System;
using System.Windows;
using System.Windows.Controls;
using Atlas.Helpers.Tags;
using Atlas.ViewModels;

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
			ViewModel = new CachePageViewModel();
			ViewModel.LoadCache(cacheLocation);
			DataContext = ViewModel;
			InitializeComponent();

			// Set Datacontext's
			TagExplorerMetroContainer.DataContext = TagExplorerSearchTextbox.DataContext = ViewModel.CacheFile.InternalName;
			TagTreeView.DataContext = ViewModel.ActiveHierarchy;
			CacheInformationPropertyGrid.SelectedObject = ViewModel.CacheHeaderInformation;
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
	}
}
