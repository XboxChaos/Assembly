using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;
using Blamite.Blam;
using Blamite.Blam.ThirdGen;
using Blamite.Flexibility;
using Blamite.IO;
using Blamite.Patching;

namespace PatchApplier
{
    public partial class Form1 : Form
    {
	    private Patch _patch;

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

        private void browsePatch_Click(object sender, EventArgs e)
        {
			var ofd = new OpenFileDialog
				          {
					          Title = "Open Patch File", 
							  Filter = "Assembly Patch Files|*.asmp"
				          };
	        if (ofd.ShowDialog() != DialogResult.OK) return;

	        using (var reader = new EndianReader(File.OpenRead(ofd.FileName), Endian.BigEndian))
	        {
		        _patch = AssemblyPatchLoader.LoadPatch(reader);
		        patchAuthor.Text = _patch.Author;
		        patchDescription.Text = _patch.Description;
		        patchName.Text = _patch.Name;
	        }
	        patchPath.Text = ofd.FileName;
        }

        private void browseOutput_Click(object sender, EventArgs e)
        {
			var sfd = new SaveFileDialog
				          {
					          Title = "Save Map File", 
							  Filter = "Blam Map Files|*.map"
				          };
	        if (sfd.ShowDialog() == DialogResult.OK)
                outPath.Text = sfd.FileName;
        }

        private void applyPatch_Click(object sender, EventArgs e)
        {
            if (_patch == null)
            {
                MessageBox.Show("No patch loaded.");
                return;
            }

            // Copy the original map to the destination path
            File.Copy(unmoddedPath.Text, outPath.Text, true);

            // Open the destination map
			using (var stream = new EndianStream(File.Open(outPath.Text, FileMode.Open, FileAccess.ReadWrite), Endian.BigEndian))
            {
				var version = new CacheFileVersionInfo(stream);
				var loader = new BuildInfoLoader(XDocument.Load(@"Formats\SupportedBuilds.xml"), @"Formats\");
				var buildInfo = loader.LoadBuild(version.BuildString);
				var cacheFile = new ThirdGenCacheFile(stream, buildInfo, version.BuildString);

                // Apply the patch!
                Blamite.Patching.PatchApplier.ApplyPatch(_patch, cacheFile, stream);
            }

            MessageBox.Show("Done!");
        }
    }
}
