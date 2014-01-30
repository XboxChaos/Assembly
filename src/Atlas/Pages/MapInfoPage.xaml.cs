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
using Atlas.Pages.CacheEditors;
using Atlas.Pages.CacheEditors.TagEditorComponents.Data;
using Atlas.ViewModels;
using Blamite.Blam.Scripting;
using XBDMCommunicator;

namespace Atlas.Pages
{
	/// <summary>
	/// Interaction logic for MapInfoPage.xaml
	/// </summary>
	public partial class MapInfoPage : IAssemblyPage
	{
		public readonly MapInfoPageViewModel ViewModel;

		public MapInfoPage(string mapInfoLocation)
		{
			InitializeComponent();

			ViewModel = new MapInfoPageViewModel(this);
			DataContext = ViewModel;
			ViewModel.LoadMapInfo(mapInfoLocation);
		}

		public bool Close()
		{
			var result = MetroMessageBox.Show("Are you sure?",
				"Are you sure you want to close this mapinfo? Unsaved changes will be lost.",
				new List<MetroMessageBox.MessageBoxButton>
				{
					MetroMessageBox.MessageBoxButton.Yes,
					MetroMessageBox.MessageBoxButton.No,
					MetroMessageBox.MessageBoxButton.Cancel
				});

			return (result == MetroMessageBox.MessageBoxButton.No);
		}
	}
}
