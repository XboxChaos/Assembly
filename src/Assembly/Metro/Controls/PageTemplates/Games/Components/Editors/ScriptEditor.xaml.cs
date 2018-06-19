using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Assembly.Metro.Dialogs;
using Blamite.Blam.Scripting;
using Blamite.Serialization;
using Blamite.IO;
using Microsoft.Win32;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.Editors
{
	/// <summary>
	///     Interaction logic for ScriptEditor.xaml
	/// </summary>
	public partial class ScriptEditor : UserControl
	{
		private readonly EngineDescription _buildInfo;
		private readonly IScriptFile _scriptFile;

		public ScriptEditor(EngineDescription buildInfo, IScriptFile scriptFile, IStreamManager streamManager)
		{
			_buildInfo = buildInfo;
			_scriptFile = scriptFile;
			InitializeComponent();

			var thrd = new Thread(DecompileScripts);
			thrd.SetApartmentState(ApartmentState.STA);
			thrd.Start(streamManager);
		}

		private void DecompileScripts(object streamManager)
		{
			DateTime startTime = DateTime.Now;

			ScriptTable scripts;
			using (IReader reader = ((IStreamManager) streamManager).OpenRead())
			{
				scripts = _scriptFile.LoadScripts(reader);
				if (scripts == null)
					return;
			}

			OpcodeLookup opcodes = _buildInfo.ScriptInfo;
			var generator = new BlamScriptGenerator(scripts, opcodes);
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
				code.Write("(global {0} {1} ", opcodes.GetTypeInfo((ushort) global.Type).Name, global.Name);
				generator.WriteExpression(global.ExpressionIndex, code);
				code.WriteLine(")");
			}
			code.WriteLine();

			generator.WriteComment("Scripts", code);
			foreach (Script script in scripts.Scripts)
			{
				code.Write("(script {0} {1} ", opcodes.GetScriptTypeName((ushort) script.ExecutionType),
					opcodes.GetTypeInfo((ushort) script.ReturnType).Name);

				if (script.Parameters.Count > 0)
				{
					code.Write("({0} (", script.Name);

					bool firstParam = true;
					foreach (ScriptParameter param in script.Parameters)
					{
						if (!firstParam)
							code.Write(", ");
						code.Write("{1} {0}", param.Name, opcodes.GetTypeInfo((ushort) param.Type).Name);
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
			sfd.Title = "Save Script As";
			sfd.Filter = "BlamScript Files|*.hsc|Text Files|*.txt|All Files|*.*";
			if (!(bool) sfd.ShowDialog())
				return;

			File.WriteAllText(sfd.FileName, txtScript.Text);
			MetroMessageBox.Show("Script Exported", "Script exported successfully.");
		}

		private void btnCompile_Click(object sender, RoutedEventArgs e)
		{
			MetroMessageBox.Show("go away", "gameleak gtfo");
			/*LispScriptScanner scanner = new LispScriptScanner();
            scanner.SetSource(txtScript.Text, 0);
            LispScriptParser parser = new LispScriptParser(scanner);
            parser.Parse();*/
			// You have to stick a breakpoint here to look at the parsed nodes :P
		}

		public void Dispose()
		{
			txtScript.Clear();
		}
	}
}