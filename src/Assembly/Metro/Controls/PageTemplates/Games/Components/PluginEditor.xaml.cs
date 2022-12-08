using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Assembly.Helpers;
using Assembly.Helpers.CodeCompletion.XML;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using Assembly.SyntaxHighlighting;
using Blamite.Serialization;
using Blamite.Util;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
	/// <summary>
	///     Interaction logic for PluginEditor.xaml
	/// </summary>
	public partial class PluginEditor
	{
		private readonly XMLCodeCompleter _completer = new XMLCodeCompleter();
		private readonly MetaContainer _parent;
		private readonly string _pluginPath;
		private readonly string _fallbackPluginPath;
		private readonly MetaEditor _sibling;
		private CompletionWindow _completionWindow;

		public PluginEditor(EngineDescription buildInfo, TagEntry tag, MetaContainer parent, MetaEditor sibling)
		{
			InitializeComponent();

			txtPlugin.TextArea.TextEntered += PluginTextEntered;

			_parent = parent;
			_sibling = sibling;

			LoadSyntaxHighlighting();
			SetHighlightColor();
			LoadCodeCompletion();

			App.AssemblyStorage.AssemblySettings.PropertyChanged += Settings_SettingsChanged;

			string groupName = VariousFunctions.SterilizeTagGroupName(CharConstant.ToString(tag.RawTag.Group.Magic)).Trim();
			_pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins",
					buildInfo.Settings.GetSetting<string>("plugins"), groupName.Trim());

			if (buildInfo.Settings.PathExists("fallbackPlugins"))
				_fallbackPluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins",
					buildInfo.Settings.GetSetting<string>("fallbackPlugins"), groupName.Trim());
			LoadPlugin();
		}

		public void GoToLine(int line)
		{
			UpdateLayout();

			if (line < txtPlugin.Document.LineCount)
			{
				DocumentLine selectedLineDetails = txtPlugin.Document.GetLineByNumber(line);
				txtPlugin.ScrollToLine(line);
				txtPlugin.Select(selectedLineDetails.Offset, selectedLineDetails.Length);
			}
			else
				txtPlugin.ScrollToEnd();
		}

		private void Settings_SettingsChanged(object sender, EventArgs e)
		{
			// Reload the syntax highlighting definition in case the theme changed
			LoadSyntaxHighlighting();

			// Reset the highlight color in case the theme changed
			SetHighlightColor();
		}

		private void btnPluginSave_Click(object sender, RoutedEventArgs e)
		{
			File.WriteAllText(_pluginPath, txtPlugin.Text);
			_sibling.RefreshEditor(MetaReader.LoadType.File);
			_parent.ShowMetaEditor();
		}

		private void btnLoadFromDisk_Click_1(object sender, RoutedEventArgs e)
		{
			LoadPlugin();
		}

		private void txtPlugin_MouseRightButtonDown_1(object sender, MouseButtonEventArgs e)
		{
			// Move the cursor to the place where the click occurred (AvalonEdit doesn't do this by default)
			// http://community.sharpdevelop.net/forums/p/12521/34105.aspx
			TextViewPosition? position = txtPlugin.GetPositionFromPoint(e.GetPosition(txtPlugin));
			if (position.HasValue)
				txtPlugin.TextArea.Caret.Position = position.Value;
		}

		private void LoadSyntaxHighlighting()
		{
			// Load the file depending upon which theme is being used
			string filename = "XMLBlue.xshd";
			switch (App.AssemblyStorage.AssemblySettings.ApplicationAccent)
			{
				case Settings.Accents.Blue:
					filename = "XMLBlue.xshd";
					break;
				case Settings.Accents.Green:
					filename = "XMLGreen.xshd";
					break;
				case Settings.Accents.Orange:
					filename = "XMLOrange.xshd";
					break;
				case Settings.Accents.Purple:
					filename = "XMLPurple.xshd";
					break;
			}
			txtPlugin.SyntaxHighlighting = HighlightLoader.LoadEmbeddedDefinition(filename);
		}

		private void SetHighlightColor()
		{
			var bconv = new System.Windows.Media.BrushConverter();
			var selbrsh = (System.Windows.Media.Brush)bconv.ConvertFromString("#1D98EB");

			//yucky
			switch (App.AssemblyStorage.AssemblySettings.ApplicationAccent)
			{
				case Settings.Accents.Blue:
					selbrsh = (System.Windows.Media.Brush)bconv.ConvertFromString("#1D98EB");
					break;
				case Settings.Accents.Green:
					selbrsh = (System.Windows.Media.Brush)bconv.ConvertFromString("#98e062");
					break;
				case Settings.Accents.Orange:
					selbrsh = (System.Windows.Media.Brush)bconv.ConvertFromString("#D66F2B");
					break;
				case Settings.Accents.Purple:
					selbrsh = (System.Windows.Media.Brush)bconv.ConvertFromString("#9C40B4");
					break;
			}

			txtPlugin.TextArea.SelectionBorder = new System.Windows.Media.Pen(selbrsh, 1);
			selbrsh.Opacity = 0.3;
			txtPlugin.TextArea.SelectionBrush = selbrsh;
		}

		private void LoadPlugin()
		{
			string bestPluginPath;

			if (string.IsNullOrEmpty(_pluginPath) || !File.Exists(_pluginPath))
			{
				if (string.IsNullOrEmpty(_fallbackPluginPath) || !File.Exists(_fallbackPluginPath))
					return;

				bestPluginPath = _fallbackPluginPath;
			}
			else
				bestPluginPath = _pluginPath;

			txtPlugin.Text = File.ReadAllText(bestPluginPath);
		}

		private void LoadCodeCompletion()
		{
			RegisterMetaTag("uint8", "Unsigned 8-bit integer");
			RegisterMetaTag("int8", "Signed 8-bit integer");
			RegisterMetaTag("uint16", "Unsigned 16-bit integer");
			RegisterMetaTag("int16", "Signed 16-bit integer");
			RegisterMetaTag("uint32", "Unsigned 32-bit integer");
			RegisterMetaTag("int32", "Signed 32-bit integer");
			RegisterMetaTag("float32", "32-bit floating-point value");
			RegisterMetaTag("stringId", "32-bit string ID");
			RegisterMetaTag("flags8", "8-bit flags value");
			RegisterMetaTag("flags16", "16-bit flags value");
			RegisterMetaTag("flags32", "32-bit flags value");
			RegisterMetaTag("flags64", "64-bit flags value");
			RegisterMetaTag("enum8", "8-bit enumeration value");
			RegisterMetaTag("enum16", "8-bit enumeration value");
			RegisterMetaTag("enum32", "8-bit enumeration value");
			RegisterMetaTag("range16", "Range of two 16-bit values");
			RegisterMetaTag("rangeF", "Range of two 32-bit floating point values");
			RegisterMetaTag("rangeD", "Range of two radian values converted to/from degrees");

			RegisterMetaTag("point2", "2D point of 32-bit floating point values (x,y)");
			RegisterMetaTag("point3", "3D point of 32-bit floating point values (x,y,z)");

			RegisterMetaTag("vector2", "2D vector of 32-bit floating point values (i,j)");
			RegisterMetaTag("vector3", "3D vector of 32-bit floating point values (i,j,k)");
			RegisterMetaTag("vector4", "Quaternion of 32-bit floating point values (i,j,k,w)");

			RegisterMetaTag("degree", "Radian value that should be converted to/from degrees");
			RegisterMetaTag("degree2", "2D radian angles that should be converted to/from degrees (y,p)");
			RegisterMetaTag("degree3", "3D radian angles that should be converted to/from degrees (y,p,r)");

			RegisterMetaTag("plane2", "2D plane of 3 32-bit floating point values (i,j,d)");
			RegisterMetaTag("plane3", "3D plane of 4 of 32-bit floating point values (i,j,k,d)");
			
			RegisterMetaTag("rect16", "4 16-bit values representing the sides of a rectangle (top, left, bottom, right)");

			RegisterMetaTag("datum", "32-bit datum value");

			RegisterMetaTag("oldstringid", "Early string ID, x1C length string ending in a 32-bit string ID. 32 bytes long");

			CompletableXMLTag color = RegisterMetaTag("color32", "Integer color value");
			CompletableXMLTag colorf = RegisterMetaTag("colorf", "Floating-point color value");
			var colorAlpha = new CompletableXMLAttribute("alpha",
				"Whether or not the color includes the alpha channel (required)");
			colorAlpha.RegisterValue(new CompletableXMLValue("true", "The color includes an alpha channel"));
			colorAlpha.RegisterValue(new CompletableXMLValue("false", "The color does not include an alpha channel"));
			color.RegisterAttribute(colorAlpha);
			colorf.RegisterAttribute(colorAlpha);

			var colorBasic = new CompletableXMLAttribute("basic",
				"Whether or not the color should be limited to 32bits (not required, default depends on engine)");
			colorBasic.RegisterValue(new CompletableXMLValue("true", "The color is limited to 32bits (default for engines prior to thirdgen)"));
			colorBasic.RegisterValue(new CompletableXMLValue("false", "The color is scRGB (default for thirdgen engines)"));
			colorf.RegisterAttribute(colorBasic);

			CompletableXMLTag tagRef = RegisterMetaTag("tagRef", "Tag reference");
			var withGroup = new CompletableXMLAttribute("withGroup",
				"Whether or not the reference includes a group ID (optional, default=true)");
			withGroup.RegisterValue(new CompletableXMLValue("true", "The reference includes a group ID (default)"));
			withGroup.RegisterValue(new CompletableXMLValue("false", "The reference only includes a 4-byte datum index"));
			tagRef.RegisterAttribute(withGroup);

			CompletableXMLTag dataRef = RegisterMetaTag("dataRef", "Data reference");
			var format = new CompletableXMLAttribute("format", "The format of the data in the dataref (optional, default=bytes)");
			format.RegisterValue(new CompletableXMLValue("bytes", "Raw byte data (default)"));
			format.RegisterValue(new CompletableXMLValue("asciiz", "Null-terminated ASCII string"));
			format.RegisterValue(new CompletableXMLValue("utf16", "Null-terminated UTF-16 string"));
			dataRef.RegisterAttribute(format);

			CompletableXMLTag tagblock = RegisterMetaTag("tagblock", "Tagblock pointer");
			tagblock.RegisterAttribute(new CompletableXMLAttribute("elementSize",
				"The size of each element in the tag block (required)"));

			CompletableXMLTag ascii = RegisterMetaTag("ascii", "ASCII string");
			CompletableXMLTag utf16 = RegisterMetaTag("utf16", "UTF-16 string");
			CompletableXMLTag hexstring = RegisterMetaTag("hexstring", "Raw byte data as a string, intended for things like hashes and checksums vs \"raw\"");
			ascii.RegisterAttribute(new CompletableXMLAttribute("size",
				"The maximum size of the string data, including the null terminator. Should normally only be 0x20 or 0x100. TextBox max length = size (required)"));
			utf16.RegisterAttribute(new CompletableXMLAttribute("size",
				"The maximum size of the string data, including the null terminator. Should normally only be 0x40 or 0x100. TextBox max length = size / 2 (required)"));
			hexstring.RegisterAttribute(new CompletableXMLAttribute("size",
				"The maximum size of the byte array. TextBox max length = size * 2. (required)"));

			CompletableXMLTag raw = RegisterMetaTag("raw", "Raw data viewer");
			raw.RegisterAttribute(new CompletableXMLAttribute("size", "The size of the raw data (required)"));

			var comment = new CompletableXMLTag("comment", "Displays a message");
			comment.RegisterAttribute(new CompletableXMLAttribute("title", "The title of the comment (optional)"));
			_completer.RegisterTag(comment);

			CompletableXMLTag shader = RegisterMetaTag("shader", "Shader reference");
			var shaderType = new CompletableXMLAttribute("type", "The type of the shader (required)");
			shaderType.RegisterValue(new CompletableXMLValue("pixel", "Pixel shader"));
			shaderType.RegisterValue(new CompletableXMLValue("vertex", "Vertex shader"));
			shader.RegisterAttribute(shaderType);

			RegisterMetaTag("undefined", "Value of an unknown type");

			_completer.TagCompletionAvailable += TagCompletionAvailable;
			_completer.AttributeCompletionAvailable += AttributeCompletionAvailable;
			_completer.ValueCompletionAvailable += ValueCompletionAvailable;
		}

		private void ValueCompletionAvailable(object sender, ValueCompletionEventArgs e)
		{
			_completionWindow = new CompletionWindow(txtPlugin.TextArea);

			IList<ICompletionData> data = _completionWindow.CompletionList.CompletionData;
			foreach (CompletableXMLValue tag in e.Suggestions)
				data.Add(new XMLValueCompletionData(tag));

			_completionWindow.Show();
		}

		private void AttributeCompletionAvailable(object sender, AttributeCompletionEventArgs e)
		{
			_completionWindow = new CompletionWindow(txtPlugin.TextArea);

			IList<ICompletionData> data = _completionWindow.CompletionList.CompletionData;
			foreach (CompletableXMLAttribute tag in e.Suggestions)
				data.Add(new XMLAttributeCompletionData(tag, _completer));

			_completionWindow.Show();
		}

		private void TagCompletionAvailable(object sender, TagCompletionEventArgs e)
		{
			_completionWindow = new CompletionWindow(txtPlugin.TextArea);

			IList<ICompletionData> data = _completionWindow.CompletionList.CompletionData;
			foreach (CompletableXMLTag tag in e.Suggestions)
				data.Add(new XMLTagCompletionData(tag, _completer));

			_completionWindow.Show();
		}

		private CompletableXMLTag RegisterMetaTag(string name, string description)
		{
			var tag = new CompletableXMLTag(name, description);
			tag.RegisterAttribute(new CompletableXMLAttribute("name", "The field's name (required)"));
			tag.RegisterAttribute(new CompletableXMLAttribute("offset", "The field's offset (required)"));

			var visible = new CompletableXMLAttribute("visible", "Whether or not the field is always visible (required)");
			visible.RegisterValue(new CompletableXMLValue("true", "Field is always visible"));
			visible.RegisterValue(new CompletableXMLValue("false", "Field is only visible when invisibles are shown"));

			tag.RegisterAttribute(new CompletableXMLAttribute("tooltip", "The field's tooltip (optional)"));

			tag.RegisterAttribute(visible);
			_completer.RegisterTag(tag);
			return tag;
		}

		private void PluginTextEntered(object sender, TextCompositionEventArgs e)
		{
			if (e.Text == "<") // Tag completion
			{
				_completer.CompleteTag();
			}
			else if (e.Text == " " || e.Text == "\"")
			{
				// Get the current line
				DocumentLine currentLine = txtPlugin.Document.GetLineByNumber(txtPlugin.TextArea.Caret.Line);
				int lineOffset = currentLine.Offset;
				string lineText = txtPlugin.Document.GetText(lineOffset, currentLine.Length);

				if (e.Text == " ") // Attribute completion
					_completer.CompleteAttributeName(lineText, txtPlugin.TextArea.Caret.Offset - lineOffset);
				else // Value completion
					_completer.CompleteAttributeValue(lineText, txtPlugin.TextArea.Caret.Offset - lineOffset);
			}
		}

		public void Dispose()
		{
			App.AssemblyStorage.AssemblySettings.PropertyChanged -= Settings_SettingsChanged;
		}
	}
}