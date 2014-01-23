using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Atlas.Dialogs;
using Atlas.Models;
using Atlas.Pages;
using Blamite.IO;

namespace Atlas.ViewModels
{
	public class HomeViewModel : Base
	{
		public HomeViewModel()
		{
			_statusResetTimer.Tick += (sender, args) => { Status = "Ready..."; };
		}

		#region Properties

		#region Application Stuff

		public string Status
		{
			get { return _status; }
			set
			{
				if (!value.EndsWith("..."))
					value += "...";

				_statusResetTimer.Stop();
				SetField(ref _status, value);
				_statusResetTimer.Start();
			}
		}
		private string _status = "Ready...";
		private readonly DispatcherTimer _statusResetTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 5) };

		public string ApplicationTitle
		{
			get { return _applicationTitle; }
			set { SetField(ref _applicationTitle, value); }
		}
		private string _applicationTitle = "Welcome";

		public Thickness ApplicationBorderThickness
		{
			get { return _applicationBorderThickness; }
			set { SetField(ref _applicationBorderThickness, value); }
		}
		private Thickness _applicationBorderThickness;

		#endregion

		#region Window Masks

		public Visibility MaskVisibility
		{
			get { return _maskVisibility; }
			set { SetField(ref _maskVisibility, value); }
		}
		private Visibility _maskVisibility = Visibility.Collapsed;

		#endregion

		#region Window Actions/Resizing

		public Visibility ActionRestoreVisibility
		{
			get { return _actionRestoreVisibility; }
			set { SetField(ref _actionRestoreVisibility, value); }
		}
		private Visibility _actionRestoreVisibility = Visibility.Collapsed;

		public Visibility ActionMaximizeVisibility
		{
			get { return _actionMaximizeVisibility; }
			set { SetField(ref _actionMaximizeVisibility, value); }
		}
		private Visibility _actionMaximizeVisibility = Visibility.Collapsed;

		public Visibility ResizingVisibility
		{
			get { return _resizingVisibility; }
			set { SetField(ref _resizingVisibility, value); }
		}
		private Visibility _resizingVisibility = Visibility.Collapsed;

		#endregion

		#region Content

		public IAssemblyPage AssemblyPage
		{
			get { return _assemblyPage; }
			set
			{
				// try closing current page
				if (_assemblyPage != null)
					if (!_assemblyPage.Close()) return;

				// aite, we can
				SetField(ref _assemblyPage, value);
			}
		}
		private IAssemblyPage _assemblyPage;
		#endregion

		#endregion

		#region Events

		#region Overrides
		public void OnStateChanged(object sender, EventArgs eventArgs)
		{
			switch ((WindowState)sender)
			{
				case WindowState.Normal:
					ApplicationBorderThickness = new Thickness(1, 1, 1, 25);
					ActionRestoreVisibility = Visibility.Collapsed;
					ActionMaximizeVisibility = 
						ResizingVisibility = Visibility.Visible;
					break;
				case WindowState.Maximized:
					ApplicationBorderThickness = new Thickness(0, 0, 0, 25);
					ActionRestoreVisibility = Visibility.Visible;
					ActionMaximizeVisibility =
						ResizingVisibility = Visibility.Collapsed;
					break;
			}
		}
		#endregion

		#endregion

		#region Dialog Management

		private int _maskCount;

		public void ShowDialog(bool showMask = true)
		{
			if (showMask)
				_maskCount++;

			if (_maskCount > 0)
				MaskVisibility = Visibility.Visible;
		}

		public void HideDialog(bool maskShown = true)
		{
			if (maskShown)
				_maskCount--;

			if (_maskCount == 0)
				MaskVisibility = Visibility.Collapsed;
		}
		
		#endregion

		#region Helpers

		public void UpdateStatus(string status)
		{
			ApplicationTitle = App.Storage.HomeWindow.Title = String.Format("{0} - Assembly", status);
		}

		public enum Type
		{
			BlamCache,
			MapInfo,
			MapImage,
			Campaign,
			Patch,

			Other
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		public void ValidateFile(Type type)
		{
			var openFileDialog = new OpenFileDialog
			{
				Filter = "Blam Cache Files (*.map)|*.map|" +
						"Blam Map Info (*.mapinfo)|*.mapinfo|" +
						"Blam Map Image (*.blf)|*.blf|" +
						"Blam Campaign (*.campaign)|*.campaign|" +
						"Assembly Patch (*.asmp)|*.asmp|" +
						"All Files (*.*)|*.*",
				FilterIndex = (int) type
			};

			if (openFileDialog.ShowDialog() != DialogResult.OK)
				return;

			switch (type)
			{
				case Type.BlamCache:
					OpenFile(openFileDialog.FileName, type);
					return;

				case Type.MapInfo:
				case Type.MapImage:
				case Type.Campaign:
				case Type.Patch:
					throw new NotImplementedException();
			}

			// try via file extension
			var fileInfo = new FileInfo(openFileDialog.FileName);
			switch (fileInfo.Extension.Remove(0, 1))
			{
				case "map":
					OpenFile(openFileDialog.FileName, Type.BlamCache);
					break;

				case "mapinfo":
				case "blf":
				case "campaign":
				case "asmp":
					throw new NotImplementedException();
			}

			// ugh, try via magic
			string magic;
			using (var reader = 
				new EndianReader(new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read),
					Endian.BigEndian))
				magic = reader.ReadAscii(0x04);

			switch (magic.Trim())
			{
				case "head":
				case "daeh":
					OpenFile(openFileDialog.FileName, Type.BlamCache);
					break;

				case "asmp":
				case "blf":
					throw new NotImplementedException();
			}

			// just fuck this
			MetroMessageBox.Show("This type of file is not supported by Assembly.");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="type"></param>
		public void OpenFile(string filePath, Type type)
		{
			switch (type)
			{
				case Type.BlamCache:
					AssemblyPage = new CachePage(filePath);
					break;

				case Type.MapInfo:
				case Type.MapImage:
				case Type.Campaign:
				case Type.Patch:
				default:
					throw new NotImplementedException();
			}

			var existing = App.Storage.Settings.RecentFiles.FirstOrDefault(r => r.Type == type && r.FilePath == filePath);
			if (existing != null)
				App.Storage.Settings.RecentFiles.Remove(existing);
			App.Storage.Settings.RecentFiles.Insert(0, new RecentFile(filePath, type));
		}

		#endregion
	}
}
