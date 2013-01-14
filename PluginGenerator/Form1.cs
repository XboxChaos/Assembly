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
using System.Xml;
using System.Xml.Linq;
using ExtryzeDLL.Blam;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;
using ExtryzeDLL.Plugins;
using ExtryzeDLL.Plugins.Generation;
using ExtryzeDLL.Util;

namespace PluginGenerator
{
    public partial class Form1 : Form
    {
        private BuildInfoLoader _buildLoader;

        private class GeneratorWorkerArgs
        {
            public BackgroundWorker Worker;
            public string InputFolder;
            public string OutputFolder;
        }

        public Form1()
        {
            InitializeComponent();

            XDocument supportedBuilds = XDocument.Load(@"Formats\SupportedBuilds.xml");
            _buildLoader = new BuildInfoLoader(supportedBuilds, @"Formats\");
        }

        private void btnBrowseMaps_Click(object sender, EventArgs e)
        {
            DoBrowseFolder(txtMapFolder, "Select the folder containing unmodified .map files to analyze.", false);
        }

        private void btnBrowseOutput_Click(object sender, EventArgs e)
        {
            DoBrowseFolder(txtOutputFolder, "Select the folder to store generated plugins to.", true);
        }

        private void DoBrowseFolder(TextBox textBox, string description, bool newFolderButton)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = textBox.Text;
            dialog.Description = description;
            dialog.ShowNewFolderButton = newFolderButton;
            if (dialog.ShowDialog() == DialogResult.OK)
                textBox.Text = dialog.SelectedPath;
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            btnGenerate.Enabled = false;
            progressBar.Value = 0;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += worker_DoWork;
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;

            GeneratorWorkerArgs args = new GeneratorWorkerArgs()
            {
                Worker = worker,
                InputFolder = txtMapFolder.Text,
                OutputFolder = txtOutputFolder.Text
            };
            worker.RunWorkerAsync(args);
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnGenerate.Enabled = true;
            if (e.Error == null)
                MessageBox.Show("Plugin generation completed successfully in " + Math.Round(((TimeSpan)e.Result).TotalSeconds, 3) + " seconds.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show(e.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            GeneratorWorkerArgs args = (GeneratorWorkerArgs)e.Argument;

            Dictionary<string, MetaMap> globalMaps = new Dictionary<string, MetaMap>();
            DateTime startTime = DateTime.Now;

            List<string> mapFiles = Directory.EnumerateFiles(args.InputFolder, "*.map").ToList();
            for (int i = mapFiles.Count - 1; i >= 0; i--)
            {
                if (mapFiles[i].EndsWith("shared.map") || mapFiles[i].EndsWith("campaign.map"))
                    mapFiles.RemoveAt(i);
            }

            for (int i = 0; i < mapFiles.Count; i++)
            {
                Dictionary<ITag, MetaMap> tagMaps = new Dictionary<ITag, MetaMap>();

                IReader reader;
                ICacheFile cacheFile = LoadMap(mapFiles[i], out reader);
                MetaAnalyzer analyzer = new MetaAnalyzer(cacheFile);

                Queue<MetaMap> mapsToProcess = new Queue<MetaMap>();
                foreach (ITag tag in cacheFile.Tags)
                {
                    if (tag.MetaLocation.AsAddress() > 0)
                    {
                        MetaMap map = new MetaMap();
                        tagMaps[tag] = map;
                        mapsToProcess.Enqueue(map);

                        reader.SeekTo(tag.MetaLocation.AsOffset());
                        analyzer.AnalyzeArea(reader, tag.MetaLocation.AsAddress(), map);
                    }
                }
                GenerateSubMaps(mapsToProcess, analyzer, reader, cacheFile);

                Dictionary<string, MetaMap> classMaps = new Dictionary<string, MetaMap>();
                foreach (ITag tag in cacheFile.Tags)
                {
                    if (tag.MetaLocation.AsAddress() > 0)
                    {
                        MetaMap map = tagMaps[tag];
                        EstimateMapSize(map, tag.MetaLocation.AsAddress(), analyzer.GeneratedMemoryMap, 1);

                        string magicStr = CharConstant.ToString(tag.Class.Magic);
                        MetaMap oldClassMap;
                        if (classMaps.TryGetValue(magicStr, out oldClassMap))
                            oldClassMap.MergeWith(map);
                        else
                            classMaps[magicStr] = map;
                    }
                }

                foreach (KeyValuePair<string, MetaMap> map in classMaps)
                {
                    MetaMap globalMap;
                    if (globalMaps.TryGetValue(map.Key, out globalMap))
                        globalMap.MergeWith(map.Value);
                    else
                        globalMaps[map.Key] = map.Value;
                }

                reader.Close();

                args.Worker.ReportProgress(100 * i / (mapFiles.Count - 1));
            }

            string badChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (KeyValuePair<string, MetaMap> map in globalMaps)
            {
                string filename = map.Key;
                foreach (char badChar in badChars)
                    filename = filename.Replace(badChar, '_');
                filename += ".xml";
                string path = Path.Combine(args.OutputFolder, filename);

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.IndentChars = "\t";
                XmlWriter writer = XmlWriter.Create(path, settings);
                AssemblyPluginWriter pluginWriter = new AssemblyPluginWriter(writer, "Halo4");

                int size = map.Value.GetBestSizeEstimate();
                FoldSubMaps(map.Value);

                pluginWriter.EnterPlugin(size);
                
                pluginWriter.EnterRevisions();
                pluginWriter.VisitRevision(new PluginRevision("Assembly", 1, "Generated plugin from scratch."));
                pluginWriter.LeaveRevisions();

                WritePlugin(map.Value, size, pluginWriter);
                pluginWriter.LeavePlugin();
                
                writer.Dispose();
            }

            DateTime endTime = DateTime.Now;
            e.Result = endTime.Subtract(startTime);
        }

        private void WritePlugin(MetaMap map, int size, AssemblyPluginWriter writer)
        {
            for (int offset = 0; offset < size; offset += 4)
            {
                MetaValueGuess guess = map.GetGuess(offset);
                if (guess != null)
                {
                    switch (guess.Type)
                    {
                        case MetaValueType.DataReference:
                            if (offset <= size - 0x14)
                            {
                                writer.VisitDataReference("Unknown", (uint)offset, false, 0);
                                offset += 0x10;
                                continue;
                            }
                            break;

                        case MetaValueType.TagReference:
                            if (offset <= size - 0x10)
                            {
                                writer.VisitTagReference("Unknown", (uint)offset, false, true, true, 0);
                                offset += 0xC;
                                continue;
                            }
                            break;

                        case MetaValueType.Reflexive:
                            if (offset <= size - 0xC)
                            {
                                MetaMap subMap = map.GetSubMap(offset);
                                if (subMap != null)
                                {
                                    int subMapSize = subMap.GetBestSizeEstimate();
                                    writer.EnterReflexive("Unknown", (uint)offset, false, (uint)subMapSize, 0);
                                    WritePlugin(subMap, subMapSize, writer);
                                    writer.LeaveReflexive();
                                    offset += 0x8;
                                    continue;
                                }
                            }
                            break;
                    }
                }

                // Just write an unknown value depending upon how much space we have left
                if (offset <= size - 4)
                    writer.VisitUndefined("Unknown", (uint)offset, false, 0);
                else if (offset <= size - 2)
                    writer.VisitInt16("Unknown", (uint)offset, false, 0);
                else
                    writer.VisitInt8("Unknown", (uint)offset, false, 0);
            }
        }

        private void GenerateSubMaps(Queue<MetaMap> maps, MetaAnalyzer analyzer, IReader reader, ICacheFile cacheFile)
        {
            Dictionary<uint, MetaMap> generatedMaps = new Dictionary<uint, MetaMap>();
            while (maps.Count > 0)
            {
                MetaMap map = maps.Dequeue();
                foreach (MetaValueGuess guess in map.Guesses)
                {
                    if (guess.Type == MetaValueType.Reflexive)
                    {
                        MetaMap subMap;
                        if (!generatedMaps.TryGetValue(guess.Pointer, out subMap))
                        {
                            subMap = new MetaMap();
                            reader.SeekTo(cacheFile.MetaPointerConverter.AddressToOffset(guess.Pointer));
                            analyzer.AnalyzeArea(reader, guess.Pointer, subMap);
                            maps.Enqueue(subMap);
                            generatedMaps[guess.Pointer] = subMap;
                        }
                        map.AssociateSubMap(guess.Offset, subMap);
                    }
                }
            }
        }

        private void EstimateMapSize(MetaMap map, uint mapAddress, MemoryMap memMap, int entryCount)
        {
            bool alreadyVisited = map.HasSizeEstimates;

            int newSize = memMap.EstimateBlockSize(mapAddress);
            map.EstimateSize(newSize / entryCount);
            map.Truncate(newSize);

            if (!alreadyVisited)
            {    
                foreach (MetaValueGuess guess in map.Guesses)
                {
                    if (guess.Type == MetaValueType.Reflexive)
                    {
                        MetaMap subMap = map.GetSubMap(guess.Offset);
                        if (subMap != null)
                            EstimateMapSize(subMap, guess.Pointer, memMap, (int)guess.Data1);
                    }
                }
            }
        }

        private void FoldSubMaps(MetaMap map)
        {
            foreach (MetaValueGuess guess in map.Guesses)
            {
                if (guess.Type == MetaValueType.Reflexive)
                {
                    MetaMap subMap = map.GetSubMap(guess.Offset);
                    if (subMap != null)
                    {
                        int entryCount = (int)guess.Data1;
                        int firstBlockSize = subMap.GetBestSizeEstimate();
                        //if (firstBlockSize > 0 && !subMap.IsFolded(firstBlockSize))
                        //{
                            subMap.Fold(firstBlockSize);
                            FoldSubMaps(subMap);
                        //}
                    }
                }
            }
        }

        private ICacheFile LoadMap(string path, out IReader reader)
        {
            reader = new EndianReader(File.OpenRead(path), Endian.BigEndian);
            ThirdGenVersionInfo versionInfo = new ThirdGenVersionInfo(reader);
            BuildInformation buildInfo = _buildLoader.LoadBuild(versionInfo.BuildString);
            return new ThirdGenCacheFile(reader, buildInfo, versionInfo.BuildString);
        }
    }
}
