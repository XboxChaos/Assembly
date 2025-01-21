using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Blamite.Blam;
using Blamite.RTE;
using Blamite.RTE.ThirdGen;
using Blamite.RTE.SecondGen;
using Blamite.RTE.FirstGen;
using Blamite.Serialization;
using Assembly.Metro.Dialogs;
using Blamite.IO;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
	/// <summary>
	///     Interaction logic for AddressTools.xaml
	/// </summary>
	public partial class AddressTools
	{
		private readonly ICacheFile _cache;
		private IRTEProvider realtime;

		public AddressTools(ICacheFile cache, EngineDescription buildInfo)
		{
			InitializeComponent();

			_cache = cache;

			if (buildInfo.PokingPlatform == RTEConnectionType.LocalProcess32 ||
				buildInfo.PokingPlatform == RTEConnectionType.LocalProcess64 &&
				buildInfo.Engine != EngineType.Eldorado)
			{
				switch (_cache.Engine)
				{
					case EngineType.ThirdGeneration when !string.IsNullOrEmpty(buildInfo.PokingModule):
						realtime = new ThirdGenMCCRTEProvider(buildInfo);
						break;
					case EngineType.SecondGeneration when !string.IsNullOrEmpty(buildInfo.PokingModule):
						realtime = new SecondGenMCCRTEProvider(buildInfo);
						break;
					case EngineType.SecondGeneration:
						realtime = new SecondGenRTEProvider(buildInfo);
						break;
					case EngineType.FirstGeneration when !string.IsNullOrEmpty(buildInfo.PokingModule):
						realtime = new FirstGenMCCRTEProvider(buildInfo);
						break;
					case EngineType.FirstGeneration:
						realtime = new FirstGenRTEProvider(buildInfo);
						break;
				}
			}
			else
			{
				inputRuntime.IsEnabled = false;
				outputRuntime.IsEnabled = false;
			}
		}

		private void btnConvert_Click(object sender = null, RoutedEventArgs e = null)
		{
			// Parse
			long action;
			bool parsed = txtCoverterInput.Text.ToLowerInvariant().StartsWith("0x")
				? long.TryParse(txtCoverterInput.Text.Remove(0, 2), NumberStyles.HexNumber, null, out action)
				: long.TryParse(txtCoverterInput.Text, out action);
			if (!parsed)
			{
				MetroMessageBox.Show("Cannot parse input value. Hex should be prefixed with \"0x\".");
				return;
			}

			var rteStream = realtime?.GetMetaStream(_cache, null);
			long rteMagic = 0;
			if (cbInputType.SelectedIndex == 3 || cbOutputType.SelectedIndex == 3)
			{
				if (rteStream == null)
				{
					MetroMessageBox.Show("Cannot convert using a runtime address. Possibly because:\r\n\r\n" +
										"-The game is not running.\r\n" +
										"-The game is not running the same map as the one you have open.\r\n" +
										"-Or this engine doesn't use/support poking/runtime addresses.");
					return;
				}

				rteMagic = ((OffsetStream)rteStream.BaseStream).Offset;
			}

			switch(cbInputType.SelectedIndex)
			{
				case 0://file
					{
						long addr = _cache.MetaArea.OffsetToPointer((uint)action);
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
							case 3://realtime
								action = addr + rteMagic;
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
							case 3://realtime
								action = addr + rteMagic;
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
							case 3://realtime
								action += rteMagic;
								break;
						}
						break;
					}
				case 3://runtime address
					{
						long addr = action - rteMagic;
						switch (cbOutputType.SelectedIndex)
						{
							case 0://file
								action = _cache.MetaArea.PointerToOffset(addr);
								break;
							case 1://contracted
								action = _cache.PointerExpander.Contract(addr);
								break;
							case 2://address
								action = addr;
								break;
							case 3://realtime
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