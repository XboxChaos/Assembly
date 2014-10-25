using System;

namespace Assembly.Helpers.PostGeneration
{
	public class BlamitePostGenerator
	{
		/// <summary>
		///     Initalizes the Blamite Post Generator
		/// </summary>
		/// <param name="viewModel">The DataContext of the data used to generate the post.</param>
		public BlamitePostGenerator(ModPostInfo viewModel)
		{
			PostInfo = viewModel;
		}

		public ModPostInfo PostInfo { get; private set; }

		public string Parse()
		{
			string output = string.Empty;

			#region Header

			// Title
			output += BBCodeCentre(
				BBCodeBold(
					BBCodeColour(
						BBCodeSize(string.Format(".:: {0} ::.", PostInfo.ModName), 5)
						, "FF0000")), true);

			// Author
			output += BBCodeCentre(
				BBCodeSize(string.Format("By {0}", PostInfo.ModAuthor), 4), true);

			// Preview Image
			output += BBCodeCentre(
				BBCodeImg(PostInfo.ModPreviewImage), true);

			// Description
			output += BBCodeIndent(PostInfo.ModDescription, 1, true);

			#endregion

			output += "\n";

			#region Information

			// Title
			output += BBCodeBold(
				BBCodeSize(
					BBCodeColour("Mod Information:", "C0C0C0"), 4), true);

			// Mod List
			string modInfoList = "";
			modInfoList += BBCodeListOption(BBCodeBold("Mod Name: ") + PostInfo.ModName, true);
			modInfoList += BBCodeListOption(BBCodeBold("Original Map Name: ") + PostInfo.ModOriginalMap, true);
			modInfoList += BBCodeListOption(BBCodeBold("Creator: ") + PostInfo.ModAuthor, true);
			modInfoList += BBCodeListOption(BBCodeBold("Patch Download: ") + BBCodeUrl("Download", PostInfo.ModPatchDownload),
				true);

			output += BBCodeList(modInfoList, true);

			#endregion

			output += "\n";

			#region Attributes

			// Title
			output += BBCodeBold(
				BBCodeSize(
					BBCodeColour("Mod Attributes:", "C0C0C0"), 4), true);

			// Attributes
			string attributeList = "";

			if (PostInfo.BSPEdits)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] BSP Edits", true);
			if (PostInfo.InjectedTags)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] Injected Tags", true);
			if (PostInfo.WeaponEdits)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] Weapon Edits", true);
			if (PostInfo.WeaponBalances)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] Weapon Balances", true);
			if (PostInfo.WeatherEdits)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] Weather Edits", true);
			if (PostInfo.TextureEdits)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] Texture Edits", true);
			if (PostInfo.ProjectileEdits)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] Projectile Edits", true);
			if (PostInfo.BipdEdits)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] Biped Edits", true);
			if (PostInfo.VehicleEdits)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] Vehicle Edits", true);
			if (PostInfo.LighingEdits)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] Lighing Edits", true);
			if (PostInfo.JmadEdits)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] Animation Edits", true);
			if (PostInfo.EffectEdits)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] Effect Edits", true);
			if (PostInfo.ModelEdits)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] Model Edits", true);
			if (PostInfo.PhysicsEdits)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] Physics Edits", true);
			if (PostInfo.BarrierEdits)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] Barrier Edits", true);
			if (PostInfo.AiEdits)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] AI Edits", true);
			if (PostInfo.BlfMapinfoEdits)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] Custom BLFs/Mapinfo", true);
			if (PostInfo.OtherEdits)
				attributeList +=
					BBCodeListOption("[ " + BBCodeBold(BBCodeColour("Yes", "009900")) + " ] Other Edits", true);

			output += BBCodeList(attributeList, true);

			#endregion

			output += "\n";

			#region Video

			if (!String.IsNullOrEmpty(PostInfo.ModVideo.Trim()))
			{
				// Title
				output += BBCodeBold(
					BBCodeSize(
						BBCodeColour("Video:", "C0C0C0"), 4), true);

				// Video
				output += BBCodeMedia(PostInfo.ModVideo, true);
			}

			#endregion

			output += "\n";

			#region Images

			if (PostInfo.Images.Count > 0)
			{
				// Title
				output += BBCodeBold(
					BBCodeSize(
						BBCodeColour("Images:", "C0C0C0"), 4), true);

				// Images
				string imageList = "";
				foreach (ModPostInfo.Image image in PostInfo.Images)
					imageList += BBCodeImg(image.Url, true);

				output += BBCodeList(imageList, true);
			}

			#endregion

			output += "\n";

			#region Credits

			if (PostInfo.Thanks.Count > 0)
			{
				// Title
				output += BBCodeBold(
					BBCodeSize(
						BBCodeColour("Thanks:", "C0C0C0"), 4), true);

				// Thanks
				string thanksList = "";
				foreach (ModPostInfo.Thank thank in PostInfo.Thanks)
					thanksList += BBCodeListOption(BBCodeBold(thank.Alias) + ": " + thank.Reason, true);

				output += BBCodeList(thanksList, true);
			}

			#endregion

			output += "\n";

			#region Footer

			output += BBCodeSize(
				BBCodeColour("This post was generated by the Blamite Post Generator, which is part of " +
							 BBCodeUrl("Assembly", "https://github.com/xboxchaos/assembly") + " by " +
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