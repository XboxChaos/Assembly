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
using Microsoft.Win32;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;

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
            //_showInfo = App.AssemblyStorage.AssemblySettings.ShowScriptInfo;
            _showInfo = true;


            InitializeComponent();

            List<Task> tasks = new List<Task>();
            tasks.Add(Task.Run(() => { DecompileScripts(); }));
            //tasks.Add(Task.Run(() => { GenerateSeatMappings(); }));
            //tasks.Add(Task.Run(() => { DumpEngineGlobalsToXML(); }));
            // tasks.Add(Task.Run(() => { DumpSpecialGlobalsToXML(); }));

            //var thrd1 = new Thread(DecompileScripts);
            //thrd1.SetApartmentState(ApartmentState.STA);
            //thrd1.Start();

            SearchPanel srch = SearchPanel.Install(txtScript);

            var bconv = new System.Windows.Media.BrushConverter();
            var srchbrsh = (System.Windows.Media.Brush)bconv.ConvertFromString("#40F0F0F0");

            srch.MarkerBrush = srchbrsh;

            App.AssemblyStorage.AssemblySettings.PropertyChanged += Settings_SettingsChanged;
            SetHighlightColor();
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
                counter++;
            }

            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime.Subtract(startTime);
            generator.WriteComment("Decompilation finished in ~" + duration.TotalSeconds + "s", code);

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

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (txtScript.LineCount > 0)
            {
                StringBuilder sb = new StringBuilder();
                int counter = 1;
                string[] lines = txtScript.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    sb.AppendLine(lines[i]);
                    if (lines[i].Contains("(script"))
                    {
                        string[] words = lines[i].Split(' ');
                        sb.AppendLine($"\t(print \"Enter Script {words[3].TrimEnd('\r', '\n')}\")");
                        counter++;
                    }
                }
                txtScript.Text = sb.ToString();
                MetroMessageBox.Show("A print call has been added to the beginning of each script.");
            }
        }

        private async void btnCompile_Click(object sender, RoutedEventArgs e)
        {
            switch(_buildInfo.Name)
            {
                case "Halo: Reach":
                case "Halo: Reach MCC":
                case "Halo: Reach MCC Update 1":
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
                                }
                            });
                            _metaRefresh();
                            StatusUpdater.Update("Scripts saved");
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

                    progressBar.Value = 0;
                    break;


                default:
                    MetroMessageBox.Show("Not Implemented", $"Unsupported Game: {_buildInfo.Name}");
                    break;
            }
        }

        private async void btnDump_Click(object sender, RoutedEventArgs e)
        {
            Task task = Task.Run(() => AllExpressionsToXML());
            await task.ContinueWith(t =>
            {
                Dispatcher.Invoke(() => MetroMessageBox.Show("All Expressions were dumped."));
            });
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
                Dictionary<long, UnitSeatMapping> seats = LoadSeatMappings();
                 ScriptCompiler compiler = new ScriptCompiler(_cashefile, scrContext, _opcodes, seats, progress, logger);
                ParseTreeWalker.Default.Walk(compiler, tree);
                return compiler.Result();
            }
        }

        //remove later?
        private void btnExpressions_Click(object sender, RoutedEventArgs e)
        {
            ExpressionsToXML();
        }

        // remover later?
        private void ExpressionsToXML()
        {

            string searchString = SearchTermTextBox.Text;
            var info = _opcodes.GetTypeInfo(searchString);

            if (info == null)
            {
                MetroMessageBox.Show("Unable to retrieve value type information. Please check your spelling.");
                return;
            }

            ScriptTable scripts;
            using (IReader reader = ((IStreamManager)_streamManager).OpenRead())
            {
                scripts = _scriptFile.LoadScripts(reader);
            }

            string folder = "Dump";
            string fileName = _cashefile.InternalName + "_Expressions_" + searchString + ".xml";
            string path = Path.Combine(folder, fileName);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }


            var expressions = scripts.Expressions.ExpressionsAsReadonly;
            int occurences = 0;

            var settings = new XmlWriterSettings();
            settings.Indent = true;
            using (var writer = XmlWriter.Create(path, settings))
            {
                writer.WriteComment($"Map: \"{_cashefile.InternalName}\" ValueType: \"{searchString}\" Opcode: \"{info.Opcode}\"");
                writer.WriteStartElement("Expressions");

                for (int i = 0; i < expressions.Count; i++)
                {
                    var exp = expressions[i];
                    if (exp.Type == ScriptExpressionType.Expression && (exp.Opcode == info.Opcode || exp.ReturnType == info.Opcode))
                    {
                        // throw out function_names
                        if (exp.ReturnType != _opcodes.GetTypeInfo("function_name").Opcode)
                        {
                            var bytes = BitConverter.GetBytes(exp.Value);
                            ushort second16 = BitConverter.ToUInt16(bytes, 0);
                            ushort first16 = BitConverter.ToUInt16(bytes, 2);

                            writer.WriteStartElement("Expression");
                            writer.WriteAttributeString("Index", i.ToString());
                            writer.WriteAttributeString("Opcode", exp.Opcode.ToString());
                            writer.WriteAttributeString("ValueType", exp.ReturnType.ToString());
                            writer.WriteAttributeString("ExpType", exp.Type.ToString());
                            writer.WriteAttributeString("String", exp.StringValue);
                            writer.WriteAttributeString("Val32", exp.Value.ToString());
                            writer.WriteAttributeString("Val16_1", first16.ToString());
                            writer.WriteAttributeString("Val16_2", second16.ToString());
                            writer.WriteAttributeString("Val8_1", bytes[3].ToString());
                            writer.WriteAttributeString("Val8_2", bytes[2].ToString());
                            writer.WriteAttributeString("Val8_3", bytes[1].ToString());
                            writer.WriteAttributeString("Val8_4", bytes[0].ToString());
                            writer.WriteEndElement();

                            occurences++;
                        }
                    }
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
            }
            MetroMessageBox.Show($"{occurences} expressions matching the criteria were found.\nThe output was saved in \"{path}\".");
        }

        private void AllExpressionsToXML()
        {
            using (IReader reader = ((IStreamManager)_streamManager).OpenRead())
            {
                ScriptTable scripts = _scriptFile.LoadScripts(reader);
                string folder = "Dump";
                string fileName = _cashefile.InternalName + "_Expression_Table.xml";
                string path = Path.Combine(folder, fileName);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var expressions = scripts.Expressions.ExpressionsAsReadonly;

                var settings = new XmlWriterSettings();
                settings.Indent = true;
                using (var writer = XmlWriter.Create(path, settings))
                {
                    writer.WriteComment($"Map: \"{_cashefile.InternalName}\"");
                    writer.WriteStartElement("Expressions");

                    for (int i = 0; i < expressions.Count; i++)
                    {
                        var exp = expressions[i];

                        var bytes = BitConverter.GetBytes(exp.Value);
                        ushort second16 = BitConverter.ToUInt16(bytes, 0);
                        ushort first16 = BitConverter.ToUInt16(bytes, 2);

                        writer.WriteStartElement("Expression");
                        writer.WriteAttributeString("Index", i.ToString());
                        writer.WriteAttributeString("Opcode", exp.Opcode.ToString());
                        writer.WriteAttributeString("ValueType", exp.ReturnType.ToString());
                        writer.WriteAttributeString("ExpType", exp.Type.ToString());
                        if (exp.Next != null)
                        {
                            writer.WriteAttributeString("Next_Salt", exp.Next.Index.Salt.ToString());
                            writer.WriteAttributeString("Next_Index", exp.Next.Index.Index.ToString());
                        }
                        else
                        {
                            writer.WriteAttributeString("Next_Salt", short.MaxValue.ToString());
                            writer.WriteAttributeString("Next_Index", short.MaxValue.ToString());
                        }

                        writer.WriteAttributeString("String", exp.StringValue);
                        writer.WriteAttributeString("Value", exp.Value.ToString("X8"));
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Close();
                }
            }
        }

        private void DumpEngineGlobalsToXML()
        {
            ScriptTable scripts;
            using (IReader reader = ((IStreamManager)_streamManager).OpenRead())
            {
                scripts = _scriptFile.LoadScripts(reader);
            }

            string folder = "Dump";
            string fileName = _cashefile.InternalName + "_EngineGlobals.xml";
            string path = Path.Combine(folder, fileName);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }


            var expressions = scripts.Expressions.ExpressionsAsReadonly;
            int occurences = 0;

            var settings = new XmlWriterSettings();
            settings.Indent = true;
            using (var writer = XmlWriter.Create(path, settings))
            {
                writer.WriteStartElement("globals");

                for (int i = 0; i < expressions.Count; i++)
                {
                    var exp = expressions[i];
                    if (exp.Type == ScriptExpressionType.GlobalsReference)
                    {

                        var bytes = BitConverter.GetBytes(exp.Value);
                        ushort first16 = BitConverter.ToUInt16(bytes, 2);
                        ushort second16 = (ushort)(BitConverter.ToUInt16(bytes, 0) ^ 0x8000);

                        if (first16 == 0xFFFF)
                        {
                            int con = (int)exp.Value;

                            writer.WriteStartElement("global");
                            writer.WriteAttributeString("Index", i.ToString());
                            writer.WriteAttributeString("Opcode", exp.Opcode.ToString());
                            writer.WriteAttributeString("ValueType", exp.ReturnType.ToString());
                            writer.WriteAttributeString("ExpType", exp.Type.ToString());
                            writer.WriteAttributeString("String", exp.StringValue);
                            writer.WriteAttributeString("OpCode", second16.ToString("X"));
                            writer.WriteEndElement();

                            occurences++;
                        }
                    }
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
            }
            Dispatcher.Invoke(new Action(() => MetroMessageBox.Show($"{occurences} expressions matching the criteria were found.\nThe output was saved in \"{path}\".")));
        }

        private void DumpSpecialGlobalsToXML()
        {
            ScriptTable scripts;
            using (IReader reader = ((IStreamManager)_streamManager).OpenRead())
            {
                scripts = _scriptFile.LoadScripts(reader);
            }

            string folder = "Dump";
            string fileName = _cashefile.InternalName + "_SpecialGlobals.xml";
            string path = Path.Combine(folder, fileName);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }


            var expressions = scripts.Expressions.ExpressionsAsReadonly;
            int occurences = 0;

            var settings = new XmlWriterSettings();
            settings.Indent = true;
            using (var writer = XmlWriter.Create(path, settings))
            {
                writer.WriteStartElement("globals");

                for (int i = 0; i < expressions.Count; i++)
                {
                    var exp = expressions[i];
                    if (exp.Type == ScriptExpressionType.GlobalsReference && exp.Opcode != exp.ReturnType)
                    {

                        var bytes = BitConverter.GetBytes(exp.Value);
                        ushort first16 = BitConverter.ToUInt16(bytes, 2);
                        ushort second16 = BitConverter.ToUInt16(bytes, 0);

                        writer.WriteStartElement("global");
                        writer.WriteAttributeString("Index", i.ToString());
                        writer.WriteAttributeString("Opcode", exp.Opcode.ToString());
                        writer.WriteAttributeString("ValueType", exp.ReturnType.ToString());
                        writer.WriteAttributeString("ExpType", exp.Type.ToString());
                        writer.WriteAttributeString("String", exp.StringValue);
                        writer.WriteAttributeString("Value", exp.Value.ToString("X"));
                        writer.WriteEndElement();

                        occurences++;
                    }
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
            }
            Dispatcher.Invoke(new Action(() => MetroMessageBox.Show($"{occurences} expressions matching the criteria were found.\nThe output was saved in \"{path}\".")));
        }

        // remove later
        private void TagsToXML()
        {

            string folder = "Dump";
            string fileName = _cashefile.InternalName + "_Tags.xml";
            string path = Path.Combine(folder, fileName);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }


            var tags = _cashefile.Tags;

            var settings = new XmlWriterSettings();
            settings.Indent = true;
            using (var writer = XmlWriter.Create(path, settings))
            {
                writer.WriteComment($"Map: \"{_cashefile.InternalName}\"");
                writer.WriteStartElement("Tags");

                for (int i = 0; i < tags.Count; i++)
                {
                    var tag = tags[i];

                    writer.WriteStartElement("Tag");
                    writer.WriteAttributeString("Index", i.ToString());
                    writer.WriteAttributeString("DatumSalt", tag.Index.Salt.ToString());
                    writer.WriteAttributeString("DatumIndex", tag.Index.Index.ToString());
                    if (tag.Group == null)
                        writer.WriteAttributeString("Class", "");
                    else
                        writer.WriteAttributeString("Class", CharConstant.ToString(tag.Group.Magic));
                    if (tag.MetaLocation == null)
                        writer.WriteAttributeString("MetaLocation", "");
                    else
                        writer.WriteAttributeString("MetaLocation", tag.MetaLocation.AsPointer().ToString());
                    if (tag.Index.IsValid)
                        writer.WriteAttributeString("Name", _cashefile.FileNames.GetTagName(tag));
                    else
                        writer.WriteAttributeString("Name", "");
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
            }
        }
        // remove later
        private void SIDsToXML()
        {

            string folder = "Dump";
            string fileName = _cashefile.InternalName + "_StringIDs.xml";
            string path = Path.Combine(folder, fileName);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var settings = new XmlWriterSettings();
            settings.Indent = true;
            using (var writer = XmlWriter.Create(path, settings))
            {
                writer.WriteComment($"Map: \"{_cashefile.InternalName}\"");
                writer.WriteStartElement("String_IDs");

                foreach (string str in _cashefile.StringIDs)
                {

                    StringID id = _cashefile.StringIDs.FindStringID(str);

                    writer.WriteStartElement("String_ID");
                    writer.WriteAttributeString("String", str);
                    writer.WriteAttributeString("Set", id.GetNamespace(_cashefile.StringIDs.IDLayout).ToString());
                    writer.WriteAttributeString("Index", id.GetIndex(_cashefile.StringIDs.IDLayout).ToString());
                    writer.WriteAttributeString("Value", id.Value.ToString());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
            }
        }

        private Dictionary<long, UnitSeatMapping> LoadSeatMappings()
        {
            string folder = _buildInfo.SeatMappings;
            string filename = _casheName + "_Mappings.xml";
            string path = Path.Combine(folder, filename);

            XDocument doc = XDocument.Load(path);
            var mappings = doc.Element("UnitSeatMappings").Elements("Mapping");

            Dictionary<long, UnitSeatMapping> result = new Dictionary<long, UnitSeatMapping>();
            foreach (XElement mapping in mappings)
            {
                long index = XMLUtil.GetNumericAttribute(mapping, "Index");
                long count = XMLUtil.GetNumericAttribute(mapping, "Count");
                string name = XMLUtil.GetStringAttribute(mapping, "Name");

                if (result.ContainsKey(index))
                {
                    throw new Exception($"Duplicate unit seat mapping index: {index}");
                }

                UnitSeatMapping seat = new UnitSeatMapping((Int16)index, (Int16)count, name);
                result.Add(index, seat);
            }

            return result;
        }


        private void Settings_SettingsChanged(object sender, EventArgs e)
        {
            // Reset the highlight color in case the theme changed
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
    }
}