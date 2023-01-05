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
using System.IO;

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
		private long _memOffset;

		public ICacheFile ParentCache
		{
			get { return _cacheFile; }
		}

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

			_memOffset = _cacheFile.MetaArea.OffsetToPointer((uint)_cacheOffset);

			// Set Textboxes
			txtOffset.Text = "0x" + _cacheOffset.ToString("X");
			txtMemOffset.Text = "0x" + _memOffset.ToString("X");
			txtMapName.Text = Path.GetFileName(_cacheFile.FilePath);
			txtMapName.ToolTip = _cacheFile.FilePath;

			// Load Meta
			panelMetaComponents.ItemsSource = _fields;
			RefreshMeta();
		}

		public void RefreshMeta()
		{
			_reader = new MetaReader(_streamManager, _cacheOffset, _cacheFile, _buildInfo, MetaReader.LoadType.File, null);
			_reader.ReadFields(_fields);
		}

		public uint GetSkip()
		{
			switch (cbSkip.SelectedIndex)
			{
				case 0:
					return 1;
				case 1:
					return 2;
				case 2:
					return 4;
				default:
					return 0;
			}
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

		private void RefreshFileOffset()
		{
			uint offset;

			// Validate Textbox
			bool success;
			if (txtOffset.Text.StartsWith("0x") || txtOffset.Text.StartsWith("0X"))
			{
				// Is Hex
				success = uint.TryParse(txtOffset.Text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out offset);
			}
			else
			{
				// Not Hex
				success = uint.TryParse(txtOffset.Text, out offset);
			}

			if (!success || !_cacheFile.MetaArea.ContainsOffset((uint)offset))
			{
				MetroMessageBox.Show(
					"Invalid offset.",
					"The meta offset you set is not valid. It might be beyond the boundaries of the file or contain invalid characters. Remember, if it's a hex number, it must start with a '0x'."
					);
				return;
			}
			_cacheOffset = offset;

			//Update the other textbox
			_memOffset = _cacheFile.MetaArea.OffsetToPointer((uint)_cacheOffset);
			txtMemOffset.Text = "0x" + _memOffset.ToString("X");

			RefreshMeta();
		}

		private void RefreshMemAddr()
		{
			long offset;

			// Validate Textbox
			bool success;
			if (txtMemOffset.Text.StartsWith("0x") || txtMemOffset.Text.StartsWith("0X"))
			{
				// Is Hex
				success = long.TryParse(txtMemOffset.Text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out offset);
			}
			else
			{
				// Not Hex
				success = long.TryParse(txtMemOffset.Text, out offset);
			}

			if (!success || !_cacheFile.MetaArea.ContainsPointer(offset))
			{
				MetroMessageBox.Show(
					"Invalid address.",
					"The meta address you set is not valid. It might be beyond the boundaries of the file or contain invalid characters. Remember, if it's a hex number, it must start with a '0x'."
					);
				return;
			}
			_cacheOffset = (uint)_cacheFile.MetaArea.PointerToOffset(offset);

			//Update the other textbox
			txtOffset.Text = "0x" + _cacheOffset.ToString("X");

			RefreshMeta();
		}

		private void txtOffset_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				RefreshFileOffset();
		}

		private void txtMemOffset_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				RefreshMemAddr();
		}

		private void btnReset_Click(object sender, RoutedEventArgs e)
		{
			_cacheOffset = _cacheOffsetOriginal;
			_memOffset = _cacheFile.MetaArea.OffsetToPointer((uint)_cacheOffset);

			txtOffset.Text = "0x" + _cacheOffset.ToString("X");
			txtMemOffset.Text = "0x" + _memOffset.ToString("X");
			RefreshMeta();
		}

		private void btnDown_Click(object sender, RoutedEventArgs e)
		{
			_cacheOffset -= GetSkip();
			_memOffset = _cacheFile.MetaArea.OffsetToPointer((uint)_cacheOffset);

			txtOffset.Text = "0x" + _cacheOffset.ToString("X");
			txtMemOffset.Text = "0x" + _memOffset.ToString("X");
			RefreshMeta();
		}

		private void btnUp_Click(object sender, RoutedEventArgs e)
		{
			_cacheOffset += GetSkip();
			_memOffset = _cacheFile.MetaArea.OffsetToPointer((uint)_cacheOffset);

			txtOffset.Text = "0x" + _cacheOffset.ToString("X");
			txtMemOffset.Text = "0x" + _memOffset.ToString("X");
			RefreshMeta();
		}
	}
}