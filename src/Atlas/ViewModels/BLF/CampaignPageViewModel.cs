using System;
using System.IO;
using System.Threading;
using System.Windows;
using Atlas.Dialogs;
using Atlas.Models;
using Atlas.Models.BLF;
using Atlas.Views.BLF;
using Blamite.Blam.ThirdGen;
using Blamite.Blam.ThirdGen.BLF;

namespace Atlas.ViewModels.BLF
{
	public class CampaignPageViewModel : Base
	{
		public CampaignPage CampaignPage { get; private set; }

		public CampaignPageViewModel(CampaignPage campaignPage)
		{
			CampaignPage = campaignPage;
		}

		#region Properties

		public string CampaignLocation
		{
			get { return _campaignLocation; }
			private set { SetField(ref _campaignLocation, value); }
		}
		private string _campaignLocation;

		public Campaign CampaignFile
		{
			get { return _campaign; }
			private set { SetField(ref _campaign, value); }
		}
		private Campaign _campaign;

		public CampaignBLF CampaignBLF
		{
			get { return _campaignBLF; }
			private set { SetField(ref _campaignBLF, value); }
		}
		private CampaignBLF _campaignBLF;

		#endregion

		public void LoadCampaign(string campaignLocation)
		{
			//App.Storage.HomeWindowViewModel.AssemblyPage = null;
			var dialog = MetroBusyAlertBox.Show();

			var thread = new Thread(() =>
			{
				CampaignLocation = campaignLocation;
				CampaignBLF = new CampaignBLF();

				// Validate the BLF file, add info to the Model
				PureBLF _blf = new PureBLF(CampaignLocation);
				if (_blf.BLFChunks[1].ChunkMagic != "cmpn")
					throw new Exception("The selected BLF is not a valid Campaign file.");

				CampaignBLF.ChunkCount = _blf.BLFChunks.Count.ToString();
				CampaignBLF.Length = "0x" + _blf.BLFStream.Length.ToString("X8");
				CampaignBLF.CmpnLength = "0x" + _blf.BLFChunks[1].ChunkLength.ToString("X8");
				CampaignBLF.CmpnVersion = BitConverter.ToInt16(BitConverter.GetBytes(_blf.BLFChunks[1].ChunkFlags), 2).ToString();

				_blf.Close();

				// Open as a Campaign File
				var fileInfo = new FileInfo(CampaignLocation);
				CampaignFile = new Campaign(CampaignLocation);

				switch (CampaignFile.HaloCampaign.Game)
				{
					case Campaign.GameIdentifier.Halo3:
						CampaignBLF.Game = "Pre-Halo 4";
						break;
					case Campaign.GameIdentifier.Halo4:
						CampaignBLF.Game = "Halo 4";
						break;

					default:
						CampaignBLF.Game = "Unknown";
						break;
				}

				Application.Current.Dispatcher.Invoke(delegate
				{
					App.Storage.HomeWindowViewModel.UpdateStatus(
					   String.Format("{0} ({1})", CampaignFile.HaloCampaign.MapNames[0], fileInfo.Name));

					dialog.ViewModel.CanClose = true;
					dialog.Close();
					App.Storage.HomeWindowViewModel.HideDialog();
					//App.Storage.HomeWindowViewModel.AssemblyPage = CampaignPage;
				});
			});
			thread.Start();
		}
	}
}
