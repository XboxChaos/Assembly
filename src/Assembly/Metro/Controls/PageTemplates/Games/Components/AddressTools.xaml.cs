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

		private void btnConvert_Click(object sender = null, RoutedEventArgs e = null)
		{
			// Parse
			long action = txtCoverterInput.Text.ToLowerInvariant().StartsWith("0x")
					? long.Parse(txtCoverterInput.Text.Remove(0, 2), NumberStyles.HexNumber)
					: long.Parse(txtCoverterInput.Text);

			switch(cbInputType.SelectedIndex)
			{
				case 0://file
					{
						long addr = _cache.MetaArea.OffsetToPointer((int)action);
						switch (cbOutputType.SelectedIndex)
						{
							case 0://file
								break;
							case 1://contracted
								action = _cache.PointerExpander.Contract(addr);
								break;
							case 2://address
								action = addr;
								break;
						}
						break;
					}
				case 1://contracted
					{
						long addr = _cache.PointerExpander.Expand((uint)action);
						switch (cbOutputType.SelectedIndex)
						{
							case 0://file
								action = _cache.MetaArea.PointerToOffset(addr);
								break;
							case 1://contracted
								break;
							case 2://address
								action = addr;
								break;
						}
						break;
					}
				case 2://address
					{
						switch (cbOutputType.SelectedIndex)
						{
							case 0://file
								action = _cache.MetaArea.PointerToOffset(action);
								break;
							case 1://contracted
								action = _cache.PointerExpander.Contract(action);
								break;
							case 2://address
								break;
						}
						break;
					}
			}

			// Write output
			txtConverterOutput.Text = "0x" + action.ToString("X");
		}

		private void txtCoverterInput_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter && e.Key != Key.Return) return;

			txtConverterOutput.Text = "";
			btnConvert_Click();
		}
	}
}