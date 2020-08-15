﻿using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Assembly.Metro.Dialogs;
using Assembly.Windows;
using Assembly.Helpers;
using Assembly.SyntaxHighlighting;
using Blamite.Blam;
using Blamite.Blam.Scripting;
using Blamite.Blam.Scripting.Compiler;
using Blamite.Blam.Scripting.Context;
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
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;
using Assembly.Helpers.CodeCompletion.Scripting;
using System.Threading;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Windows.Media;
using System.ComponentModel;
using System.Text;
using ICSharpCode.AvalonEdit.Editing;

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
        private Endian _endian;
        private CompletionWindow _completionWindow = null;
        private ScriptingContextCollection _context;
        private CancellationTokenSource _searchToken;
        private Regex _scriptRegex;
        private Regex _globalsRegex;
        private IEnumerable<ICompletionData> _staticCompletionData = new ICompletionData[0];
        private IEnumerable<ICompletionData> _dynamicScriptCompletionData = new ICompletionData[0];
        private IEnumerable<ICompletionData> _dynamicGlobalCompletionData = new ICompletionData[0];
        private bool _loaded = false;
        private readonly bool _compilationSupported;

        public ScriptEditor(EngineDescription buildInfo, IScriptFile scriptFile, IStreamManager streamManager, ICacheFile casheFile, string casheName, Endian endian)
        {
            _endian = endian;
            _buildInfo = buildInfo;
            _opcodes = _buildInfo.ScriptInfo;
            _scriptFile = scriptFile;
            _streamManager = streamManager;
            _cashefile = casheFile;
            _casheName = casheName;

            // All games other than H4 use the old blam script syntax. Currently the compiler only supports the old syntax.
            _compilationSupported = !buildInfo.Name.Contains("Halo 4") && !(buildInfo.ScriptingContextPath is null);

            InitializeComponent();

            // Disable user input. Enable it again when all background tasks have been completed.
            txtScript.IsReadOnly = true;

            // Enable code completion only if the compiler supports this game.
            if(_compilationSupported)
            {
                GotFocus += EditorGotFocus;
                LostFocus += EditorLostFocus;
                txtScript.TextArea.TextEntering += EditorTextEntering;
                txtScript.TextArea.TextEntered += EditorTextEntered;
                txtScript.TextArea.Document.Changed += EditorTextChanged;
            }

            App.AssemblyStorage.AssemblySettings.PropertyChanged += Settings_SettingsChanged;
            SetHighlightColor();
            SearchPanel srch = SearchPanel.Install(txtScript);
            var bconv = new System.Windows.Media.BrushConverter();
            var srchbrsh = (System.Windows.Media.Brush)bconv.ConvertFromString("#40F0F0F0");
            srch.MarkerBrush = srchbrsh;

            txtScript.SyntaxHighlighting = LoadSyntaxHighlighting(_opcodes);
        }


        #region Event Handlers
        private async void EditorLoadedAsync(object sender, RoutedEventArgs e)
        {
            if (!_loaded)
            {
                if (_compilationSupported)
                {
                    // Background tasks.      
                    List<Task> tasks = new List<Task>();

                    Task regexTask = Task.Run(() => CreateRegex());
                    Task<string> decompilationTask = Task.Run(() => DecompileToLISP());
                    Task contextTask = Task.Run(() => LoadContext());
                    Task<IEnumerable<ICompletionData>> completionTask = contextTask.ContinueWith(t => GenerateStaticCompletionData());
                    tasks.Add(regexTask);
                    tasks.Add(decompilationTask);
                    tasks.Add(contextTask);
                    tasks.Add(completionTask);

                    await Task.WhenAll(tasks);
                    _staticCompletionData = await completionTask;
                    txtScript.Text = await decompilationTask;
                }
                else if (_buildInfo.Name.Contains("Halo 4"))
                {
                    txtScript.Text = await Task.Run(() => DecompileHalo4Old());
                }
                else
                {
                    txtScript.Text = await Task.Run(() => DecompileToLISP());
                }
                txtScript.IsReadOnly = false;
                _loaded = true;
            }
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

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "BlamScript Files|*.hsc|Text Files|*.txt|All Files|*.*";
            if (!(bool)ofd.ShowDialog())
                return;
            txtScript.Text = File.ReadAllText(ofd.FileName);
        }

        private async void CompileButtonClick(object sender, RoutedEventArgs e)
        {
            if (_buildInfo.Name.Contains("Reach"))
            {
                // Logger Setup
                string folder = "Compiler";
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                string logPath = Path.Combine(folder, "ScriptCompiler.log");

                using (var logStream = File.Create(logPath))
                {
                    // Create the logger and the exception collector. They are used for debugging.
                    var traceListener = new TextWriterTraceListener(logStream);
                    var logger = new ScriptCompilerLogger(traceListener);
                    var exceptionCollector = new ParsingExceptionCollector();
                    logger.Information($"Attempting to compile: {_scriptFile.Name}, Time: {DateTime.Now}");
                    try
                    {
                        // Get the script file.
                        string hsc = txtScript.Text;

                        // Setup the progress handler.
                        var progress = new Progress<int>(v => { progressBar.Value = v; });

                        // Measure the time it took to compile the scripts.
                        var stopWatch = Stopwatch.StartNew();

                        // Compile the scripts.
                        ScriptData compileData = await Task.Run(() => CompileScripts(hsc, progress, logger, exceptionCollector));
                        stopWatch.Stop();
                        var timeSpan = stopWatch.Elapsed;
                        string compilationMessage = $"The scripts were successfully compiled in {timeSpan.TotalSeconds} seconds.";
                        logger.Information(compilationMessage);

                        // Show the message box.
                        var saveResult = MetroMessageBox.Show("Scripts Compiled", compilationMessage
                                + "\nWARNING: This compiler is not 100% accurate and could corrupt your map."
                                + "\n\nDo you want to save the changes to the file?", MetroMessageBox.MessageBoxButtons.YesNo);
                        if(saveResult == MetroMessageBox.MessageBoxResult.Yes)
                        {
                            //TODO: Move this to its own function.
                            await Task.Run(() =>
                            {
                                using (IStream stream = _streamManager.OpenReadWrite())
                                {
                                    _scriptFile.SaveScripts(compileData, stream);
                                    _cashefile.SaveChanges(stream);
                                }
                            });
                            RefreshMeta();
                            StatusUpdater.Update("Scripts saved");
                        }
                    }
                    // Handle Parsing Errors.
                    catch(OperationCanceledException opEx)
                    {
                        if (exceptionCollector.ContainsExceptions)
                        {
                            HandleParsingErrors(opEx, exceptionCollector, logger);
                        }
                        else
                        {
                            MetroMessageBox.Show("Operation Canceled", opEx.Message);
                        }

                    }
                    // Handle Compiler Errors.
                    catch(CompilerException compEx)
                    {
                        HandleCompilerErrors(compEx, logger);
                    }
                    finally
                    {
                        logger.Flush();
                        progressBar.Value = 0;
                    }
                }
            }
            else
            {
                MetroMessageBox.Show("Game Not Supported", $"Script compilation for {_buildInfo.Name} is not supoprted yet.");
            }
        }

        private void EditorGotFocus(object sender, RoutedEventArgs e)
        {
            // Start the background search.
            _searchToken = new CancellationTokenSource();
            Task backGroundSearch = BackgroundSearchAsync(TimeSpan.FromSeconds(5.0), _searchToken.Token);
        }

        private void EditorLostFocus(object sender, RoutedEventArgs e)
        {
            // Stop the background search.
            _searchToken.Cancel();
            _searchToken.Dispose();
        }

        private async void Settings_SettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "ShowScriptInfo")
            {
                if(_buildInfo.Name.Contains("Halo 4"))
                {
                    txtScript.Text = await Task.Run(() => DecompileHalo4Old());
                }
                else
                {
                    txtScript.Text = await Task.Run(() => DecompileToLISP() );
                }
            }
            else if(e.PropertyName == "ApplicationAccent")
            {
                SetHighlightColor();
            }
        }

        private void EditorTextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && _completionWindow == null)
            {
                if (char.IsLetterOrDigit(e.Text[0]))
                {
                    InitializaCompletionWindow();
                    _completionWindow.Show();
                }
            }
        }

        private void EditorTextEntered(object sender, TextCompositionEventArgs e)
        {
            if (_completionWindow != null && !_completionWindow.CompletionList.ListBox.HasItems)
            {
                _completionWindow.Close();
            }
        }

        private void EditorTextChanged(object sender, DocumentChangeEventArgs e)
        {
            if (e.InsertedText.Text.Contains("script") || e.RemovedText.Text.Contains("script"))
            {
                _ = FindScriptNamesAsync(txtScript.Text);
            }

            if (e.InsertedText.Text.Contains("global") || e.RemovedText.Text.Contains("global"))
            {
                _ = FindGlobalNamesAsync(txtScript.Text);
            }
        }

        #endregion


        #region Functions
        private string DecompileHalo4Old()
        {
            bool _showInfo = App.AssemblyStorage.AssemblySettings.ShowScriptInfo;

            DateTime startTime = DateTime.Now;

            ScriptTable scripts;
            using (IReader reader = ((IStreamManager)_streamManager).OpenRead())
            {
                scripts = _scriptFile.LoadScripts(reader);
                if (scripts == null)
                    return "";
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

            return code.InnerWriter.ToString();
        }

        private string DecompileToLISP()
        {
            var watch = Stopwatch.StartNew();

            ScriptTable scripts;
            using (IReader reader = ((IStreamManager)_streamManager).OpenRead())
            {
                scripts = _scriptFile.LoadScripts(reader);
                if (scripts == null)
                    return "";
            }

            OpcodeLookup opcodes = _buildInfo.ScriptInfo;
            var code = new IndentedTextWriter(new StringWriter(CultureInfo.InvariantCulture));
            var decompiler = new BlamScriptDecompiler(code, scripts, opcodes, _endian);
            decompiler.WriteComment("Decompiled with Assembly");
            decompiler.WriteComment("Source file: " + _scriptFile.Name);
            decompiler.WriteComment("Start time: " + DateTime.Now);
            decompiler.WriteComment("Remember that all script code is property of Bungie/343 Industries.");
            decompiler.WriteComment("You have no rights. Play nice.");
            decompiler.Decompile(App.AssemblyStorage.AssemblySettings.ShowScriptInfo);
            watch.Stop();
            decompiler.WriteComment("Decompilation finished in ~" + watch.Elapsed.TotalSeconds + "s");
            return code.InnerWriter.ToString();
        }

        private void LoadContext()
        {
            // Load the context and seat mappings.
            var loader = new XMLScriptingContextLoader(_cashefile, _streamManager, _buildInfo);
            string folder = _buildInfo.SeatMappingPath;
            string filename = Path.GetFileNameWithoutExtension(_cashefile.FileName) + "_Mappings.xml";
            string unitSeatMappingPath = Path.Combine(folder, filename);
            _context = loader.LoadContext(_buildInfo.ScriptingContextPath, unitSeatMappingPath);
        }

        private ScriptData CompileScripts(string code, IProgress<int> progress, ScriptCompilerLogger logger, ParsingExceptionCollector collector)
        {
            using (IReader reader = _streamManager.OpenRead())
            {
                // Set up the lexer.
                logger.Information("Running the lexer...");
                ICharStream stream = CharStreams.fromstring(code);
                BS_ReachLexer lexer = new BS_ReachLexer(stream);
                lexer.AddErrorListener(collector);
                ITokenStream tokens = new CommonTokenStream(lexer);

                // Set up the parser.
                logger.Information("Running the parser...");
                BS_ReachParser parser = new BS_ReachParser(tokens);
                parser.AddErrorListener(collector);

                // Parse the scripts.
                parser.BuildParseTree = true;
                IParseTree tree = parser.hsc();

                // Throw an exception if ANTLR reports parsing or lexing errors.
                if(collector.ContainsExceptions)
                {
                    logger.Information("The collector contained errors. Cancelling the process...");
                    throw new OperationCanceledException($"Parsing Failed! {collector.ExceptionCount} Exceptions occured during the parsing process.");
                }

                // Run the compiler.
                logger.Information("Running the compiler...");
                ScriptCompiler compiler = new ScriptCompiler(_cashefile, _opcodes, _context, progress, logger, true);
                ParseTreeWalker.Default.Walk(compiler, tree);
                return compiler.Result();
            }
        }

        // TODO: Find out to properly handle themes.
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

        private IHighlightingDefinition LoadSyntaxHighlighting(OpcodeLookup lookup)
        {
            string filename = "BlamScriptSolarized.xshd";
            return HighlightLoader.LoadEmbeddedBlamScriptDefinition(filename, _opcodes);
        }

        // Is this necessary?
        public void Dispose()
        {
            txtScript.Clear();
            App.AssemblyStorage.AssemblySettings.PropertyChanged -= Settings_SettingsChanged;
        }

        private IEnumerable<ICompletionData> GenerateStaticCompletionData()
        {
            List<ICompletionData> result = new List<ICompletionData>();

            // Functions.
            result.AddRange(_opcodes.GetAllImplementedFunctions().Select(info => new FunctionCompletion(info)));

            // Engine globals.
            result.AddRange(_opcodes.GetAllImplementedGlobals().Select(info => new GlobalCompletion(info, GlobalType.Engine)));

            // Objects.
            IEnumerable<ObjectCompletion> objectCompletion = _context.GetAllContextObjects().Select(obj => new ObjectCompletion(obj));
            IEnumerable<ObjectCompletion> uniqueObjectCompletion = objectCompletion.GroupBy(obj => new { obj.Text, obj.Description }).Select(obj => obj.First());
            result.AddRange(uniqueObjectCompletion);

            // Unit seat mappings.
            result.AddRange(_context.GetAllUnitSeatMappings().Select(mapping => new ObjectCompletion(mapping)));

            return result;
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
            _completionWindow.Closed += delegate { _completionWindow = null; };
            _completionWindow.CloseAutomatically = true;
            _completionWindow.CloseWhenCaretAtBeginning = true;

            // Add completion data to the list.
            IList<ICompletionData> data = _completionWindow.CompletionList.CompletionData;

            // Static Items.
            foreach (var item in _staticCompletionData)
            {
                data.Add(item);
            }

            // Dynamic Script Items.
            foreach (var item in _dynamicScriptCompletionData)
            {
                data.Add(item);
            }

            // Dynamic Global Items.
            foreach (var item in _dynamicGlobalCompletionData)
            {
                data.Add(item);
            }
        }

        private void CreateRegex()
        {
            string valueTypePattern = "(?<ValueType>" + string.Join("|", _opcodes.GetAllValueTypeNames()) + ")";
            string scriptTypePattern = "(?<ScriptType>" + string.Join("|", _opcodes.GetAllScriptTypeNames()) + ")";
            string scriptPattern = @"\bscript\s+" + scriptTypePattern + @"\s+" + valueTypePattern + @"\s+(?<Name>\S+)";
            string globalsPattern = @"\bglobal\s+" + valueTypePattern + @"\s+(?<Name>\S+)";
            _scriptRegex = new Regex(scriptPattern, RegexOptions.Compiled);
            _globalsRegex = new Regex(globalsPattern, RegexOptions.Compiled);
        }

        private async Task FindScriptNamesAsync(string hsc)
        {
            IEnumerable<ICompletionData> data = await Task.Run(() => 
            { 
                var collection = _scriptRegex.Matches(hsc);
                var matches = collection.OfType<Match>().ToArray();
                return matches.Select(match => new ScriptCompletion(match.Groups["Name"].Value, match.Groups["ScriptType"].Value, match.Groups["ValueType"].Value));
            });
            _dynamicScriptCompletionData = data;
        }

        private async Task FindGlobalNamesAsync(string hsc)
        {
            IEnumerable<ICompletionData> data = await Task.Run(() =>
            {
                var collection = _globalsRegex.Matches(hsc);
                var matches = collection.OfType<Match>().ToArray();
                return matches.Select(match => new GlobalCompletion(match.Groups["Name"].Value, match.Groups["ValueType"].Value, GlobalType.Map));
            });
            _dynamicGlobalCompletionData = data;
        }

        private async Task BackgroundSearchAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    await FindGlobalNamesAsync(txtScript.Text);
                    await FindScriptNamesAsync(txtScript.Text);
                    await Task.Delay(interval, cancellationToken);
                }
            }
            catch (TaskCanceledException _)
            {

            }
        }

        private void RefreshMeta()
        {
            DependencyObject parent = VisualTreeHelper.GetParent(this);
            while(parent != null)
            {
                var haloMap = parent as HaloMap;
                if(haloMap != null)
                {
                    haloMap.RefreshTags();
                    StatusUpdater.Update("Meta Refreshed");
                    return;
                }
                else
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }
            }
        }

        private void HandleParsingErrors(OperationCanceledException ex, ParsingExceptionCollector collector, ScriptCompilerLogger logger)
        {
            StringBuilder sb = new StringBuilder();
            ParsingExceptionInformation[] errors = collector.GetExceptions().ToArray();
            int counter = errors.Length <= 10 ? errors.Length : 10;
            for (int i = 0; i < counter; i++)
            {
                string errorMessage = $"{i + 1}: {errors[i].Message}, Line: {errors[i].Line}, Column: {errors[i].Column}";
                sb.AppendLine(errorMessage);
                logger.Error("Parsing Error " + errorMessage);
            }

            var _ = MetroMessageBox.Show("Parsing Failed", sb.ToString(), MetroMessageBox.MessageBoxButtons.Ok);
            if (txtScript.TextArea.Focus())
            {
                // Go to the first error.
                var firstError = errors[0];
                if (firstError.Exception != null && firstError.Exception.OffendingToken != null)
                {
                    txtScript.Select(firstError.Exception.OffendingToken.StartIndex, firstError.Exception.OffendingToken.StopIndex + 1 - firstError.Exception.OffendingToken.StartIndex);
                }
                else
                {
                    txtScript.TextArea.Caret.Offset = txtScript.Document.GetOffset(firstError.Line, firstError.Column);
                }
                txtScript.ScrollToLine(firstError.Line);
            }
        }

        private void HandleCompilerErrors(CompilerException ex, ScriptCompilerLogger logger)
        {
            logger.Error(ex);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(ex.Message);
            sb.AppendLine();
            sb.AppendLine($"Text: \"{ex.Text}\"");
            sb.AppendLine("Line: " + ex.Line);
            sb.AppendLine("Column: " + ex.Column);
            var _ = MetroMessageBox.Show("Compilation Failed", sb.ToString() , MetroMessageBox.MessageBoxButtons.Ok);
            if(txtScript.TextArea.Focus())
            {
                txtScript.TextArea.Caret.Offset = txtScript.Document.GetOffset(ex.Line, ex.Column);
                txtScript.ScrollToLine(ex.Line);
            }
        }

        #endregion
    }
}