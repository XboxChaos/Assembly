using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using Assembly.Metro.Dialogs;
using Blamite.Blam.Scripting;
using Blamite.Blam.Scripting.Compiler;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Blam;
using Microsoft.Win32;
using System.Xml;
using System.Xml.Linq;
using Blamite.Util;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

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

        public ScriptEditor(EngineDescription buildInfo, IScriptFile scriptFile, IStreamManager streamManager, ICacheFile casheFile, string casheName)
		{
			_buildInfo = buildInfo;
            _opcodes = _buildInfo.ScriptInfo;
            _scriptFile = scriptFile;
            _streamManager = streamManager;
            _cashefile = casheFile;
            _casheName = casheName;

			InitializeComponent();

			var thrd1 = new Thread(DecompileScripts);
            var thrd2 = new Thread(GenerateSeatMappings);
			thrd1.SetApartmentState(ApartmentState.STA);
            thrd2.SetApartmentState(ApartmentState.STA);
            thrd1.Start();
            thrd2.Start();
        }

		private void DecompileScripts()
		{
			DateTime startTime = DateTime.Now;

			ScriptTable scripts;
			using (IReader reader = ((IStreamManager) _streamManager).OpenRead())
			{
				scripts = _scriptFile.LoadScripts(reader);
				if (scripts == null)
					return;
			}

			var generator = new BlamScriptGenerator(scripts, _opcodes);
			var code = new IndentedTextWriter(new StringWriter());

			generator.WriteComment("Decompiled with Assembly", code);
			generator.WriteComment("", code);
			generator.WriteComment("Source file: " + _scriptFile.Name, code);
			generator.WriteComment("Start time: " + startTime, code);
			generator.WriteComment("", code);
			generator.WriteComment("Remember that all script code is property of Bungie/343 Industries.", code);
			generator.WriteComment("You have no rights. Play nice.", code);
			code.WriteLine();

			generator.WriteComment("Globals", code);
			foreach (ScriptGlobal global in scripts.Globals)
			{
				code.Write("(global {0} {1} ", _opcodes.GetTypeInfo((ushort) global.Type).Name, global.Name);
				generator.WriteExpression(global.ExpressionIndex, code);
				code.WriteLine(")");
			}
			code.WriteLine();

			generator.WriteComment("Scripts", code);
			foreach (Script script in scripts.Scripts)
			{
				code.Write("(script {0} {1} {2}", _opcodes.GetScriptTypeName((ushort) script.ExecutionType),
					_opcodes.GetTypeInfo((ushort) script.ReturnType).Name, script.Name);

				if (script.Parameters.Count > 0)
				{
					code.Write(" (");

					bool firstParam = true;
					foreach (ScriptParameter param in script.Parameters)
					{
						if (!firstParam)
							code.Write(", ");
						code.Write("{1} {0}", param.Name, _opcodes.GetTypeInfo((ushort) param.Type).Name);
						firstParam = false;
					}

					code.Write(")");
				}

				code.Indent++;
				code.WriteLine();
				generator.WriteExpression(script.RootExpressionIndex, code);
				code.Indent--;

				code.WriteLine();
				code.WriteLine(")");
				code.WriteLine();
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
			if (!(bool) sfd.ShowDialog())
				return;

			File.WriteAllText(sfd.FileName, txtScript.Text);
			MetroMessageBox.Show("Script Exported", "Script exported successfully.");
		}

		private void btnCompile_Click(object sender, RoutedEventArgs e)
		{

            if (_buildInfo.Name == "Halo: Reach")
            {
                string hsc = txtScript.Text;
                var thrd = new Thread(()=>CompileScripts(hsc));
                thrd.SetApartmentState(ApartmentState.STA);
                thrd.Start();
                MetroMessageBox.Show("Success!", "The scripts were compiled successfully!");
            }
            else
                MetroMessageBox.Show("Not Implemented", $"Unsupported Game: {_buildInfo.Name}");

        }

        //remove later?
        private void btnExpressions_Click(object sender, RoutedEventArgs e)
        {
            ExpressionsToXML();
        }

        private void CompileScripts(string code)
        {
            ICharStream stream = CharStreams.fromstring(code);
            ITokenSource lexer = new BS_ReachLexer(stream);
            ITokenStream tokens = new CommonTokenStream(lexer);
            BS_ReachParser parser = new BS_ReachParser(tokens);
            parser.BuildParseTree = true;
            IParseTree tree = parser.hsc();
            ScriptCompiler compiler = new ScriptCompiler(_cashefile, _scriptFile.LoadContext(_streamManager.OpenRead()), _opcodes, LoadSeatMappings());
            ParseTreeWalker.Default.Walk(compiler, tree);
        }

        // Remove Later
        private void WriteClasses()
        {
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            var fileName = "Reach_classes.xml";


            using (var writer = XmlWriter.Create(fileName, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Classes");

                foreach (var cla in _cashefile.TagClasses)
                {
                    writer.WriteStartElement("Tagclass");
                    writer.WriteAttributeString("Magic", CharConstant.ToString(cla.Magic));
                    writer.WriteAttributeString("Parent", CharConstant.ToString(cla.ParentMagic));
                    writer.WriteAttributeString("Grandparent", CharConstant.ToString(cla.GrandparentMagic));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
            }
        }
        
        // remover later?
        private void ExpressionsToXML()
        {

            string searchString = SearchTermTextBox.Text;
            var info = _opcodes.GetTypeInfo(searchString);

            if(info == null)
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
            string fileName =_cashefile.InternalName + "_Expressions_" + searchString + ".xml";
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

                for(int i = 0; i < expressions.Count; i++)
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
                    if(tag.Class == null)
                        writer.WriteAttributeString("Class", "");
                    else
                        writer.WriteAttributeString("Class", CharConstant.ToString(tag.Class.Magic));
                    if(tag.MetaLocation == null)
                        writer.WriteAttributeString("MetaLocation", "");
                    else
                        writer.WriteAttributeString("MetaLocation", tag.MetaLocation.AsPointer().ToString());
                    if(tag.Index.IsValid)
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

                foreach(string str in _cashefile.StringIDs)
                {

                    StringID id = _cashefile.StringIDs.FindStringID(str);                  

                    writer.WriteStartElement("String_ID");
                    writer.WriteAttributeString("String", str);
                    writer.WriteAttributeString("Set", id.GetSet(_cashefile.StringIDs.IDLayout).ToString());
                    writer.WriteAttributeString("Index", id.GetIndex(_cashefile.StringIDs.IDLayout).ToString());
                    writer.WriteAttributeString("Value", id.Value.ToString());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
            }
        }

        private void GenerateSeatMappings()
        {
            string folder = _buildInfo.SeatMappings;
            string filename = _casheName + "_Mappings.xml";
            string path = Path.Combine(folder, filename);

            if (!File.Exists(path))
            {
                ScriptTable scripts;
                using (IReader reader = ((IStreamManager)_streamManager).OpenRead())
                {
                    scripts = _scriptFile.LoadScripts(reader);
                }

                var expressions = scripts.Expressions.ExpressionsAsReadonly;
                ushort opc = _opcodes.GetTypeInfo("unit_seat_mapping").Opcode;
                SortedDictionary<uint, ScriptExpression> uniqueMappings = new SortedDictionary<uint, ScriptExpression>();
                
                // find all unique mappings
                foreach(var exp in expressions)
                {
                    if(exp.Opcode == opc && exp.ReturnType == opc && exp.Type == ScriptExpressionType.Expression && exp.Value != 0xFFFFFFFF)
                    {
                        uint index = exp.Value & 0xFFFF;
                        if (!uniqueMappings.ContainsKey(index))
                        {
                            uniqueMappings.Add(index, exp);
                        }
                    }
                }

                if(uniqueMappings.Count > 0)
                {
                    var settings = new XmlWriterSettings();
                    settings.Indent = true;
                    using (var writer = XmlWriter.Create(path, settings))
                    {
                        writer.WriteComment($"Map Name: '{_casheName}'    Internal Name: '{_cashefile.InternalName}'");
                        writer.WriteStartElement("UnitSeatMappings");
                        foreach(var mapping in uniqueMappings)
                        {
                            string name = mapping.Value.StringValue;
                            uint count = (mapping.Value.Value & 0xFFFF0000) >> 16;
                            writer.WriteStartElement("Mapping");
                            writer.WriteAttributeString("Index", mapping.Key.ToString());
                            writer.WriteAttributeString("Name", name);
                            writer.WriteAttributeString("Count", count.ToString());
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                        writer.Close();
                    }
                }
            }
        }

        private Dictionary<int, UnitSeatMapping> LoadSeatMappings()
        {
            string folder = _buildInfo.SeatMappings;
            string filename = _casheName + "_Mappings.xml";
            string path = Path.Combine(folder, filename);

            XDocument doc = XDocument.Load(path);
            var mappings = doc.Element("UnitSeatMappings").Elements("Mapping");

            Dictionary<int, UnitSeatMapping> result = new Dictionary<int, UnitSeatMapping>();
            foreach (XElement mapping in mappings)
            {
                int index = XMLUtil.GetNumericAttribute(mapping, "Index");
                int count = XMLUtil.GetNumericAttribute(mapping, "Count");
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
    }
}