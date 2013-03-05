using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Diagnostics;
using Blamite.Plugins;

namespace PluginConverter
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void browseOriginal_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Select the folder containing your Ascension or Alteration plugins.";
            dialog.ShowNewFolderButton = false;
            if (dialog.ShowDialog() == DialogResult.OK)
                originalFolder.Text = dialog.SelectedPath;
        }

        private void browseOutput_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Select the folder to save the converted plugins to.";
            dialog.ShowNewFolderButton = true;
            if (dialog.ShowDialog() == DialogResult.OK)
                outputFolder.Text = dialog.SelectedPath;
        }

        private void convert_Click(object sender, EventArgs e)
        {
            string originalDir = originalFolder.Text;
            string outputDir = outputFolder.Text;

            if (!Directory.Exists(originalDir))
            {
                MessageBox.Show("The input folder \"" + originalDir + "\" does not exist.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!Directory.Exists(outputDir))
            {
                MessageBox.Show("The output folder \"" + outputDir + "\" does not exist.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            IEnumerable<string> files = Directory.EnumerateFiles(originalDir);
            convertProgress.Maximum = files.Count();
            convertProgress.Value = 0;
            convertProgress.Visible = true;
            convert.Visible = false;
            originalFolder.Enabled = false;
            browseOriginal.Enabled = false;
            outputFolder.Enabled = false;
            browseOutput.Enabled = false;

            BackgroundWorker worker = new BackgroundWorker();
            string gameName = targetGame.SelectedItem.ToString();
            worker.DoWork += (object o, DoWorkEventArgs args) => doConvert(worker, files, outputDir, gameName);
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync();
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            convertProgress.Value = e.ProgressPercentage;
            statusLabel.Text = (string)e.UserState;
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show("Plugin conversion complete.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            statusLabel.Text = "Ready";
            convert.Visible = true;
            convertProgress.Visible = false;
            originalFolder.Enabled = true;
            browseOriginal.Enabled = true;
            outputFolder.Enabled = true;
            browseOutput.Enabled = true;
        }

        private void doConvert(BackgroundWorker worker, IEnumerable<string> files, string outputDir, string gameName)
        {
            int progress = 0;
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                worker.ReportProgress(progress, "Converting " + name + "...");
                progress++;

                if ((file.EndsWith(".asc") || file.EndsWith(".alt") || file.EndsWith(".ent") || file.EndsWith(".xml")) && Path.GetFileNameWithoutExtension(file).Length == 4)
                {
                    XmlReader reader = null;
                    XmlWriter writer = null;
                    try
                    {
                        string extension = ".xml";
                        if (ascensionFmt.Checked)
                            extension = ".asc";
                        else if (alterationFmt.Checked)
                            extension = ".alt";

                        XmlWriterSettings settings = new XmlWriterSettings();
                        settings.Indent = true;
                        settings.IndentChars = "\t";
                        string outPath = Path.Combine(outputDir, Path.ChangeExtension(name, extension));
                        writer = XmlWriter.Create(outPath, settings);
                        reader = XmlReader.Create(file);

                        IPluginVisitor visitor;
                        if (assemblyFmt.Checked)
                            visitor = new AssemblyPluginWriter(writer, gameName);
                        else if (ascensionFmt.Checked)
                            visitor = new AscensionPluginWriter(writer, Path.GetFileNameWithoutExtension(file));
                        else
                            throw new InvalidOperationException("Unsupported output format.");

                        UniversalPluginLoader.LoadPlugin(reader, visitor);
                    }
                    finally
                    {
                        if (reader != null)
                            reader.Close();
                        if (writer != null)
                            writer.Close();
                    }
                }
            }
            worker.ReportProgress(progress, "Done!");
        }

        private void folder_TextChanged(object sender, EventArgs e)
        {
            convert.Enabled = (!string.IsNullOrWhiteSpace(originalFolder.Text) && !string.IsNullOrWhiteSpace(outputFolder.Text));
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            targetGame.SelectedIndex = 0;
        }

        private void assemblyFmt_CheckedChanged(object sender, EventArgs e)
        {
            // Only Assembly plugins use the "target game" feature
            targetGame.Enabled = assemblyFmt.Checked;
        }
    }
}
