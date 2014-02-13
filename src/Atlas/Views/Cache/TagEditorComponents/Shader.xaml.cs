using System.Diagnostics;
using System.IO;
using Atlas.Views.Cache.TagEditorComponents.Data;
using Blamite.Blam.Shaders;

namespace Atlas.Views.Cache.TagEditorComponents
{
	/// <summary>
	///     Interaction logic for Shader.xaml
	/// </summary>
	public partial class Shader
	{
		public Shader()
		{
			InitializeComponent();
		}

		private void btnDisassemble_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			var shaderRef = (ShaderRef)DataContext;
			var shader = shaderRef.Shader;
			if (shader == null)
				return;

			var xsdPath = App.Storage.Settings.XsdPath;
			if (string.IsNullOrWhiteSpace(xsdPath) || !File.Exists(xsdPath))
			{
				// TODO: add dialogs
				//MetroMessageBox.Show("xsd.exe (from the XDK) is required in order to disassemble shaders.\r\nYou can set a path to it in Settings under Map Editor.");
				return;
			}

			var microcodePath = Path.GetTempFileName();
			try
			{
				// Write the microcode to a file so XSD can use it
				File.WriteAllBytes(microcodePath, shader.Microcode);

				// Start XSD.exe with one of the /raw switches (depending upon shader type)
				// and the microcode file
				var startInfo = new ProcessStartInfo(xsdPath);
				if (shaderRef.Type == ShaderType.Pixel)
					startInfo.Arguments = "/rawps";
				else
					startInfo.Arguments = "/rawvs";

				// Add the path to the microcode file
				startInfo.Arguments += " \"" + microcodePath + "\"";
				startInfo.CreateNoWindow = true;
				startInfo.RedirectStandardOutput = true;
				startInfo.UseShellExecute = false;

				// Run it and capture the output
				var process = Process.Start(startInfo);
				var output = process.StandardOutput.ReadToEnd();
				process.WaitForExit();

				// Display it
				// TODO: Add dialogs
				//MetroMessageBoxCode.Show("Shader Disassembly", output);
			}
			finally
			{
				File.Delete(microcodePath);
			}
		}
	}
}