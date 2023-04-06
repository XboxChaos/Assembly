using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Assembly.Metro.Dialogs;
using Assembly.Windows;
using Assembly.Helpers;
using Assembly.SyntaxHighlighting;
using Blamite.Blam;
using Blamite.Blam.Scripting;
using Blamite.Blam.Scripting.Compiler;
using Blamite.IO;
using Blamite.Serialization;
using Blamite.Util;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Search;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Assembly.Helpers.CodeCompletion.Scripting;
using System.Threading;
using System.Windows.Media;
using System.ComponentModel;
using System.Text;

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
        private Endian _endian;
        private CompletionWindow _completionWindow = null;
        private CancellationTokenSource _searchToken;
        private Regex _scriptRegex;
        private Regex _globalsRegex;
        private IEnumerable<ICompletionData> _staticCompletionData = new ICompletionData[0];
        private IEnumerable<ICompletionData> _dynamicScriptCompletionData = new ICompletionData[0];
        private IEnumerable<ICompletionData> _dynamicGlobalCompletionData = new ICompletionData[0];
        private bool _loaded = false;
        private readonly bool _hasNewSyntax;
        private readonly Progress<int> _progress;

        public ScriptEditor(EngineDescription buildInfo, IScriptFile scriptFile, IStreamManager streamManager, ICacheFile casheFile, Endian endian)
        {
            _endian = endian;
            _buildInfo = buildInfo;
            _opcodes = _buildInfo.ScriptInfo;
            _scriptFile = scriptFile;
            _streamManager = streamManager;
            _cashefile = casheFile;

            // If a game contains hsdt tags, it uses a newer Blam Script syntax. Currently the compiler only supports the old syntax.
            _hasNewSyntax = _buildInfo.Layouts.HasLayout("hsdt");

            InitializeComponent();

            // Disable user input. Enable it again when all background tasks have been completed.
            txtScript.IsReadOnly = true;

            // Enable code completion only if the compiler supports this game.
            if(!_hasNewSyntax)
            {
                txtScript.TextArea.GotFocus += EditorGotFocus;
                txtScript.TextArea.LostFocus += EditorLostFocus;
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

            txtScript.SyntaxHighlighting = LoadSyntaxHighlighting();

            // With syntax highlighting and HTML formatting, copying text takes ages. Disable the HTML formatting for copied text.
            DataObject.AddSettingDataHandler(txtScript, onTextViewSettingDataHandler);

            _progress = new Progress<int>(i =>
            {
                progressBar.Value = i;
            });

            itemShowInformation.IsChecked = App.AssemblyStorage.AssemblySettings.ShowScriptInfo;
            itemDebugData.IsChecked = App.AssemblyStorage.AssemblySettings.OutputCompilerDebugData;

            // Enable compilation only for supported games.
            if(_buildInfo.Name.Contains("Reach") || _buildInfo.Name.Contains("Halo 3") || _buildInfo.Name.Contains("ODST") && _buildInfo.HeaderSize != 0x800)
            {
                compileButton.Visibility = Visibility.Visible;
                progressReporter.Visibility = Visibility.Visible;
            }
        }


        #region Event Handlers
        private async void EditorLoadedAsync(object sender, RoutedEventArgs e)
        {
            if (!_loaded)
            {
                if (!_hasNewSyntax)
                {
                    // Background tasks.      
                    List<Task> tasks = new List<Task>();

                    Task regexTask = Task.Run(() => CreateRegex());
                    Task<string> decompilationTask = Task.Run(() => DecompileScnrScripts());
                    Task<IEnumerable<ICompletionData>> completionTask = Task.Run(() => GenerateStaticCompletionData());
                    tasks.Add(regexTask);
                    tasks.Add(decompilationTask);
                    tasks.Add(completionTask);

                    await Task.WhenAll(tasks);
                    _staticCompletionData = await completionTask;
                    txtScript.Text = await decompilationTask;
                }
                else
                {
                    txtScript.Text = await Task.Run(() => DecompileHsdtScripts());
                }
                txtScript.IsReadOnly = false;
                _loaded = true;
            }
        }

        private void ExportSourceClick(object sender, RoutedEventArgs e)
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

        private void ExportExpressionsClick(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.FileName = $"{_cashefile.InternalName}_Expressions.xml";
            sfd.Title = "Save Script Expressions As";
            sfd.Filter = "XML Files|*.xml";
            if (!(bool)sfd.ShowDialog())
                return;

            ScriptTable scripts;
            using (IReader reader = ((IStreamManager)_streamManager).OpenRead())
            {
                scripts = _scriptFile.LoadScripts(reader);
                if (scripts == null)
                    return;
            }
            XMLUtil.WriteScriptExpressionsToXml(scripts.Expressions, sfd.FileName);
            MetroMessageBox.Show("Expressions Exported", "Expressions exported successfully.");
        }

        private void ImportClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "BlamScript Files|*.hsc|Text Files|*.txt|All Files|*.*";
            if (!(bool)ofd.ShowDialog())
                return;
            txtScript.Text = File.ReadAllText(ofd.FileName);
        }

        private async void CompileClick(object sender, RoutedEventArgs e)
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

                    // Measure the time it took to compile the scripts.
                    var stopWatch = Stopwatch.StartNew();

                    // Compile the scripts.
                    ScriptData compileData = await Task.Run(() => CompileScripts(hsc, _progress, logger, exceptionCollector));
                    stopWatch.Stop();
                    var timeSpan = stopWatch.Elapsed;
                    string compilationMessage = $"The scripts were successfully compiled in {Math.Round(timeSpan.TotalSeconds, 3)} seconds.";
                    logger.Information(compilationMessage);

                    // Show the message box.
                    var saveResult = MetroMessageBox.Show("Scripts Compiled", compilationMessage
                            + "\nWARNING: This compiler is not 100% accurate and could corrupt the map in rare cases. Making a backup before proceeding is advisable."
                            + "\n\nDo you want to save the changes to the file?", MetroMessageBox.MessageBoxButtons.YesNo);
                    if(saveResult == MetroMessageBox.MessageBoxResult.Yes)
                    {
                        //TODO: Move this to its own function.
                        await Task.Run(() =>
                        {
                            using (IStream stream = _streamManager.OpenReadWrite())
                            {
                                _scriptFile.SaveScripts(compileData, stream, _progress);
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
                    ResetProgressBar();
                }
            }
        }

        private void AdditionalInfoCheckChange(object sender, RoutedEventArgs e)
        {
            App.AssemblyStorage.AssemblySettings.ShowScriptInfo = itemShowInformation.IsChecked;
        }

        private void OutputDebugDataCheckChange(object sender, RoutedEventArgs e)
        {
            App.AssemblyStorage.AssemblySettings.OutputCompilerDebugData = itemDebugData.IsChecked;
        }

        private async void ReloadContextClick(object sender, RoutedEventArgs e)
        {
            IEnumerable<ICompletionData> completionData = await Task.Run(() => GenerateStaticCompletionData());
            _staticCompletionData = completionData;
            StatusUpdater.Update("Context Reloaded");
        }

        private async void EditorGotFocus(object sender, RoutedEventArgs e)
        {
            // Start the background search.
            _searchToken = new CancellationTokenSource();
            try
            {
                await BackgroundSearchAsync(TimeSpan.FromSeconds(10), _searchToken.Token);
            }
            catch(TaskCanceledException _)
            {

            }
            if(_searchToken != null)
            {
                _searchToken.Dispose();
                _searchToken = null;
            }
        }

        private void EditorLostFocus(object sender, RoutedEventArgs e)
        {
            // Stop the background search.
            if(_searchToken != null)
            {
                _searchToken.Cancel();
            }
        }

        private async void Settings_SettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "ShowScriptInfo")
            {
                if(_buildInfo.Layouts.HasLayout("hsdt"))
                {
                    txtScript.Text = await Task.Run(() => DecompileHsdtScripts());
                }
                else
                {
                    txtScript.Text = await Task.Run(() => DecompileScnrScripts() );
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

        static public void onTextViewSettingDataHandler(object sender, DataObjectSettingDataEventArgs e)
        {
            // Disable HTML formatting for copied text.
            var textView = sender as TextEditor;
            if (textView != null && e.Format == DataFormats.Html)
            {
                e.CancelCommand();
            }
        }

        #endregion



        #region Functions
        private string DecompileHsdtScripts()
        {
            using (IReader reader = _streamManager.OpenRead())
            {
                ScriptTable scripts = _scriptFile.LoadScripts(reader);
                if (scripts is null)
                {
                    return "";
                }

                OpcodeLookup opcodes = _buildInfo.ScriptInfo;
                var generator = new BlamScriptGenerator(scripts, opcodes, _buildInfo, _endian);
                return generator.Decompile(_scriptFile.Name, App.AssemblyStorage.AssemblySettings.ShowScriptInfo);
            }
        }

        private string DecompileScnrScripts()
        {
            using (IReader reader = _streamManager.OpenRead())
            {
                ScriptTable scripts = _scriptFile.LoadScripts(reader);
                if (scripts is null)
                {
                    return "";
                }

                OpcodeLookup opcodes = _buildInfo.ScriptInfo;
                var decompiler = new BlamScriptDecompiler(scripts, opcodes, _endian);
                return decompiler.DecompileAll(_scriptFile.Name, App.AssemblyStorage.AssemblySettings.ShowScriptInfo, true);
            }
        }

        private ScriptData CompileScripts(string code, IProgress<int> progress, ScriptCompilerLogger logger, ParsingExceptionCollector collector)
        {
            using (IReader reader = _streamManager.OpenRead())
            {
                // Set up the lexer.
                logger.Information("Running the lexer...");
                ICharStream stream = CharStreams.fromstring(code);
                HS_Gen1Lexer lexer = new HS_Gen1Lexer(stream);
                lexer.AddErrorListener(collector);
                ITokenStream tokens = new CommonTokenStream(lexer);

                // Set up the parser.
                logger.Information("Running the parser...");
                HS_Gen1Parser parser = new HS_Gen1Parser(tokens);
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

                // Load the context.
                var context = _scriptFile.LoadContext(reader, _cashefile);

                // Run the compiler.
                logger.Information("Running the compiler...");
                bool outputDebugData = App.AssemblyStorage.AssemblySettings.OutputCompilerDebugData;
                ScriptCompiler compiler = new ScriptCompiler(_cashefile, _buildInfo, _opcodes, context, progress, logger, outputDebugData);
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

        private IHighlightingDefinition LoadSyntaxHighlighting()
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
            using (var reader = _streamManager.OpenRead())
            {
                List<ICompletionData> result = new List<ICompletionData>();

                // Load the context
                var context = _scriptFile.LoadContext(reader, _cashefile);

                // Functions.
                result.AddRange(_opcodes.GetAllImplementedFunctions().Select(info => new FunctionCompletion(info)));

                // Engine globals.
                result.AddRange(_opcodes.GetAllImplementedGlobals().Select(info => new GlobalCompletion(info, GlobalType.Engine)));

                // Objects.
                IEnumerable<ObjectCompletion> objectCompletion = context.GetAllContextObjects().Select(obj => new ObjectCompletion(obj));
                IEnumerable<ObjectCompletion> uniqueObjectCompletion = objectCompletion.GroupBy(obj => new { obj.Text, obj.Description }).Select(obj => obj.First());
                result.AddRange(uniqueObjectCompletion);

                // Unit seat mappings.
                result.AddRange(context.GetAllUnitSeatMappings().Select(mapping => new ObjectCompletion(mapping)));

                return result;
            }
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
            while (true)
            {
                await Task.WhenAll(FindGlobalNamesAsync(txtScript.Text), FindScriptNamesAsync(txtScript.Text));
                await Task.Delay(interval, cancellationToken);
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

        private void ResetProgressBar()
        {
            progressBar.Value = 0;
        }

        #endregion
    }
}