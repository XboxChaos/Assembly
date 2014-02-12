using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Atlas.Dialogs;
using Atlas.Helpers.Tags;
using Atlas.Metro.Controls.Custom;
using Atlas.Models;
using Atlas.ViewModels;
using Atlas.Views.CacheEditors;
using Atlas.Views.CacheEditors.TagEditorComponents.Data;
using Blamite.Blam.Scripting;
using XBDMCommunicator;

namespace Atlas.Views
{
	/// <summary>
	/// Interaction logic for CachePage.xaml
	/// </summary>
	public partial class CachePage : IAssemblyPage
	{
		public readonly CachePageViewModel ViewModel;

		public CachePage(string cacheLocation)
		{
			InitializeComponent();

			ViewModel = new CachePageViewModel(this);
			DataContext = ViewModel;
			ViewModel.LoadCache(cacheLocation);

			// Set Datacontext's
			XdkIpAddressTextBox.DataContext = App.Storage.Settings;

			// woo
			AddHandler(MetroClosableTabItem.CloseTabEvent, new RoutedEventHandler(CloseTab));
		}

		public bool Close()
		{
			var letsContinue = ViewModel.Editors.All(editor => editor.Close());

			if (!letsContinue)
				return false;

			// r u shure
			var result = MetroMessageBox.Show("Are you sure?",
				"Are you sure you want to close this cache? Unsaved changes will be lost.",
				new List<MetroMessageBox.MessageBoxButton>
				{
					MetroMessageBox.MessageBoxButton.Yes,
					MetroMessageBox.MessageBoxButton.No,
					MetroMessageBox.MessageBoxButton.Cancel
				});

			return (result == MetroMessageBox.MessageBoxButton.No);
		}

		private void OpenTagContextMenu_OnClick(object sender, RoutedEventArgs e)
		{
			var tagHierarchyNode = NodeFromContextMenu(sender);
			if (tagHierarchyNode == null) return;

			ViewModel.LoadTagEditor(tagHierarchyNode);
		}
		private void ExtractTagContextMenu_OnClick(object sender, RoutedEventArgs e)
		{
			var tagHierarchyNode = NodeFromContextMenu(sender);
			if (tagHierarchyNode == null) return;

			// yea
		}
		private void RenameNodeContextMenu_OnClick(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}

		private void ScriptButton_OnClick(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button == null) return;

			ViewModel.LoadScriptEditor(button.DataContext as IScriptFile);
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
				if (MetroMessageBox.Show("careful", "this could fuck shit up. are you sure you want to continue?",
					new List<MetroMessageBox.MessageBoxButton>
					{
						MetroMessageBox.MessageBoxButton.Yes,
						MetroMessageBox.MessageBoxButton.No,
						MetroMessageBox.MessageBoxButton.Cancel
					}) != MetroMessageBox.MessageBoxButton.Yes)
					return;

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

		#region Xbdm Sidebar

		private void AdvancedMemoryModificationButton_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.LoadAdvancedMemoryEditor();
		}

		private void NetworkSessionButton_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.LoadNetworkSessionEditor();
		}

		#endregion


		#region Tag Editor Toolbar Buttons

		private void TagEditorSaveButton_OnClick(object sender, RoutedEventArgs e)
		{
			var editor = ViewModel.SelectedEditor as TagEditor;
			if (editor == null) return;
			editor.ViewModel.SaveTagData(TagDataWriter.SaveType.File);
		}

		private void TagEditorPokeModifiedMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			var editor = ViewModel.SelectedEditor as TagEditor;
			if (editor == null) return;
			editor.ViewModel.SaveTagData(TagDataWriter.SaveType.Memory);
			TagEditorPokeDropDownButton.IsOpen = false;
			editor.Focus();
		}
		private void TagEditorPokeAllMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			var editor = ViewModel.SelectedEditor as TagEditor;
			if (editor == null) return;
			editor.ViewModel.SaveTagData(TagDataWriter.SaveType.Memory, false);
			TagEditorPokeDropDownButton.IsOpen = false;
			editor.Focus();
		}

		private void TagEditorReloadLocalMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			var editor = ViewModel.SelectedEditor as TagEditor;
			if (editor == null) return;
			editor.ViewModel.LoadTagData(TagDataReader.LoadType.File, editor);
			TagEditorReloadDropDownButton.IsOpen = false;
			editor.Focus();
		}
		private void TagEditorReloadMemoryMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			var editor = ViewModel.SelectedEditor as TagEditor;
			if (editor == null) return;
			editor.ViewModel.LoadTagData(TagDataReader.LoadType.Memory, editor);
			TagEditorReloadDropDownButton.IsOpen = false;
			editor.Focus();
		}

		#endregion

		#region Script Editor Toolbar Buttons

		private void ScriptEditorExportButton_OnClick(object sender, RoutedEventArgs e)
		{
			var editor = ViewModel.SelectedEditor as ScriptEditor;
			if (editor == null) return;
			editor.ViewModel.ExportScript();
		}

		#endregion


		private void CloseTab(object source, RoutedEventArgs args)
		{
			var editor = ((MetroClosableTabItem)args.OriginalSource).Content as ICacheEditor;
			if (editor == null) return;
			if (editor.Close()) ViewModel.Editors.Remove(editor);
		}
	}
}
