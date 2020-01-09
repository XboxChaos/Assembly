using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Blamite.Blam;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
	/// <summary>
	///     Interaction logic for AddressTools.xaml
	/// </summary>
	public partial class AddressTools
	{
		private readonly ICacheFile _cache;

		public AddressTools(ICacheFile cache)
		{
			InitializeComponent();

			_cache = cache;
		}

		private void btnConvertTo_Click(object sender = null, RoutedEventArgs e = null)
		{
			// Parse
			long action = txtCoverterOffset.Text.ToLowerInvariant().StartsWith("0x")
					? long.Parse(txtCoverterOffset.Text.Remove(0, 2), NumberStyles.HexNumber)
					: long.Parse(txtCoverterOffset.Text);

			action = _cache.MetaArea.OffsetToPointer((int) action);

			// Write output
			txtConverterAddress.Text = "0x" + action.ToString("X");
		}

		private void btnConvertFrom_Click(object sender = null, RoutedEventArgs e = null)
		{
			// Parse
			long action = txtConverterAddress.Text.ToLowerInvariant().StartsWith("0x")
					? long.Parse(txtConverterAddress.Text.Remove(0, 2), NumberStyles.HexNumber)
					: long.Parse(txtConverterAddress.Text);

			// Do calc
			action = _cache.MetaArea.PointerToOffset(action);

			// Write output
			txtCoverterOffset.Text = "0x" + action.ToString("X");

		}

		private void txtCoverterOffset_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter && e.Key != Key.Return) return;

			txtConverterAddress.Text = "";
			btnConvertTo_Click();
		}

		private void txtConverterAddress_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter && e.Key != Key.Return) return;

			txtCoverterOffset.Text = "";
			btnConvertFrom_Click();
		}

	}
}