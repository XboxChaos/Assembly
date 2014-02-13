using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Windows.Forms;
using Atlas.Dialogs;
using Atlas.Models;
using Blamite.Blam.Scripting;

namespace Atlas.ViewModels.Cache
{
	public class ScriptEditorViewModel : Base
	{
		public ScriptEditorViewModel(CachePageViewModel cachePageViewModel, IScriptFile scriptFile)
		{
			CachePageViewModel = cachePageViewModel;
			ScriptFile = scriptFile;

			DecompileScript();
		}

		private void DecompileScript()
		{
			var startTime = DateTime.Now;

			ScriptTable scripts;
			using (var reader = (CachePageViewModel.MapStreamManager).OpenRead())
			{
				scripts = _scriptFile.LoadScripts(reader);
				if (scripts == null) return;
			}

			var opcodes = CachePageViewModel.EngineDescription.ScriptInfo;
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
			foreach (var global in scripts.Globals)
			{
				code.Write("(global {0} {1} ", opcodes.GetTypeInfo((ushort)global.Type).Name, global.Name);
				generator.WriteExpression(global.ExpressionIndex, code);
				code.WriteLine(")");
			}
			code.WriteLine();

			generator.WriteComment("Scripts", code);
			foreach (var script in scripts.Scripts)
			{
				code.Write("(script {0} {1} ", opcodes.GetScriptTypeName((ushort)script.ExecutionType),
					opcodes.GetTypeInfo((ushort)script.ReturnType).Name);

				if (script.Parameters.Count > 0)
				{
					code.Write("({0} (", script.Name);

					var firstParam = true;
					foreach (var param in script.Parameters)
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
				generator.WriteExpression(script.RootExpressionIndex, code);
				code.Indent--;

				code.WriteLine();
				code.WriteLine(")");
				code.WriteLine();
			}

			var endTime = DateTime.Now;
			var duration = endTime.Subtract(startTime);
			generator.WriteComment("Decompilation finished in ~" + duration.TotalSeconds + "s", code);

			ScriptText = code.InnerWriter.ToString();
		}

		public void ExportScript()
		{
			var sfd = new SaveFileDialog
			{
				Title = "Save Script As",
				Filter = "BlamScript Files|*.hsc|Text Files|*.txt|All Files|*.*"
			};

			if (sfd.ShowDialog() == DialogResult.OK) return;
			File.WriteAllText(sfd.FileName, ScriptText);
			MetroMessageBox.Show("Script Exported", "Script exported successfully.");
		}

		public CachePageViewModel CachePageViewModel
		{
			get { return _cachePageViewModel; }
			set { SetField(ref _cachePageViewModel, value); }
		}
		private CachePageViewModel _cachePageViewModel;

		public IScriptFile ScriptFile
		{
			get { return _scriptFile; }
			set { SetField(ref _scriptFile, value); }
		}
		private IScriptFile _scriptFile;

		public string ScriptText
		{
			get { return _scriptText; }
			set { SetField(ref _scriptText, value); }
		}
		private string _scriptText;
	}
}
