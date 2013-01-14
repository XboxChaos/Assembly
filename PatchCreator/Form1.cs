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
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open Unmodded Map File";
            ofd.Filter = "Blam Map Files|*.map";
            if (ofd.ShowDialog() == DialogResult.OK)
                unmoddedPath.Text = ofd.FileName;
        }

        private void browseModded_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open Modded Map File";
            ofd.Filter = "Blam Map Files|*.map";
            if (ofd.ShowDialog() == DialogResult.OK)
                moddedPath.Text = ofd.FileName;
        }

        private void browseOutput_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Save Patch File"; 
            sfd.Filter = "Assembly Patch Files|*.asmp";
            if (sfd.ShowDialog() == DialogResult.OK)
                outPath.Text = sfd.FileName;
        }

        private void makePatch_Click(object sender, EventArgs e)
        {
            Patch patch = new Patch();
            patch.Name = patchName.Text;
            patch.Description = patchDescription.Text;
            patch.Author = patchAuthor.Text;

            IReader originalReader = new EndianReader(File.OpenRead(unmoddedPath.Text), Endian.BigEndian);
            IReader newReader = new EndianReader(File.OpenRead(moddedPath.Text), Endian.BigEndian);
            ThirdGenVersionInfo version = new ThirdGenVersionInfo(originalReader);
            BuildInfoLoader loader = new BuildInfoLoader(XDocument.Load(@"Formats\SupportedBuilds.xml"), @"Formats\");
            BuildInformation buildInfo = loader.LoadBuild(version.BuildString);
            ThirdGenCacheFile originalFile = new ThirdGenCacheFile(originalReader, buildInfo, version.BuildString);
            ThirdGenCacheFile newFile = new ThirdGenCacheFile(newReader, buildInfo, version.BuildString);
            MetaComparer.CompareMeta(originalFile, originalReader, newFile, newReader, patch);
            originalReader.Close();
            newReader.Close();

            IWriter output = new EndianWriter(File.OpenWrite(outPath.Text), Endian.BigEndian);
            AssemblyPatchWriter.WritePatch(patch, output);
            output.Close();

            MessageBox.Show("Done!");
        }
    }
}
