using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Assembly.Metro.Dialogs;
using Assembly.Windows;
using Assembly.Helpers;
using Blamite.Blam;
using Blamite.Blam.Scripting;
using Blamite.Blam.Scripting.Compiler;
using Blamite.IO;
using Blamite.Serialization;
using Blamite.Util;
using ICSharpCode.AvalonEdit.Search;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Microsoft.Win32;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Diagnostics;
using System.Linq;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.Editors
{
    /// <summary>
    ///     Interaction logic for ScriptEditor.xaml
    /// </summary>
    public partial class ScriptEditor : UserControl
    {
        private readonly EngineDescription _buildInfo;
        private readonly IScriptFile _scriptFile;
        private readonly IStreamManager _streamManager;
        private readonly OpcodeLookup _opcodes;
        private readonly ICacheFile _cashefile;
        private readonly string _casheName;
        private bool _showInfo;
        private Endian _endian;
        private Action _metaRefresh;
        private CompletionWindow _completionWindow = null;
        private readonly List<ICompletionData> _completionData = new List<ICompletionData>();

        public ScriptEditor(Action metaRefresh, EngineDescription buildInfo, IScriptFile scriptFile, IStreamManager streamManager, ICacheFile casheFile, string casheName, Endian endian)
        {
            _endian = endian;
            _metaRefresh = metaRefresh;
            _buildInfo = buildInfo;
            _opcodes = _buildInfo.ScriptInfo;
            _scriptFile = scriptFile;
            _streamManager = streamManager;
            _cashefile = casheFile;
            _casheName = casheName;
            _showInfo = App.AssemblyStorage.AssemblySettings.ShowScriptInfo;

            InitializeComponent();
            txtScript.TextArea.TextEntering += txtScript_TextArea_TextEntering;
            App.AssemblyStorage.AssemblySettings.PropertyChanged += Settings_SettingsChanged;
            SetHighlightColor();

            SearchPanel srch = SearchPanel.Install(txtScript);
            var bconv = new System.Windows.Media.BrushConverter();
            var srchbrsh = (System.Windows.Media.Brush)bconv.ConvertFromString("#40F0F0F0");
            srch.MarkerBrush = srchbrsh;

            List<Task> tasks = new List<Task>();
            tasks.Add(Task.Run(() => { GenerateCompletionData(); }));
            switch (_buildInfo.Name)
            {
                case "Halo: Reach":
                case "Halo: Reach MCC":
                case "Halo: Reach MCC Update 1":
                case "Halo: Reach MCC Update 2":
                    tasks.Add(Task.Run(() => { DecompileReach(); }));
                    break;
                default:
                    tasks.Add(Task.Run(() => { DecompileScripts(); }));
                    break;      
            }
        }

        private void DecompileScripts()
        {
            DateTime startTime = DateTime.Now;

            ScriptTable scripts;
            using (IReader reader = ((IStreamManager)_streamManager).OpenRead())
            {
                scripts = _scriptFile.LoadScripts(reader);
                if (scripts == null)
                    return;
            }

            OpcodeLookup opcodes = _buildInfo.ScriptInfo;
            var generator = new BlamScriptGenerator(scripts, opcodes, _endian);
            var code = new IndentedTextWriter(new StringWriter(CultureInfo.InvariantCulture));

            generator.WriteComment("Decompiled with Assembly", code);
            generator.WriteComment("", code);
            generator.WriteComment("Source file: " + _scriptFile.Name, code);
            generator.WriteComment("Start time: " + startTime, code);
            generator.WriteComment("", code);
            generator.WriteComment("Remember that all script code is property of Bungie/343 Industries.", code);
            generator.WriteComment("You have no rights. Play nice.", code);
            code.WriteLine();

            int counter = 0;

            if (scripts.Variables != null)
            {
                generator.WriteComment("VARIABLES", code);
                foreach (ScriptGlobal variable in scripts.Variables)
                {
                    code.Write("(variable {0} {1} ", opcodes.GetTypeInfo((ushort)variable.Type).Name, variable.Name);
                    generator.WriteExpression(variable.ExpressionIndex, code);
                    if (_showInfo)
                        code.WriteLine(")\t\t; Index: {0}, Expression Index: {1}", counter.ToString(), variable.ExpressionIndex.Index.ToString());
                    else
                        code.WriteLine(")");
                    counter++;
                }
                code.WriteLine();
                counter = 0;
            }

            generator.WriteComment("GLOBALS", code);
            foreach (ScriptGlobal global in scripts.Globals)
            {
                code.Write("(global {0} {1} ", _opcodes.GetTypeInfo((ushort)global.Type).Name, global.Name);
                generator.WriteExpression(global.ExpressionIndex, code);
                if (_showInfo)
                    code.WriteLine(")\t\t; Index: {0}, Expression Index: {1}", counter.ToString(), global.ExpressionIndex.Index.ToString());
                else
                    code.WriteLine(")");
                counter++;
            }
            code.WriteLine();
            counter = 0;

            generator.WriteComment("SCRIPTS", code);
            foreach (Script script in scripts.Scripts)
            {
                // filter out branch scripts which were generated by the compiler
                var split = script.Name.Split(new string[] { "_to_" }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length > 1 && scripts.Scripts.Exists(s => s.Name == split[0]))
                {
                    continue;
                }

                if (_showInfo)
                {
                    generator.WriteComment(string.Format("Index: {0}, Expression Index: {1}", counter.ToString(), script.RootExpressionIndex.Index.ToString()), code);
                }

                code.Write("(script {0} {1} {2}", _opcodes.GetScriptTypeName((ushort)script.ExecutionType),
                    _opcodes.GetTypeInfo((ushort)script.ReturnType).Name, script.Name);

                if (script.Parameters.Count > 0)
                {
                    code.Write(" (");

                    bool firstParam = true;
                    foreach (ScriptParameter param in script.Parameters)
                    {
                        if (!firstParam)
                            code.Write(", ");
                        code.Write("{1} {0}", param.Name, _opcodes.GetTypeInfo((ushort)param.Type).Name);
                        firstParam = false;
                    }

                    code.Write(")");
                }

                code.Indent++;
                code.WriteLine();
                generator.WriteExpression(script.RootExpressionIndex, code, _buildInfo.HeaderSize == 0x1E000);
                code.Indent--;

                code.WriteLine();
                code.WriteLine(")");
                code.WriteLine();

                code.Indent = 0;
                counter++;
            }

            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime.Subtract(startTime);
            generator.WriteComment("Decompilation finished in ~" + duration.TotalSeconds + "s", code);

            Dispatcher.Invoke(new Action(delegate { txtScript.Text = code.InnerWriter.ToString(); }));
        }

        private void DecompileReach()
        {
            DateTime startTime = DateTime.Now;

            ScriptTable scripts;
            using (IReader reader = ((IStreamManager)_streamManager).OpenRead())
            {
                scripts = _scriptFile.LoadScripts(reader);
                if (scripts == null)
                    return;
            }

            OpcodeLookup opcodes = _buildInfo.ScriptInfo;
            var code = new IndentedTextWriter(new StringWriter(CultureInfo.InvariantCulture));
            var decompiler = new BlamScriptDecompiler(code, scripts, opcodes, _endian);
            decompiler.WriteComment("Decompiled with Assembly");
            decompiler.WriteComment("Source file: " + _scriptFile.Name);
            decompiler.WriteComment("Start time: " + startTime);
            decompiler.WriteComment("Remember that all script code is property of Bungie/343 Industries.");
            decompiler.WriteComment("You have no rights. Play nice.");
            decompiler.Decompile(_showInfo);
            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime.Subtract(startTime);
            decompiler.WriteComment("Decompilation finished in ~" + duration.TotalSeconds + "s");
            Dispatcher.Invoke(new Action(delegate { txtScript.Text = code.InnerWriter.ToString(); }));
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.FileName = $"{_cashefile.InternalName}.hsc";
            sfd.Title = "Save Script As";
            sfd.Filter = "BlamScript Files|*.hsc|Text Files|*.txt|All Files|*.*";
            if (!(bool)sfd.ShowDialog())
                return;

            File.WriteAllText(sfd.FileName, txtScript.Text);
            MetroMessageBox.Show("Script Exported", "Script exported successfully.");
        }

        private async void btnCompile_Click(object sender, RoutedEventArgs e)
        {
            switch(_buildInfo.Name)
            {
                case "Halo: Reach":
                case "Halo: Reach MCC":
                case "Halo: Reach MCC Update 1":
                case "Halo: Reach MCC Update 2":
                    bool saved = false;
                    string folder = "Compiler";
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    string name = "compilation.log";
                    string filePath = Path.Combine(folder, name);

                    using (StreamWriter sw = File.CreateText(filePath))
                    {
                        Logger log = new Logger(sw);
                        string hsc = txtScript.Text;
                        var progress = new Progress<int>(v =>
                        {
                            progressBar.Value = v;

                        });
                        try
                        {

                            DateTime start = DateTime.Now;
                            Task<ScriptData> task = Task.Run(() => CompileScripts(hsc, progress, log));
                            await task.ContinueWith(t =>
                            {
                                ScriptData data = t.Result;

                                DateTime end = DateTime.Now;
                                TimeSpan duration = end.Subtract(start);

                                var res = Dispatcher.Invoke<MetroMessageBox.MessageBoxResult>(() => MetroMessageBox.Show("Success!", $"The scripts were successfully compiled in {duration.TotalSeconds} seconds."
                                    + "\nWARNING: This compiler is not 100% accurate and could corrupt your map.\nPlease backup your map before proceeding."
                                    + "\n\nDo you want to save the changes to the file?", MetroMessageBox.MessageBoxButtons.YesNo));
                                if (res == MetroMessageBox.MessageBoxResult.Yes)
                                {
                                    using (IStream stream = _streamManager.OpenReadWrite())
                                    {
                                        _scriptFile.SaveScripts(data, stream);
                                        _cashefile.SaveChanges(stream);
                                    }
                                    saved = true;
                                }
                            });
                        }
                        catch (AggregateException ex)
                        {
                            ex.Handle((x) =>
                            {
                                if (x is CompilerException)
                                {
                                    var comEx = x as CompilerException;
                                    MetroMessageBox.Show("Compiler Exception", $"{comEx.Message}\n\nText: \"{comEx.Text}\"\nLine: {comEx.Line}");
                                    log.WriteNewLine();
                                    log.WriteLine("EXCEPTION", $"{comEx.Message}\tText: {comEx.Text}\tLine: {comEx.Line}");
                                    return true;

                                }
                                else
                                {
                                    MetroMessageBox.Show(x.GetType().ToString(), x.Message);
                                    return true;
                                }
                            });
                        }
                    }

                    if (saved)
                    {
                        _metaRefresh();
                        StatusUpdater.Update("Scripts saved");
                    }

                    progressBar.Value = 0;
                    break;


                default:
                    MetroMessageBox.Show("Not Implemented", $"Unsupported Game: {_buildInfo.Name}");
                    break;
            }
        }

        private ScriptData CompileScripts(string code, IProgress<int> progress, Logger logger)
        {
            using (IReader reader = _streamManager.OpenRead())
            {
                ICharStream stream = CharStreams.fromstring(code);
                ITokenSource lexer = new BS_ReachLexer(stream);
                ITokenStream tokens = new CommonTokenStream(lexer);
                BS_ReachParser parser = new BS_ReachParser(tokens);
                parser.BuildParseTree = true;
                IParseTree tree = parser.hsc();
                ScriptContext scrContext = _scriptFile.LoadContext(reader);
                Dictionary<string, UnitSeatMapping> seats = LoadSeatMappings();
                 ScriptCompiler compiler = new ScriptCompiler(_cashefile, scrContext, _opcodes, seats, progress, logger);
                ParseTreeWalker.Default.Walk(compiler, tree);
                return compiler.Result();
            }
        }

        private Dictionary<string, UnitSeatMapping> LoadSeatMappings()
        {
            string folder = _buildInfo.SeatMappings;
            string filename = _casheName + "_Mappings.xml";
            string path = Path.Combine(folder, filename);

            XDocument doc = XDocument.Load(path);
            var mappings = doc.Element("UnitSeatMappings").Elements("Mapping");

            Dictionary<string, UnitSeatMapping> result = new Dictionary<string, UnitSeatMapping>();
            foreach (XElement mapping in mappings)
            {
                long index = XMLUtil.GetNumericAttribute(mapping, "Index");
                long count = XMLUtil.GetNumericAttribute(mapping, "Count");
                string name = XMLUtil.GetStringAttribute(mapping, "Name");

                if (result.ContainsKey(name))
                {
                    throw new Exception($"Duplicate unit seat mapping names: {name}");
                }

                UnitSeatMapping seat = new UnitSeatMapping((short)index, (short)count, name);
                result.Add(name, seat);
            }

            return result;
        }

        private void Settings_SettingsChanged(object sender, EventArgs e)
        {
            // Reset the highlight color in case the theme changed.
            SetHighlightColor();
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
            txtScript.TextArea.SelectionBorder = new System.Windows.Media.Pen(selbrsh, 1);
            selbrsh.Opacity = 0.3;
            txtScript.TextArea.SelectionBrush = selbrsh;
        }

        public void Dispose()
        {
            txtScript.Clear();
            App.AssemblyStorage.AssemblySettings.PropertyChanged -= Settings_SettingsChanged;
        }

        private void GenerateCompletionData()
        {
            _completionData.AddRange(_opcodes.GetAllImplementedFunctions().Select(info => new ScriptCompletion(info)));
            _completionData.AddRange(_opcodes.GetAllImplementedGlobals().Select(info => new ScriptCompletion(info)));
        }

        private void InitializaCompletionWindow()
        {
            // Set properties.
            var converter = new System.Windows.Media.BrushConverter();
            var bg = (System.Windows.Media.Brush)converter.ConvertFromString("#FF303032");
            _completionWindow = new CompletionWindow(txtScript.TextArea);
            _completionWindow.SizeToContent = SizeToContent.WidthAndHeight;
            _completionWindow.Background = bg;
            _completionWindow.WindowStyle = WindowStyle.None;
            _completionWindow.CompletionList.ListBox.Background = System.Windows.Media.Brushes.Transparent;
            _completionWindow.CompletionList.ListBox.Foreground = this.Foreground;
            _completionWindow.SizeChanged += completionWindow_SizeChanged;
            _completionWindow.Closed += delegate {
                _completionWindow = null;
            };

            // Add completion data to the list.
            IList<ICompletionData> data = _completionWindow.CompletionList.CompletionData;
            foreach (ScriptCompletion func in _completionData)
            {
                data.Add(func);
            }
        }

        private void txtScript_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && _completionWindow == null)
            {
                if(char.IsLetterOrDigit(e.Text[0]))
                {
                    InitializaCompletionWindow();
                    _completionWindow.Show();
                }
            }
        }

        private void completionWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CompletionWindow window = sender as CompletionWindow;
            if(window.CompletionList.ListBox.Items.Count == 0)
            {
                window.Close();
            }
        }
    }
}