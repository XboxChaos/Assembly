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
	public class MapInfoPageViewModel : Base
	{
		public MapInfoPage MapInfoPage { get; private set; }

		public MapInfoPageViewModel(MapInfoPage mapInfoPage)
		{
			MapInfoPage = mapInfoPage;
		}

		#region Properties

		public string MapInfoLocation
		{
			get { return _mapInfoLocation; }
			private set { SetField(ref _mapInfoLocation, value); }
		}
		private string _mapInfoLocation;

		public MapInfo MapInfoFile
		{
			get { return _mapInfo; }
			private set { SetField(ref _mapInfo, value); }
		}
		private MapInfo _mapInfo;

		public MapInfoBLF MapInfoBLF
		{
			get { return _mapInfoBLF; }
			private set { SetField(ref _mapInfoBLF, value); }
		}
		private MapInfoBLF _mapInfoBLF;

		#endregion

		public void LoadMapInfo(string mapInfoLocation)
		{
			//App.Storage.HomeWindowViewModel.AssemblyPage = null;
			var dialog = MetroBusyAlertBox.Show();

			var thread = new Thread(() =>
			{
				MapInfoLocation = mapInfoLocation;
				MapInfoBLF = new MapInfoBLF();

				// Validate the BLF file, add info to the Model
				PureBLF _blf = new PureBLF(MapInfoLocation);
				if (_blf.BLFChunks[1].ChunkMagic != "levl")
					throw new Exception("The selected BLF is not a valid Map Info file.");

				MapInfoBLF.ChunkCount = _blf.BLFChunks.Count.ToString();
				MapInfoBLF.Length = "0x" + _blf.BLFStream.Length.ToString("X8");
				MapInfoBLF.LevlLength = "0x" + _blf.BLFChunks[1].ChunkLength.ToString("X8");
				MapInfoBLF.LevlVersion = BitConverter.ToInt16(BitConverter.GetBytes(_blf.BLFChunks[1].ChunkFlags), 2).ToString();

				_blf.Close();

				// Open as a MapInfo File
				var fileInfo = new FileInfo(MapInfoLocation);
				MapInfoFile = new MapInfo(MapInfoLocation);

				switch (MapInfoFile.MapInformation.Game)
				{
					case MapInfo.GameIdentifier.Halo3:
						MapInfoBLF.Game = "Halo 3";
						break;
					case MapInfo.GameIdentifier.Halo3ODST:
						MapInfoBLF.Game = "Halo 3: ODST";
						break;
					case MapInfo.GameIdentifier.HaloReach:
						MapInfoBLF.Game = "Halo Reach";
						break;
					case MapInfo.GameIdentifier.HaloReachBetas:
						MapInfoBLF.Game = "Halo Reach Pre/Beta";
						break;
					case MapInfo.GameIdentifier.Halo4NetworkTest:
						MapInfoBLF.Game = "Halo 4 Network Test";
						break;
					case MapInfo.GameIdentifier.Halo4:
						MapInfoBLF.Game = "Halo 4";
						break;

					default:
						MapInfoBLF.Game = "Unknown";
						break;
				}

				Application.Current.Dispatcher.Invoke(delegate
				{
					App.Storage.HomeWindowViewModel.UpdateStatus(
					   String.Format("{0} ({1})", MapInfoFile.MapInformation.InternalName, fileInfo.Name));

					dialog.ViewModel.CanClose = true;
					dialog.Close();
					App.Storage.HomeWindowViewModel.HideDialog();
					//App.Storage.HomeWindowViewModel.AssemblyPage = MapInfoPage;
				});
			});
			thread.Start();
		}
	}
}
