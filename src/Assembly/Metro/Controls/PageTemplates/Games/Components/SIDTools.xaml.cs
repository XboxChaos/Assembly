using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Blamite.Blam;
using Blamite.Util;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
	/// <summary>
	///     Interaction logic for SIDTools.xaml
	/// </summary>
	public partial class SIDTools
	{
		private readonly ICacheFile _cache;

		public SIDTools(ICacheFile cache)
		{
			InitializeComponent();

			_cache = cache;
		}

		private void btnConvertTo_Click(object sender = null, RoutedEventArgs e = null)
		{
			// Parse
			uint action;
			if(txtCoverterVal.Text.ToLowerInvariant().StartsWith("0x"))
				uint.TryParse(txtCoverterVal.Text.Remove(0, 2), NumberStyles.HexNumber, null, out action);
			else
				uint.TryParse(txtCoverterVal.Text, NumberStyles.HexNumber, null, out action);

			string str = _cache.StringIDs.GetString(new StringID(action));

			if (str == "")
				str = "NOT A STRINGID";

			txtConverterString.Text = str;
		}

		private void btnConvertFrom_Click(object sender = null, RoutedEventArgs e = null)
		{
			// Parse
			uint action;
			string str = txtConverterString.Text.ToLowerInvariant();
			StringID temp;
			temp = _cache.StringIDs.FindStringID(str);
			if (temp.Value != 0)
				action = temp.Value;
			else
				action = 0xFFFFFFFF;

			txtCoverterVal.Text = "0x" + action.ToString("X");
		}

		private void txtCoverterVal_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter && e.Key != Key.Return) return;

			txtConverterString.Text = "";
			btnConvertTo_Click();
		}

		private void txtConverterString_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter && e.Key != Key.Return) return;

			txtCoverterVal.Text = "";
			btnConvertFrom_Click();
		}
	}
}