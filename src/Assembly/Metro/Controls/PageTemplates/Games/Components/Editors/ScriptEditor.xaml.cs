using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using System.Xml.Linq;
using Assembly.Metro.Dialogs;
using ExtryzeDLL.Blam;
using ExtryzeDLL.Blam.Scripting;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Flexibility;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.Editors
{
    /// <summary>
    /// Interaction logic for ScriptEditor.xaml
    /// </summary>
    public partial class ScriptEditor : UserControl
    {
        private ICacheFile _cache;
        private string _scriptDefsFile;

        public ScriptEditor(ICacheFile cache, string scriptDefsFile)
        {
            _cache = cache;
            _scriptDefsFile = scriptDefsFile;
            InitializeComponent();

            Thread thrd = new Thread(new ThreadStart(DecompileScripts));
            thrd.SetApartmentState(ApartmentState.STA);
            thrd.Start();
        }

        private void DecompileScripts()
        {
            XDocument scriptDefs = XDocument.Load(_scriptDefsFile);
            XMLOpcodeLookup opcodes = new XMLOpcodeLookup(scriptDefs);
            BlamScriptGenerator generator = new BlamScriptGenerator(_cache.Scenario, opcodes);
            IndentedTextWriter code = new IndentedTextWriter(new StringWriter());

            DateTime startTime = DateTime.Now;

            generator.WriteComment("Decompiled with Assembly", code);
            generator.WriteComment("", code);
            generator.WriteComment("Source scenario: " + _cache.ScenarioName, code);
            generator.WriteComment("Start time: " + startTime.ToString(), code);
            generator.WriteComment("", code);
            generator.WriteComment("Remember that all script code is property of Bungie/343 Industries.", code);
            generator.WriteComment("You have no rights. Play nice.", code);
            code.WriteLine();

            generator.WriteComment("Globals", code);
            foreach (IGlobal global in _cache.Scenario.ScriptGlobals)
            {
                code.Write("(global {0} {1} ", opcodes.GetTypeInfo((ushort)global.Type).Name, global.Name);
                generator.WriteExpression(global.Value, code);
                code.WriteLine(")");
            }
            code.WriteLine();

            generator.WriteComment("Objects", code);
            foreach (IGlobalObject obj in _cache.Scenario.ScriptObjects)
            {
                code.WriteLine("(object {0} {1} {2})", opcodes.GetTagClassName(obj.Class), obj.Name, obj.PlacementIndex);
            }
            code.WriteLine();

            generator.WriteComment("Scripts", code);
            foreach (IScript script in _cache.Scenario.Scripts)
            {
                code.Write("(script {0} {1} ", opcodes.GetScriptTypeName((ushort)script.ExecutionType), opcodes.GetTypeInfo((ushort)script.ReturnType).Name);

                if (script.Parameters.Count > 0)
                {
                    code.Write("({0} (", script.Name);

                    bool firstParam = true;
                    foreach (IScriptParameter param in script.Parameters)
                    {
                        if (!firstParam)
                            code.Write(", ");
                        code.Write("{1} {0}", param.Name, opcodes.GetTypeInfo((ushort)param.Type).Name);
                        firstParam = false;
                    }

                    code.Write("))");
                }
                else
                {
                    code.Write(script.Name);
                }

                code.Indent++;
                code.WriteLine();
                generator.WriteExpression(script.RootExpression, code);
                code.Indent--;

                code.WriteLine();
                code.WriteLine(")");
                code.WriteLine();
            }

            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime.Subtract(startTime);
            generator.WriteComment("Decompilation finished in ~" + duration.TotalSeconds.ToString() + "s", code);

            Dispatcher.Invoke(new Action(delegate { txtScript.Text = code.InnerWriter.ToString(); }));
        }

        private void btnCompile_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            MetroMessageBox.Show("Script compilation. What a joke.");
        }
    }
}
