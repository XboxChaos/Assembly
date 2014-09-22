using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Assembly.Helpers.Native;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using Blamite.Blam;
using Blamite.Serialization;
using Blamite.IO;
using System;
using System.Threading;
using WMPLib;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	///     Interaction logic for ViewValueAs.xaml
	/// </summary>
	public partial class ViewValueAs
	{
		private readonly EngineDescription _buildInfo;
		private readonly ICacheFile _cacheFile;
		private readonly uint _cacheOffsetOriginal;
		private readonly IList<MetaField> _fields;
		private readonly IStreamManager _streamManager;
		private uint _cacheOffset;
		private MetaReader _reader;

		public ViewValueAs(ICacheFile cacheFile, EngineDescription buildInfo, IStreamManager streamManager,
			IList<MetaField> fields, uint cacheOffset)
		{
			InitializeComponent();

			DwmDropShadow.DropShadowToWindow(this);

			_buildInfo = buildInfo;
			_streamManager = streamManager;
			_cacheFile = cacheFile;
			_fields = fields;
			_cacheOffsetOriginal = _cacheOffset = cacheOffset;

			// Set Textbox
			txtOffset.Text = "0x" + _cacheOffset.ToString("X");

			// Load Meta
			panelMetaComponents.ItemsSource = _fields;
			RefreshMeta();
		}

		public void RefreshMeta()
		{
			_reader = new MetaReader(_streamManager, _cacheOffset, _cacheFile, _buildInfo, MetaReader.LoadType.File, null);
			_reader.ReadFields(_fields);
		}

		private void btnClose_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void ResizeDrop_DragDelta(object sender, DragDeltaEventArgs e)
		{
			double yadjust = Height + e.VerticalChange;
			double xadjust = Width + e.HorizontalChange;

			if (xadjust > MinWidth)
				Width = xadjust;
			if (yadjust > MinHeight)
				Height = yadjust;
		}

		private void btnRefresh_Click(object sender = null, RoutedEventArgs e = null)
		{
			int offset;

			// Validate Textbox
			bool success;
			if (txtOffset.Text.StartsWith("0x") || txtOffset.Text.StartsWith("0X"))
			{
				// Is Hex
				success = int.TryParse(txtOffset.Text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out offset);
			}
			else
			{
				// Not Hex
				success = int.TryParse(txtOffset.Text, out offset);
			}

			if (Egg((uint)offset))
				return;

			if (!success || offset < _cacheFile.MetaArea.Offset ||
			    offset >= _cacheFile.MetaArea.Offset + _cacheFile.MetaArea.Size)
			{
				ShowInvalidOffsetError();
			}

			_reader.BaseOffset = (uint) offset;
			RefreshMeta();
		}

		private void ShowInvalidOffsetError()
		{
			MetroMessageBox.Show(
					"Invalid offset.",
					"The meta offset you set is not valid. It might be beyond the boundaries of the file or contain invalid characters. Remember, if it's a hex number, it must start with a '0x'."
					);
		}

		private void txtOffset_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				btnRefresh_Click();
		}

		private void btnReset_Click(object sender, RoutedEventArgs e)
		{
			_cacheOffset = _cacheOffsetOriginal;
			txtOffset.Text = "0x" + _cacheOffset.ToString("X");
			RefreshMeta();
		}

		private void btnDown_Click(object sender, RoutedEventArgs e)
		{
			_cacheOffset -= 1;
			txtOffset.Text = "0x" + _cacheOffset.ToString("X");
			RefreshMeta();
		}

		private void btnUp_Click(object sender, RoutedEventArgs e)
		{
			_cacheOffset += 1;
			txtOffset.Text = "0x" + _cacheOffset.ToString("X");
			RefreshMeta();
		}

		#region Egg
		private static WindowsMediaPlayer _eggPlayer;
		private static uint[] _eggNumbers = new uint[] { 0x4A4F484E, 0x43454E41 };
		private static int _eggIndex = 0;

		private bool Egg(uint x)
		{
			if (_eggIndex >= _eggNumbers.Length)
				return false;
			if (x == _eggNumbers[_eggIndex])
			{
				_eggIndex++;
			}
			else
			{
				_eggIndex = 0;
				return false;
			}

			if (_eggIndex == _eggNumbers.Length)
			{
				var result = MetroMessageBox.Show("I have just one question for you.", "Are you ready?", MetroMessageBox.MessageBoxButtons.YesNo);
				if (result == MetroMessageBox.MessageBoxResult.Yes)
				{
					if (_eggPlayer == null)
						_eggPlayer = new WindowsMediaPlayer();
					_eggPlayer.URL = @"http://www.xboxchaos.com/assembly/kbdata/MyTimeIsNow.wma";
					_eggPlayer.MarkerHit += _eggPlayer_MarkerHit;
					_eggPlayer.controls.play();
				}
				else
				{
					_eggIndex = 0;
				}
				return true;
			}
			else
			{
				ShowInvalidOffsetError();
				return true;
			}
		}

		void _eggPlayer_MarkerHit(int MarkerNum)
		{
			myTimeIsNowImage.Visibility = Visibility.Visible;
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			if (_eggPlayer != null)
			{
				_eggPlayer.controls.stop();
				_eggPlayer.close();
				_eggIndex = 0;
			}
		}
		#endregion
	}
}