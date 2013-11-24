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
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.WeaponEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Weapon Edits", true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.WeaponBalances ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Weapon Balances", true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.WeatherEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Weather Edits", true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.TextureEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Texture Edits", true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.ProjectileEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Projectile Edits", true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.MachineEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Machine Edits", true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.SceneryEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Scenery Edits", true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.BipdEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Bipd Edits", true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.VehicleEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Vehicle Edits", true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.LighingEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Lighing Edits", true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.JmadEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Jmad Edits", true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.EffectEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Effect Edits", true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.ModelEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Model Edits", true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.PhysicsEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Physics Edits", true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.BarrierEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Barrier Edits", true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.AiEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) + " ] Ai Edits",
					true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.BlfMapinfoEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Custom Blf/Mapinfos", true);
			attributeList +=
				BBCodeListOption(
					"[ " + BBCodeBold(PostInfo.OtherEdits ? BBCodeColour("Yes", "009900") : BBCodeColour("No", "FF0000")) +
					" ] Other Edits", true);

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