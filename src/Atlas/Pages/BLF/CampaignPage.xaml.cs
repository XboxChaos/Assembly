﻿using System.Collections.Generic;
using Atlas.Dialogs;
using Atlas.ViewModels.BLF;

namespace Atlas.Pages.BLF
{
	/// <summary>
	/// Interaction logic for CampaignPage.xaml
	/// </summary>
	public partial class CampaignPage : IAssemblyPage
	{
		public readonly CampaignPageViewModel ViewModel;

		public CampaignPage(string mapInfoLocation)
		{
			InitializeComponent();

			ViewModel = new CampaignPageViewModel(this);
			DataContext = ViewModel;
			ViewModel.LoadCampaign(mapInfoLocation);
		}

		public bool Close()
		{
			var result = MetroMessageBox.Show("Are you sure?",
				"Are you sure you want to close this campaign? Unsaved changes will be lost.",
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
