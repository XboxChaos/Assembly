using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Blamite.Blam;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
	/// <summary>
	///     Interaction logic for DebugTools.xaml
	/// </summary>
	public partial class DebugTools
	{
		private readonly ICacheFile _cache;

		public DebugTools(ICacheFile cache)
		{
			InitializeComponent();

			_cache = cache;
		}

		#region Converter

		private void btnConvert_Click(object sender = null, RoutedEventArgs e = null)
		{
			bool convertingToOffset = string.IsNullOrEmpty(txtCoverterOffset.Text.Trim());

			// Parse
			long action;
			if (convertingToOffset)
			{
				action = txtConverterAddress.Text.ToLowerInvariant().StartsWith("0x")
					? long.Parse(txtConverterAddress.Text.Remove(0, 2), NumberStyles.HexNumber)
					: long.Parse(txtConverterAddress.Text);
			}
			else
			{
				action = txtCoverterOffset.Text.ToLowerInvariant().StartsWith("0x")
					? long.Parse(txtCoverterOffset.Text.Remove(0, 2), NumberStyles.HexNumber)
					: long.Parse(txtCoverterOffset.Text);
			}

			// Do calc
			if (convertingToOffset)
			{
				action = _cache.MetaArea.PointerToOffset(action);
			}
			else
			{
				action = _cache.MetaArea.OffsetToPointer((int) action);
			}

			// Write output

			if (convertingToOffset)
			{
				txtCoverterOffset.Text = "0x" + action.ToString("X");
			}
			else
			{
				txtConverterAddress.Text = "0x" + action.ToString("X");
			}
		}

		private void txtCoverterOffset_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter && e.Key != Key.Return) return;

			txtConverterAddress.Text = "";
			btnConvert_Click();
		}

		private void txtConverterAddress_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter && e.Key != Key.Return) return;

			txtCoverterOffset.Text = "";
			btnConvert_Click();
		}

		#endregion
	}
}