using System;
using System.Linq;

namespace Blamite.PostGeneration
{
	public class BlamitePostGenerator
	{
		public ViewModel ViewModel { get; private set; }

		/// <summary>
		/// Initalizes the Blamite Post Generator
		/// </summary>
		/// <param name="viewModel">The DataContext of the data used to generate the post.</param>
		public BlamitePostGenerator(ViewModel viewModel)
		{
			ViewModel = viewModel;
		}

		public string Parse()
		{
			var output = string.Empty;

			#region Header
			// Title
			output += BBCodeCentre(
				BBCodeBold(
					BBCodeColour(
						BBCodeSize(string.Format(".:: {0} ::.", ViewModel.ModName), 5)
						, "FF0000")), true);

			// Author
			output += BBCodeCentre(
				BBCodeSize(string.Format("By {0}", ViewModel.ModAuthor), 4), true);

			// Preview Image
			output += BBCodeCentre(
				BBCodeImg(ViewModel.ModPreviewImage), true);

			// Description
			output += BBCodeIndent(ViewModel.ModDescription, 1, true);
			#endregion

			output += "\n";

			#region Information
			// Title
			output += BBCodeBold(
				BBCodeSize(
					BBCodeColour("Mod Information:", "C0C0C0"), 4), true);

			// Mod List
			var modInfoList = "";
			modInfoList += BBCodeListOption(BBCodeBold("Mod Name: ") + ViewModel.ModName, true);
			modInfoList += BBCodeListOption(BBCodeBold("Original Map Name: ") + ViewModel.ModOriginalMap, true);
			modInfoList += BBCodeListOption(BBCodeBold("Creator: ") + ViewModel.ModAuthor, true);
			modInfoList += BBCodeListOption(BBCodeBold("Patch Download: ") + BBCodeUrl("Download", ViewModel.ModPatchDownload), true);

			output += BBCodeList(modInfoList, true);
			#endregion

			output += "\n";

			#region Attributes
			// Title
			output += BBCodeBold(
				BBCodeSize(
					BBCodeColour("Mod Attributes:", "C0C0C0"), 4), true);

			// Attributes
			var attributeList = "";
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.WeaponEdits ? "Yes" : "No") + " ] Weapon Edits", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.WeaponBalances ? "Yes" : "No") + " ] Weapon Balances", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.WeatherEdits ? "Yes" : "No") + " ] Weather Edits", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.TextureEdits ? "Yes" : "No") + " ] Texture Edits", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.ProjectileEdits ? "Yes" : "No") + " ] Projectile Edits", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.MachineEdits ? "Yes" : "No") + " ] Machine Edits", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.SceneryEdits ? "Yes" : "No") + " ] Scenery Edits", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.BipdEdits ? "Yes" : "No") + " ] Bipd Edits", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.VehicleEdits ? "Yes" : "No") + " ] Vehicle Edits", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.LighingEdits ? "Yes" : "No") + " ] Lighing Edits", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.JmadEdits ? "Yes" : "No") + " ] Jmad Edits", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.EffectEdits ? "Yes" : "No") + " ] Effect Edits", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.ModelEdits ? "Yes" : "No") + " ] Model Edits", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.PhysicsEdits ? "Yes" : "No") + " ] Physics Edits", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.BarrierEdits ? "Yes" : "No") + " ] Barrier Edits", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.AiEdits ? "Yes" : "No") + " ] Ai Edits", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.BlfMapinfoEdits ? "Yes" : "No") + " ] Custom Blf/Mapinfos", true);
			attributeList += BBCodeListOption("[ " + BBCodeBold(ViewModel.OtherEdits ? "Yes" : "No") + " ] Other Edits", true);

			output += BBCodeList(attributeList, true);
			#endregion

			output += "\n";

			#region Video
			if (!String.IsNullOrEmpty(ViewModel.ModVideo.Trim()))
			{
				// Title
				output += BBCodeBold(
					BBCodeSize(
						BBCodeColour("Video:", "C0C0C0"), 4), true);

				// Video
				output += BBCodeMedia(ViewModel.ModVideo, true);
			}
			#endregion

			output += "\n";

			#region Images
			if (ViewModel.Images.Count > 0)
			{
				// Title
				output += BBCodeBold(
					BBCodeSize(
						BBCodeColour("Images:", "C0C0C0"), 4), true);

				// Images
				var imageList = "";
				foreach (var image in ViewModel.Images)
					imageList += BBCodeImg(image.Url, true);

				output += BBCodeList(imageList, true);
			}
			#endregion

			output += "\n";

			#region Credits
			if (ViewModel.Thanks.Count > 0)
			{
				// Title
				output += BBCodeBold(
					BBCodeSize(
						BBCodeColour("Thanks:", "C0C0C0"), 4), true);

				// Thanks
				var thanksList = "";
				foreach (var thank in ViewModel.Thanks)
					thanksList += BBCodeListOption(BBCodeBold(thank.Alias) + ": " + thank.Reason, true);

				output += BBCodeList(thanksList, true);
			}
			#endregion

			output += "\n";

			#region Footer
			output += BBCodeSize(
				BBCodeColour("This post was generated by the Blamite Post Generator, which is part of " + 
					BBCodeUrl("Assembly", "https://sourceforge.net/projects/assembly/") + " by " + 
					BBCodeUrl("XboxChaos", "http://www.xboxchaos.com/topic/3263-assembly-blam-research-tool/"), 
				"#008000"), 2, true);
			#endregion

			return output;
		}

		// Alignment
		private string BBCodeCentre(string innerContent, bool trailingNewLine = false)
		{
			return string.Format("[center]{0}[/center]{1}", innerContent, trailingNewLine ? "\n" : "");
		}
		private string BBCodeIndent(string innerContent, int indentCount = 1, bool trailingNewLine = false)
		{
			return string.Format("[indent={0}]{1}[/indent]{2}", indentCount, innerContent, trailingNewLine ? "\n" : "");
		}

		// Basic Text Modification
		private string BBCodeSize(string innerContent, int size = 3, bool trailingNewLine = false)
		{
			return string.Format("[size={0}]{1}[/size]{2}", size, innerContent, trailingNewLine ? "\n" : "");
		}
		private string BBCodeColour(string innerContent, string colour = "000000", bool trailingNewLine = false)
		{
			return string.Format("[color=#{0}]{1}[/color]{2}", colour, innerContent, trailingNewLine ? "\n" : "");
		}
		private string BBCodeBold(string innerContent, bool trailingNewLine = false)
		{
			return string.Format("[b]{0}[/b]{1}", innerContent, trailingNewLine ? "\n" : "");
		}
		private string BBCodeItalic(string innerContent, bool trailingNewLine = false)
		{
			return string.Format("[i]{0}[/i]{1}", innerContent, trailingNewLine ? "\n" : "");
		}
		private string BBCodeUnderline(string innerContent, bool trailingNewLine = false)
		{
			return string.Format("[u]{0}[/u]{1}", innerContent, trailingNewLine ? "\n" : "");
		}
		private string BBCodeStrikethrough(string innerContent, bool trailingNewLine = false)
		{
			return string.Format("[s]{0}[/s]{1}", innerContent, trailingNewLine ? "\n" : "");
		}

		// Basic BBCode Hyperlinks
		private string BBCodeUrl(string innerContent, string url, bool trailingNewLine = false)
		{
			return string.Format("[url={0}]{1}[/url]{2}", url, innerContent, trailingNewLine ? "\n" : "");
		}
		private string BBCodeImg(string url, bool trailingNewLine = false)
		{
			return string.Format("[img]{0}[/img]{1}", url, trailingNewLine ? "\n" : "");
		}

		// List BBCode
		private string BBCodeList(string innerContent, bool trailingNewLine = false)
		{
			return string.Format("[list]{0}[/list]{1}", innerContent, trailingNewLine ? "\n" : "");
		}
		private string BBCodeListOption(string innerContent, bool trailingNewLine = false)
		{
			return string.Format("[*]{0}{1}", innerContent, trailingNewLine ? "\n" : "");
		}

		// Media BBCode
		private string BBCodeMedia(string innerContent, bool trailingNewLine = false)
		{
			return string.Format("[media]{0}[/media]{1}", innerContent, trailingNewLine ? "\n" : "");
		}
	}
}
