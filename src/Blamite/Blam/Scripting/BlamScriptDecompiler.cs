using Blamite.IO;
using System.IO;
using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace Blamite.Blam.Scripting
{
	/// <summary>
	/// A decompiler for blam scripts, contained in cashe files.
	/// </summary>
	public class BlamScriptDecompiler
	{
		private readonly OpcodeLookup _opcodes;
		private readonly ScriptTable _scripts;
		private readonly Endian _endian;
		private Stack<int> _condIndentStack = new Stack<int>();

		private enum BranchType
		{
			Call,
			Multiline, // begin, or, and
			CompilerBegin,
			InitialBegin,
			If,
			Cond
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BlamScriptDecompiler"/> class.
		/// </summary>
		/// <param name="scripts">The scripting data of the script file.</param>
		/// <param name="opcodes">A lookup containing script type information.</param>
		/// <param name="endian">The endianness of the parent cache file.</param>
		public BlamScriptDecompiler(ScriptTable scripts, OpcodeLookup opcodes, Endian endian)
		{
			_scripts = scripts;
			_opcodes = opcodes;
			_endian = endian;
		}

		/// <summary>
		/// Decompiles the Scripts and Globals of a script file.
		/// </summary>
		/// <param name="sourceFile">The name of the source file containing the scripts.</param>
		/// <param name="showInfo">If set to true, writes additional information to the decompiled text.</param>
		/// <param name="writeHeader">If set to true, writes a header to the decompiled text.</param>
		/// <returns>Returns the decompiled Globals and Scripts.</returns>
		public string DecompileAll(string sourceFile, bool showInfo, bool writeHeader)
		{
			IndentedTextWriter output = new IndentedTextWriter(new StringWriter(CultureInfo.InvariantCulture));
			DateTime startTime = DateTime.Now;
			Stopwatch watch = Stopwatch.StartNew();
			if(WriteGlobals(output, showInfo))
            {
				WriteEmptyLines(output, 2);
			}
			WriteScripts(output, showInfo);
			watch.Stop();

			string text = "";

			if (writeHeader)
			{
				text = GenerateHeader(sourceFile, startTime, watch.Elapsed) + "\n";
			}

			text += output.InnerWriter.ToString();
			return text;
		}

		/// <summary>
		/// Decompiles the Globals of a script file.
		/// </summary>
		/// <param name="sourceFile">The name of the source file containing the scripts.</param>
		/// <param name="showInfo">If set to true, writes additional information to the decompiled text.</param>
		/// <param name="writeHeader">If set to true, writes a header to the decompiled text.</param>
		/// <returns>Returns the decompiled Globals.</returns>
		public string DecompileGlobals(string sourceFile, bool showInfo, bool writeHeader)
		{
			IndentedTextWriter output = new IndentedTextWriter(new StringWriter(CultureInfo.InvariantCulture));
			DateTime startTime = DateTime.Now;
			var watch = Stopwatch.StartNew();
			WriteGlobals(output, showInfo);
			watch.Stop();
			string text = "";

			if(writeHeader)
            {
				text = GenerateHeader(sourceFile, startTime, watch.Elapsed) + "\n";
            }

			text += output.InnerWriter.ToString();
			return text;
		}

		/// <summary>
		/// Decompiles the Scripts of a script file.
		/// </summary>
		/// <param name="sourceFile">The name of the source file containing the scripts.</param>
		/// <param name="showInfo">If set to true, writes additional information to the decompiled text.</param>
		/// <param name="writeHeader">If set to true, writes a header to the decompiled text.</param>
		/// <returns>Returns the decompiled Scripts.</returns>
		public string DecompileScripts(string sourceFile, bool showInfo, bool writeHeader)
		{
			IndentedTextWriter output = new IndentedTextWriter(new StringWriter(CultureInfo.InvariantCulture));
			DateTime startTime = DateTime.Now;
			var watch = Stopwatch.StartNew();
			WriteScripts(output, showInfo);
			watch.Stop();
			string text = "";

			if (writeHeader)
			{
				text = GenerateHeader(sourceFile, startTime, watch.Elapsed) + "\n";
			}

			text += output.InnerWriter.ToString();
			return text;
		}


		/// <summary>
		/// Writes the globals to the output.
		/// </summary>
		/// <param name="output">The output.</param>
		/// <param name="showInfo">If set to true, writes additional information to the decompiled text.</param>
		/// <returns>Returns true if globals were written to the output.</returns>
		private bool WriteGlobals(IndentedTextWriter output, bool showInfo)
        {
			if (_scripts.Globals is null || _scripts.Globals.Count == 0)
			{
				return false;
			}

			// write Header.
			WriteComment(output, "GLOBALS");
			output.WriteLine();

			for (int i = 0; i < _scripts.Globals.Count; i++)
			{
				var glo = _scripts.Globals[i];

				// write global declaration.
				output.Write("(global {0} {1} ", _opcodes.GetTypeInfo((ushort)glo.Type).Name, glo.Name);

				// write value.
				FollowRootIndex(output, glo.ExpressionIndex, false, BranchType.Call);

				// write additional Information.
				if (showInfo)
				{
					output.WriteLine(")\t\t; Index: {0}, Expression Index: {1}", i, glo.ExpressionIndex.Index.ToString());
				}
				else
				{
					output.WriteLine(")");
				}
			}

			return true;
		}

		/// <summary>
		/// Writes scripts to the output.
		/// </summary>
		/// <param name="output">The outout.</param>
		/// <param name="showInfo">If set to true, writes additional information to the decompiled text.</param>
		/// <returns>Returns true if scripts were written to the output.</returns>
		private bool WriteScripts(IndentedTextWriter output, bool showInfo)
        {
			// return if the map doesn't contain scripts.
			if (_scripts.Scripts is null || _scripts.Scripts.Count == 0)
			{
				return false;
			}
			WriteComment(output, "SCRIPTS");
			output.WriteLine();

			for (int i = 0; i < _scripts.Scripts.Count; i++)
			{
				Script scr = _scripts.Scripts[i];

				// Filter out branch scripts which were generated by the compiler.
				var split = scr.Name.Split(new string[] { "_to_" }, StringSplitOptions.RemoveEmptyEntries);
				if (split.Length == 2)
				{
					if (_scripts.Scripts.Exists(s => s.Name == split[0]) && _scripts.Scripts.Exists(s => s.Name == split[split.Length - 1]))
						continue;
				}
				else if (split.Length == 3)
				{
					string firstTwo = string.Join("_to_", split, 0, 2);
					string lastTwo = string.Join("_to_", split, 1, 2);
					if ((_scripts.Scripts.Exists(s => s.Name == firstTwo) && _scripts.Scripts.Exists(s => s.Name == split[2]))
						|| (_scripts.Scripts.Exists(s => s.Name == lastTwo) && _scripts.Scripts.Exists(s => s.Name == split[0])))
						continue;
				}
				else if (split.Length == 4)
				{
					string firstHalf = string.Join("_to_", split, 0, 2);
					string secondHalf = string.Join("_to_", split, 2, 2);
					if (_scripts.Scripts.Exists(s => s.Name == firstHalf) && _scripts.Scripts.Exists(s => s.Name == secondHalf))
						continue;
				}

				// Write additional information.
				if (showInfo)
				{
					WriteComment(output, $"Index: {i}, Expression Index: {scr.RootExpressionIndex.Index.ToString()}");
				}

				// Write script declaration.
				output.Write("(script {0} {1} {2}", _opcodes.GetScriptTypeName((ushort)scr.ExecutionType),
					_opcodes.GetTypeInfo((ushort)scr.ReturnType).Name, scr.Name);

				// Write script parameter declarations.
				if (scr.Parameters?.Count > 0)
				{
					output.Write(" (");
					bool firstParam = true;
					foreach (ScriptParameter param in scr.Parameters)
					{
						if (!firstParam)
							output.Write(", ");
						output.Write("{1} {0}", param.Name, _opcodes.GetTypeInfo((ushort)param.Type).Name);
						firstParam = false;
					}
					output.WriteLine(")");
				}
				else
				{
					output.WriteLine();
				}

				output.Indent++;

				// Write code.
				FollowRootIndex(output, scr.RootExpressionIndex, true, BranchType.InitialBegin);

				// Close parenthesis
				output.Indent = 0;
				output.WriteLine(")");
				output.WriteLine();
			}

			return true;
		}

		/// <summary>
		/// Generates a header containing general infomation about the decompilation process.
		/// </summary>
		/// <param name="sourceFile">The name of the source file.</param>
		/// <param name="startTime">The time when the decompilation process was started.</param>
		/// <param name="duration">The duration of the decompilation process.</param>
		/// <returns>A header containing general information about the decompilation process.</returns>
		private string GenerateHeader(string sourceFile, DateTime startTime, TimeSpan duration)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("; Decompiled with Blamite");
			sb.AppendLine("; Source file: " + sourceFile);
			sb.AppendLine("; Start time: " + startTime);
			sb.AppendLine("; Decompilation finished in ~" + duration.TotalSeconds + "s");
			sb.AppendLine("; Remember that all script code is property of Bungie/343 Industries.");
			sb.AppendLine("; You have no rights. Play nice.\n");
			return sb.ToString();
		}

		/// <summary>
		/// Writes a comment to the output
		/// </summary>
		/// <param name="output">The output.</param>
		/// <param name="comment">The comment to write.</param>
		private void WriteComment(IndentedTextWriter output, string comment)
		{
			output.WriteLine("; {0}", comment);
		}

		/// <summary>
		/// Writes empty lines to the output.
		/// </summary>
		/// <param name="output">The output.</param>
		/// <param name="count">The number of empty lines to write.</param>
		private void WriteEmptyLines(IndentedTextWriter output, int count)
        {
			for(int i = 0; i < count; i++)
            {
				output.WriteLine();
			}
        }

		/// <summary>
		/// The decompiler follows a branch based on a datum index and generates code. Also handles most of the text formatting.
		/// </summary>
		/// <param name="output">The output.</param>
		/// <param name="root">The datumn index to follow.</param>
		/// <param name="newLine">Indicates whether the expressions of the branch start on a new line.</param>
		/// <param name="type">The type of the branch.</param>
		private void FollowRootIndex(IndentedTextWriter output, DatumIndex root, bool newLine, BranchType type)
		{
			ScriptExpression exp = _scripts.Expressions.FindExpression(root);
			int index = 0;

			// iterate through the branch.
			while(exp != null)
			{
				int startIndent = output.Indent;
				bool endOfExpression = exp.NextExpression is null;
				bool endOfInlineMultiline = false;

				// Remove the last expression in cond calls, which were added by the compiler.
				if(type == BranchType.Cond && endOfExpression && exp.Type == ScriptExpressionType.Expression && exp.LineNumber == 0)
				{
					return;
				}
				// if: indent the condition if it is a multiline expression.
				else if (type == BranchType.If && index == 1 && IsMultilineExpression(exp))
				{
					output.WriteLine();
					output.Indent++;
				}
				// if: indent after the condition.
				else if(type == BranchType.If && index == 2)
				{
					output.WriteLine();
					output.Indent++;
				}
				// begin, and, or (multiline): indent after the function name.
				else if(type == BranchType.Multiline && index == 1)
				{
					output.WriteLine();
					output.Indent++;
				}
				// cond: indent after the condition.
				else if (type == BranchType.Cond && index == 2)
				{
					output.WriteLine();
					output.Indent++;
				}
				// make begin, or and if calls always start on a new line.
				else if (type == BranchType.Call && IsMultilineExpression(exp))
				{
					output.WriteLine();
					output.Indent++;
					endOfInlineMultiline = true;
				}

				// write code.
				HandleExpression(output, exp, newLine);

				// insert space between the parameters of calls and script references.
				if ((type == BranchType.Call || type == BranchType.If) && !endOfExpression && !newLine)
				{
					output.Write(" ");
				}

				// handle the line break after inline multiline expressions and reset the indent.
				if(endOfInlineMultiline && !endOfExpression)
				{
					output.WriteLine();
					output.Indent = startIndent;
				}
				// handle the line break after if statements which end on a multiline expression.
				else if(type == BranchType.If && IsMultilineExpression(exp) && endOfExpression)
				{
					output.WriteLine();
				}
				// If a regular call ends on a multiline call, insert a line break and reset the indent. Mostly applies to sleep_until in combination with or.
				else if(type == BranchType.Call && IsMultilineExpression(exp) && endOfExpression)
                {
					output.WriteLine();
					output.Indent = startIndent;
				}


				index++;
				exp = exp.NextExpression;
			}
		}

		/// <summary>
		/// Decides how an expression will be handled and which actions to perform, based on the expression type.
		/// </summary>
		/// <param name="output">The output.</param>
		/// <param name="expression">The expression, which is being handled.</param>
		/// <param name="newLine">Indicates whether the expressions of the branch start on a new line.</param>
		private void HandleExpression(IndentedTextWriter output, ScriptExpression expression, bool newLine)
		{
			ushort ifOp = _opcodes.GetFunctionInfo("if")[0].Opcode;
			ushort funcNameOp = _opcodes.GetTypeInfo("function_name").Opcode;
			short clippedtype = (short)((short)expression.Type & 0xFF);

			bool _ = (ScriptExpressionType)clippedtype switch
			{
				ScriptExpressionType.Group when expression.Opcode == ifOp && expression.LineNumber == 0 => GenerateCond(output, expression, newLine),
				ScriptExpressionType.Group => GenerateGroup(output, expression, newLine),

				ScriptExpressionType.Expression when expression.ReturnType == funcNameOp => true,
				ScriptExpressionType.Expression => GenerateExpression(output, expression, newLine),

				ScriptExpressionType.ScriptReference => GenerateScriptReference(output, expression, newLine),
				ScriptExpressionType.GlobalsReference => GenerateGlobalsReference(output, expression, newLine),
				ScriptExpressionType.ParameterReference => GenerateParameterReference(output, expression, newLine),

				_ => throw new NotImplementedException($"The Decompiler encountered an unknown Expression Type: \"{(ScriptExpressionType)clippedtype}\".")
			};
		}

		/// <summary>
		/// The decompiler generates a function group based on an expression.
		/// </summary>
		/// <param name="output">The output.</param>
		/// <param name="expression">The group expression.</param>
		/// <param name="newLine">Indicates whether the expressions of the branch start on a new line.</param>
		/// <returns>Returns true if code was generated.</returns>
		private bool GenerateGroup(IndentedTextWriter output, ScriptExpression expression, bool newLine)
		{
			DatumIndex nameIndex = new DatumIndex(expression.Value.UintValue);
			FunctionInfo info = _opcodes.GetFunctionInfo(expression.Opcode);

			// filter out begin groups which were added by the compiler.
			if (expression.LineNumber == 0)
			{
				if (info.Name == "if")
					throw new Exception("The decompiler failed to catch a cond call.");

				FollowRootIndex(output, nameIndex, true, BranchType.CompilerBegin);
				return true;
			}

			int startIndent = output.Indent;

			// write the call's opening parenthesis and name.
			output.Write($"({info.Name}");

			// handle regular begin calls.
			if (info.Name.StartsWith("begin")|| info.Name == "or" || info.Name == "and" || info.Name == "branch")
			{
				FollowRootIndex(output, nameIndex, true, BranchType.Multiline);
				output.Indent = startIndent;
			}
			// handle if calls.
			else if(info.Name == "if")
			{
				FollowRootIndex(output, nameIndex, false, BranchType.If);
				output.Indent = startIndent;
			}
			// handle all other (normal) calls.
			else
			{
				FollowRootIndex(output, nameIndex, false, BranchType.Call);
			}

			// write the call's closing parenthesis.
			output.Write(")");

			HandleNewLine(output, newLine);
			return true;
		}

		/// <summary>
		/// The decompiler generates a cond construct based on an expression.
		/// </summary>
		/// <param name="output">The output.</param>
		/// <param name="expression">The cond expression.</param>
		/// <param name="newLine">Indicates whether the expressions of the branch start on a new line.</param>
		/// <returns>Returns true if code was generated.</returns>
		private bool GenerateCond(IndentedTextWriter output, ScriptExpression expression, bool newLine)
		{
			int startIndent = output.Indent;

			// Handle the initial con group.
			if (_condIndentStack.Count == 0 || expression.NextExpression != null)
			{
				// indicate that the following expressions are part of a cond construct. 
				_condIndentStack.Push(output.Indent + 1);

				output.WriteLine("(cond");

				// increase indent for the first conditional and write the opening parenthesis.
				output.Indent++;
				output.Write("(");

				// generate code.
				DatumIndex nameIndex = new DatumIndex(expression.Value.UintValue);
				FollowRootIndex(output, nameIndex, false, BranchType.Cond);

				// write the closing parenthesis of the last conditional.
				output.Indent = startIndent + 1;
				output.WriteLine(")");

				// write the closing parenthesis of the cond call.
				output.Indent = startIndent;
				output.Write(")");

				_condIndentStack.Pop();
			}

			// handle all following conditionals.
			else
			{
				output.Indent = _condIndentStack.Peek();

				// close the previous cond group and open the current one.
				output.WriteLine(")");
				output.Write("(");

				// generate code.
				DatumIndex nameIndex = new DatumIndex(expression.Value.UintValue);
				FollowRootIndex(output, nameIndex, false, BranchType.Cond);

				output.Indent = startIndent;
			}

			HandleNewLine(output, newLine);
			return true;
		}

		/// <summary>
		/// The decompiler generates a script reference based on an expression.
		/// </summary>
		/// <param name="output">The output.</param>
		/// <param name="expression">The script reference expression.</param>
		/// <param name="newLine">Indicates whether the expressions of the branch start on a new line.</param>
		/// <returns>Returns true if code was generated.</returns>
		private bool GenerateScriptReference(IndentedTextWriter output, ScriptExpression expression, bool newLine)
		{
			// we need to retrieve the script's name from its function_name expression. Otherwise branch calls will become corrupt.
			DatumIndex nameIndex = new DatumIndex(expression.Value.UintValue);
			var nameExp =_scripts.Expressions.FindExpression(nameIndex);

			// write the call's opening parenthesis and name.
			output.Write($"({nameExp.StringValue}");

			// write code
			FollowRootIndex(output, nameIndex, false, BranchType.Call);

			// write the call's closing parenthesis.
			output.Write(")");
			HandleNewLine(output, newLine);
			return true;
		}

		/// <summary>
		/// The decompiler generates a globals reference based on an expression.
		/// </summary>
		/// <param name="output">The output.</param>
		/// <param name="expression">The globals reference expression.</param>
		/// <param name="newLine">Indicates whether the expressions of the branch start on a new line.</param>
		/// <returns>Returns true if code was generated.</returns>
		private bool GenerateGlobalsReference(IndentedTextWriter output, ScriptExpression expression, bool newLine)
		{
			output.Write(expression.StringValue);
			HandleNewLine(output, newLine);
			return true;
		}

		/// <summary>
		/// The decompiler generates a script parameter reference based on an expression.
		/// </summary>
		/// <param name="output">The output.</param>
		/// <param name="expression">The parameter reference expression.</param>
		/// <param name="newLine">Indicates whether the expressions of the branch start on a new line.</param>
		/// <returns>Returns true if code was generated.</returns>
		private bool GenerateParameterReference(IndentedTextWriter output, ScriptExpression expression, bool newLine)
		{
			output.Write(expression.StringValue);
			HandleNewLine(output, newLine);
			return true;
		}

		/// <summary>
		/// The decompiler generates a literal (expression) on an expression.
		/// </summary>
		/// <param name="output">The output.</param>
		/// <param name="expression">The expression.</param>
		/// <param name="newLine">Indicates whether the expressions of the branch start on a new line.</param>
		/// <returns>Returns true if code was generated.</returns>
		private bool GenerateExpression(IndentedTextWriter output, ScriptExpression expression, bool newLine)
		{		
			ScriptValueType type = _opcodes.GetTypeInfo(expression.ReturnType);
			ScriptValueType actualType = type;

			// Check if a typecast is occurring
			if (expression.Opcode != 0xFFFF)
			{
				actualType = _opcodes.GetTypeInfo(expression.Opcode);

				// Simply write the string for quoted expressions.
				if (actualType.Quoted)
				{
					// Don't quote the keyword none.
					if (expression.Value.IsNull || expression.StringValue == "none")
                    {
						output.Write("none");
					}
                    else
                    {
						output.Write("\"{0}\"", ScriptStringHelpers.Escape(expression.StringValue));
					}
					return false;
				}
			}

			uint value = GetValue(expression, actualType);
			byte[] val = BitConverter.GetBytes(value);
			string text;


			switch (actualType.Name)
			{
				case "void":
					text = "";
					break;
				case "ai_command_script":
				case "script":
					short index = BitConverter.ToInt16(val, 0);
					text = _scripts.Scripts[index].Name;
					break;
				case "boolean":
					text = BitConverter.ToBoolean(val, 0) ? "true" : "false";
					break;
				case "short":
					text = BitConverter.ToInt16(val, 0).ToString();
					break;
				case "long":
					// Signed integer
					int signed = (int)value;
					text = signed.ToString();
					break;
				case "real":
					float fl = BitConverter.ToSingle(val, 0);
					text = fl.ToString("0.0#######", CultureInfo.InvariantCulture);
					break;
				case "ai_line":
					text = expression.StringValue == "" ? "\"\"" : expression.StringValue;
					break;
				case "unit_seat_mapping":
					text = expression.Value.IsNull ? "none" : expression.StringValue;
					break;

				default:
					if(expression.Value.IsNull)
					{
						text = "none";
					}
					else if(actualType.IsEnum)
					{
						string enumValue = actualType.GetEnumValue(value);
						if (enumValue != null)
						{
							text = enumValue;
						}
						else
						{
							throw new NotImplementedException("Unknown Enum Value.");
						}
					}
					else
					{
						//throw new NotImplementedException($"Unhandled Return Type: \"{actualType.Name}\".");
						text = expression.StringValue;
					}
						
					break;
			}

			output.Write(text);
			HandleNewLine(output, newLine);
			return false;
		}

		/// <summary>
		/// Retrieves an expression's value. Takes endianness into account.
		/// </summary>
		/// <param name="expression">The expression from which the value is being retrieved.</param>
		/// <param name="type">The value type of the expression.</param>
		/// <returns></returns>
		private uint GetValue(ScriptExpression expression, ScriptValueType type)
		{
			if (_endian == Endian.BigEndian)
			{
				return expression.Value.UintValue >> (32 - (type.Size * 8));

			}
			else
			{
				var bytes = BitConverter.GetBytes(expression.Value.UintValue);
				uint result = type.Size switch
				{
					4 => expression.Value.UintValue,
					2 => BitConverter.ToUInt16(bytes, 0),
					1 => bytes[0],
					0 => 0,
					_ => throw new ArgumentException($"Script value types can only have a size of 0, 1, 2 or 4 bytes. The size {type.Size} is invalid.")
				};
				return result;
			}
		}

		/// <summary>
		/// Finishes the current line and causes a line break if the next expression begins on a new line.
		/// </summary>
		/// <param name="output">The output.</param>
		/// <param name="newLine">Indicates whether the expressions of the branch start on a new line.</param>
		private void HandleNewLine(IndentedTextWriter output, bool newLine)
		{
			if(newLine)
			{
				output.WriteLine();
			}
		}

		/// <summary>
		/// Determines whether an expression is a multiline construct.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns>Returns true if the expression is a multiline construct.</returns>
		private bool IsMultilineExpression(ScriptExpression expression)
		{
			if(expression.Type == ScriptExpressionType.Group)
			{
				FunctionInfo info = _opcodes.GetFunctionInfo(expression.Opcode);
				return info.Name == "if" || info.Name == "or" || info.Name == "and" || info.Name.StartsWith("begin");
			}

			return false;
		}
	}
}
