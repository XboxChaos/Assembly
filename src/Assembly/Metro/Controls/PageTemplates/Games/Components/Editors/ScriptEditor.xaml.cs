﻿using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Assembly.Helpers;
using Assembly.Metro.Dialogs;
using Blamite.Blam.Scripting;
using Blamite.Serialization;
using Blamite.IO;
using Microsoft.Win32;
using ICSharpCode.AvalonEdit.Search;
using Assembly.SyntaxHighlighting;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.Editors
{
	/// <summary>
	///     Interaction logic for ScriptEditor.xaml
	/// </summary>
	public partial class ScriptEditor : UserControl
	{
		private readonly EngineDescription _buildInfo;
		private readonly IScriptFile _scriptFile;
		private bool _showInfo;
		private Endian _endian;

		public ScriptEditor(EngineDescription buildInfo, IScriptFile scriptFile, IStreamManager streamManager, Endian endian)
		{
			_endian = endian;
			_buildInfo = buildInfo;
			_scriptFile = scriptFile;
			_showInfo = App.AssemblyStorage.AssemblySettings.ShowScriptInfo;
			InitializeComponent();

			var thrd = new Thread(DecompileScripts);
			thrd.SetApartmentState(ApartmentState.STA);
			thrd.Start(streamManager);

			SearchPanel srch = SearchPanel.Install(txtScript);

			var bconv = new System.Windows.Media.BrushConverter();
			var srchbrsh = (System.Windows.Media.Brush)bconv.ConvertFromString("#40F0F0F0");

			srch.MarkerBrush = srchbrsh;

			App.AssemblyStorage.AssemblySettings.PropertyChanged += Settings_SettingsChanged;
			SetHighlightColor();
            LoadSyntaxHighlighting();
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
			var generator = new BlamScriptGenerator(scripts, opcodes, _endian);
			var code = new IndentedTextWriter(new StringWriter());

			generator.WriteComment("Decompiled with Assembly", code);
			generator.WriteComment("", code);
			generator.WriteComment("Source file: " + _scriptFile.Name, code);
			generator.WriteComment("Start time: " + startTime, code);
			generator.WriteComment("", code);
			generator.WriteComment("Remember that all script code is property of Bungie/343 Industries.", code);
			generator.WriteComment("You have no rights. Play nice.", code);
			code.WriteLine();

			int counter = 0;

            //Show implicit expressions. Helpful for newbies who are learning.
            //Seperate to another AssemblySettings?
            if (_showInfo)
            {
                generator.WriteComment("NOTE: Implicit expressions are being displayed. \n", code);
                foreach (ScriptExpression exp in scripts.Expressions)
                {
                    counter++;
                    if (exp.LineNumber == 0)
                        exp.LineNumber++;
                }
            }

            counter = 0;

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
				code.Write("(global {0} {1} ", opcodes.GetTypeInfo((ushort) global.Type).Name, global.Name);
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
				if (_showInfo)
				{
					generator.WriteComment(string.Format("Index: {0}, Expression Index: {1}", counter.ToString(), script.RootExpressionIndex.Index.ToString()), code);
				}	

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

			Dispatcher.Invoke(new Action(delegate { 
                txtScript.Text = code.InnerWriter.ToString();
                LoadSyntaxHighlighting(); //Jeeze
            }));
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

		private void Settings_SettingsChanged(object sender, EventArgs e)
		{
			// Reset the highlight colors in case the theme changed
			SetHighlightColor();
            LoadSyntaxHighlighting();
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

        private void LoadSyntaxHighlighting()
        {
            string filename = "BlamScriptBlue.xshd";

            switch (App.AssemblyStorage.AssemblySettings.ApplicationAccent)
            {
                case Settings.Accents.Blue:
                    filename = "BlamScriptBlue.xshd";
                    break;
                case Settings.Accents.Green:
                    filename = "BlamScriptGreen.xshd";
                    break;
                case Settings.Accents.Orange:
                    filename = "BlamScriptOrange.xshd";
                    break;
                case Settings.Accents.Purple:
                    filename = "BlamScriptPurple.xshd";
                    break;
            }
            txtScript.SyntaxHighlighting = HighlightLoader.LoadEmbeddedDefinition(filename);
        }

        public void Dispose()
		{
			txtScript.Clear();
			App.AssemblyStorage.AssemblySettings.PropertyChanged -= Settings_SettingsChanged;
		}
	}
}