using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;
using ExtryzeDLL.Patching;

namespace PatchApplier
{
    public partial class Form1 : Form
    {
        private Patch _patch = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void browseUnmodded_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open Unmodded Map File";
            ofd.Filter = "Blam Map Files|*.map";
            if (ofd.ShowDialog() == DialogResult.OK)
                unmoddedPath.Text = ofd.FileName;
        }

        private void browsePatch_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open Patch File";
            ofd.Filter = "Assembly Patch Files|*.asmp";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                using (EndianReader reader = new EndianReader(File.OpenRead(ofd.FileName), Endian.BigEndian))
                {
                    _patch = AssemblyPatchLoader.LoadPatch(reader);
                    patchAuthor.Text = _patch.Author;
                    patchDescription.Text = _patch.Description;
                    patchName.Text = _patch.Name;
                }
                patchPath.Text = ofd.FileName;
            }
        }

        private void browseOutput_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Save Map File";
            sfd.Filter = "Blam Map Files|*.map";
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
            using (EndianStream stream = new EndianStream(File.Open(outPath.Text, FileMode.Open, FileAccess.ReadWrite), Endian.BigEndian))
            {
                ThirdGenVersionInfo version = new ThirdGenVersionInfo(stream);
                BuildInfoLoader loader = new BuildInfoLoader(XDocument.Load(@"Formats\SupportedBuilds.xml"), @"Formats\");
                BuildInformation buildInfo = loader.LoadBuild(version.BuildString);
                ThirdGenCacheFile cacheFile = new ThirdGenCacheFile(stream, buildInfo, version.BuildString);

                // Apply the patch!
                MetaPatcher.WriteChanges(_patch.MetaChanges, cacheFile, stream);
                LocalePatcher.WriteLanguageChanges(_patch.LanguageChanges, cacheFile, stream);
                cacheFile.SaveChanges(stream);
            }

            MessageBox.Show("Done!");
        }
    }
}
