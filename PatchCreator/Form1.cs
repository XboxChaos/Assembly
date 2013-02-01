using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;
using ExtryzeDLL.Patching;

namespace PatchCreator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void browseUnmodded_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
	                      {
		                      Title = "Open Unmodded Map File",
							  Filter = "Blam Map Files|*.map"
	                      };
	        if (ofd.ShowDialog() == DialogResult.OK)
                unmoddedPath.Text = ofd.FileName;
        }
        private void browseModded_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
	                      {
		                      Title = "Open Modded Map File",
							  Filter = "Blam Map Files|*.map"
	                      };
	        if (ofd.ShowDialog() == DialogResult.OK)
                moddedPath.Text = ofd.FileName;
        }
        private void browseOutput_Click(object sender, EventArgs e)
        {
			var sfd = new SaveFileDialog
				          {
					          Title = "Save Patch File", 
							  Filter = "Assembly Patch Files|*.asmp"
				          };
	        if (sfd.ShowDialog() == DialogResult.OK)
                outPath.Text = sfd.FileName;
        }

        private void makePatch_Click(object sender, EventArgs e)
        {
			var patch = new Patch
				            {
					            Name = patchName.Text, 
								Description = patchDescription.Text,
								Author = patchAuthor.Text
				            };

	        IReader originalReader = new EndianReader(File.OpenRead(unmoddedPath.Text), Endian.BigEndian);
            IReader newReader = new EndianReader(File.OpenRead(moddedPath.Text), Endian.BigEndian);

			var version = new ThirdGenVersionInfo(originalReader);
			var loader = new BuildInfoLoader(XDocument.Load(@"Formats\SupportedBuilds.xml"), @"Formats\");
			var buildInfo = loader.LoadBuild(version.BuildString);
			var originalFile = new ThirdGenCacheFile(originalReader, buildInfo, version.BuildString);
			var newFile = new ThirdGenCacheFile(newReader, buildInfo, version.BuildString);

            patch.MapInternalName = originalFile.Info.InternalName;
            MetaComparer.CompareMeta(originalFile, originalReader, newFile, newReader, patch);
            LocaleComparer.CompareLocales(originalFile, originalReader, newFile, newReader, patch);

            originalReader.Close();
            newReader.Close();

            IWriter output = new EndianWriter(File.OpenWrite(outPath.Text), Endian.BigEndian);
            AssemblyPatchWriter.WritePatch(patch, output);
            output.Close();

            MessageBox.Show("Done!");
        }
    }
}
