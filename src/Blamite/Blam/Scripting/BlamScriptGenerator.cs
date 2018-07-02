#if NET45

using System;
using System.CodeDom.Compiler;

namespace Blamite.Blam.Scripting
{
	// This code sucks...
	public class BlamScriptGenerator
	{
		private readonly OpcodeLookup _opcodes;
		private readonly ScriptTable _scripts;
		private bool _nextFunctionIsScript;
		private bool _onNewLine = true;
		private bool _h4;

		public BlamScriptGenerator(ScriptTable scripts, OpcodeLookup opcodes)
		{
			_scripts = scripts;
			_opcodes = opcodes;
		}

		public void WriteComment(string comment, IndentedTextWriter output)
		{
			output.WriteLine("; {0}", comment);
		}

		public void WriteExpression(ScriptExpression expression, IndentedTextWriter output)
		{
			_onNewLine = true;
			GenerateCode(expression, output, true);
		}

		public void WriteExpression(DatumIndex expressionIndex, IndentedTextWriter output, bool h4 = false)
		{
			_h4 = h4;
			WriteExpression(_scripts.Expressions.FindExpression(expressionIndex), output);
		}

		private void GenerateCode(ScriptExpression expression, IndentedTextWriter output, bool firstrun = false)
		{
			int firstIndentedArg = int.MaxValue;
			bool isFunctionCall = false;

			if (expression.Type == ScriptExpressionType.Expression || expression.Type == ScriptExpressionType.Expression4)
			{
				ScriptValueType type = _opcodes.GetTypeInfo((ushort)expression.ReturnType);
				if (type.Name == "function_name")
				{
					isFunctionCall = true;

					if (!_nextFunctionIsScript)
					{
						ScriptFunctionInfo info = _opcodes.GetFunctionInfo(expression.Opcode);
						if (info != null)
						{
							//if (expression.LineNumber == 0 && !info.Name.StartsWith("begin"))
							//	System.Diagnostics.Debug.WriteLine(info.Name);



							if (info.Name.StartsWith("begin"))
							{
								firstIndentedArg = 0;
								if (expression.LineNumber > 0 && !_onNewLine)
								{
									output.Indent++;
									output.WriteLine();
								}
							}
							else if (info.Name == "if")
							{
								firstIndentedArg = 1;
							}

						}
					}
					if (expression.LineNumber > 0)
						output.Write("(");
				}
			}


			

			bool wroteAnything = HandleExpression(expression, output);
			int startIndent = output.Indent;

			int currentArg = 0;

			if (_h4 && firstrun)
			{
				firstIndentedArg = 0;
				currentArg = 1;
				_h4 = false;
			}

			ScriptExpression sibling = expression.Next;
			while (sibling != null)
			{
				if (wroteAnything)
				{
					//output.Write(" _" + currentArg.ToString() + "_" + firstIndentedArg.ToString());
					if (currentArg == firstIndentedArg)
						output.Indent++;
					//if (currentArg >= firstIndentedArg || output.Indent != startIndent)
					//{
					//	output.WriteLine();
					//	_onNewLine = true;
					//}
					if (currentArg >= firstIndentedArg)
					{
						output.WriteLine();
						_onNewLine = true;
					}
					else if (output.Indent != startIndent)
					{
						output.WriteLine();
						_onNewLine = true;
					}
					else
					{
						output.Write(" ");
					}
				}

				wroteAnything = HandleExpression(sibling, output);
				sibling = sibling.Next;
				currentArg++;
			}

			if (isFunctionCall && expression.LineNumber > 0)
			{
				if (output.Indent != startIndent)
				{
					output.Indent = startIndent;
					if (wroteAnything)
						output.WriteLine();
					output.Write(")");
					_onNewLine = true;
				}
				else
				{
					output.Write(")");
				}
			}


		}

		private void GenerateCodeOld(ScriptExpression expression, IndentedTextWriter output)
		{
			int firstIndentedArg = int.MaxValue;
			bool isFunctionCall = false;

			if (expression.Type == ScriptExpressionType.Expression || expression.Type == ScriptExpressionType.Expression4)
			{
				ScriptValueType type = _opcodes.GetTypeInfo((ushort)expression.ReturnType);
				if (type.Name == "function_name")
				{
					isFunctionCall = true;

					if (!_nextFunctionIsScript)
					{
						ScriptFunctionInfo info = _opcodes.GetFunctionInfo(expression.Opcode);
						if (info != null)
						{
							if (info.Name.StartsWith("begin"))
							{
								firstIndentedArg = 0;
								if (expression.LineNumber > 0 && !_onNewLine)
								{
									output.Indent++;
									output.WriteLine();
								}
							}
							else if (info.Name == "if")
							{
								firstIndentedArg = 1;
							}
						}
					}
					if (expression.LineNumber > 0)
						output.Write("(");
				}
			}

			bool wroteAnything = HandleExpression(expression, output);
			int startIndent = output.Indent;

			int currentArg = 0;
			ScriptExpression sibling = expression.Next;
			while (sibling != null)
			{
				if (wroteAnything)
				{
					if (currentArg == firstIndentedArg)
						output.Indent++;
					if (currentArg >= firstIndentedArg || output.Indent != startIndent)
					{
						output.WriteLine();
						_onNewLine = true;
					}
					else
					{
						output.Write(" ");
					}
				}

				wroteAnything = HandleExpression(sibling, output);
				sibling = sibling.Next;
				currentArg++;
			}

			if (isFunctionCall && expression.LineNumber > 0)
			{
				if (output.Indent != startIndent)
				{
					output.Indent = startIndent;
					if (wroteAnything)
						output.WriteLine();
					output.Write("x)");
					_onNewLine = true;
				}
				else
				{
					output.Write("y)");
				}
			}
		}

		private bool HandleExpression(ScriptExpression expression, IndentedTextWriter output)
		{
			short realtype = (short)((short)expression.Type & 0xFF);
			//short realtype = (short)expression.Type;

			switch ((ScriptExpressionType) realtype)
			{
				case ScriptExpressionType.Expression:
				case ScriptExpressionType.Expression4:
					return GenerateExpressionCode(expression, output);

				case ScriptExpressionType.GlobalsReference:
				case ScriptExpressionType.GlobalsReference4:
					return GenerateGlobalsReference(expression, output);

				case ScriptExpressionType.ParameterReference:
				case ScriptExpressionType.ParameterReference4:
					return GenerateParameterReference(expression, output);

				case ScriptExpressionType.ScriptReference:
				case ScriptExpressionType.ScriptReference4:
					return GenerateScriptReference(expression, output);

				case ScriptExpressionType.Group:
				case ScriptExpressionType.Group4:
					return GenerateGroup(expression, output);

				case ScriptExpressionType.VariableReference4:
					return GenerateVariableReference(expression, output);

				case ScriptExpressionType.VariableDecl4:
					return GenerateVariableDecl(expression, output);

				default:
					throw new InvalidOperationException("Unknown script expression type");
			}
		}

		private bool GenerateExpressionCode(ScriptExpression expression, IndentedTextWriter output)
		{
			if (expression.LineNumber == 0)
				return false;

			//if (_locallinenumber == 0)
			//{
			//	_locallinenumber++;
			//	return false;
			//}
			

			_onNewLine = false;
			ScriptValueType type = _opcodes.GetTypeInfo((ushort) expression.ReturnType);
			ScriptValueType actualType = type;
			if (type.Name != "function_name")
			{
				// Check if a typecast is occurring

				if (expression.Opcode != 0xFFFF)
				{
					actualType = _opcodes.GetTypeInfo(expression.Opcode);

					if (actualType.Quoted)
					{
						if (expression.Value != 0xFFFFFFFF)
							output.Write("\"{0}\"", expression.StringValue);
						else
							output.Write("none");
						return true;
					}
				}
				else
				{
					output.Write("_UNPARSED");
				}

			}

			uint value = GetValue(expression, type);
			switch (type.Name)
			{
				case "void":
					return false;
				case "boolean":
					if (value > 0)
						output.Write("true");
					else
						output.Write("false");
					break;
				case "short":
				case "long":
					// Signed integer
					output.Write((int) value);
					break;
				case "real":
					// Eww
					var floatBytes = new byte[4];
					floatBytes[0] = (byte) (value & 0xFF);
					floatBytes[1] = (byte) ((value >> 8) & 0xFF);
					floatBytes[2] = (byte) ((value >> 16) & 0xFF);
					floatBytes[3] = (byte) ((value >> 24) & 0xFF);
					output.Write(BitConverter.ToSingle(floatBytes, 0));
					break;
				case "function_name":
					if (_nextFunctionIsScript)
					{
						if (expression.Opcode >= _scripts.Scripts.Count)
							output.Write("_IMPORT FOR 0x" + expression.Opcode.ToString("X4"));
						else
							output.Write(_scripts.Scripts[expression.Opcode].Name);
						_nextFunctionIsScript = false;
					}
					else
					{
						ScriptFunctionInfo info = _opcodes.GetFunctionInfo(expression.Opcode);

						ScriptExpression blah = new ScriptExpression();
						blah.Opcode = expression.Opcode;
						blah.StringValue = expression.StringValue;//info.Name;

						int exist = _scripts.hsExp.FindIndex(x => x.Opcode == blah.Opcode);

						if (exist == -1)
						{
							if (info != null)
							{
								if (info.Name != blah.StringValue)
									blah.StringValue = "¥" + blah.StringValue;
							}

							_scripts.hsExp.Add(blah);
						}

						if (info == null)
							output.Write("[" + expression.Opcode.ToString("X4") + "]" + expression.StringValue);
						else
							output.Write(info.Name);
						//throw new InvalidOperationException("Unrecognized function opcode 0x" + expression.Opcode.ToString("X"));

						//string namey = info.Name;
						//if (info.Name == "")
						//	namey = "[" + expression.Opcode.ToString("X4") + "]" + expression.StringValue;

						//output.Write(namey);
					}
					break;
				case "unit_seat_mapping":
					// This isn't the technical way of doing this,
					// but since seat mapping names aren't stored anywhere,
					// it would be tricky to resolve them unless we just use an index for now
					if (expression.Value != 0xFFFFFFFF)
						output.Write(expression.Value & 0xFFFF);
					else
						output.Write("none");
					break;
				default:
					string enumValue = actualType.GetEnumValue(value);
					if (enumValue != null)
					{
						output.Write(enumValue);
					}
					else if (expression.Value == 0xFFFFFFFF)
					{
						output.Write("none");
					}
					else
					{
						enumValue = expression.StringValue;
						if (enumValue != null)
							output.Write(enumValue);
						else
							output.Write("0x{0:X}", value);
					}
					break;
			}
			return true;
		}

		private bool GenerateGlobalsReference(ScriptExpression expression, IndentedTextWriter output)
		{
			_onNewLine = false;
			string globalindex = "";

			if ((byte)((expression.Value >> 24) & 0xFF) == 0xFF)
			{
				//globalindex = " {{" + (expression.Value ^ 0xFFFF8000).ToString("X3") + "}}";
				ushort index = (ushort)(expression.Value ^ 0xFFFF8000);
				ScriptGlobal blah = new ScriptGlobal();
				blah.Name = expression.StringValue;
				blah.Type = (short)expression.Type.GetTypeCode();
				blah.ExpressionIndex = new DatumIndex(0xFFFF, index);

				int exist = _scripts.hsGlob.FindIndex(x => x.ExpressionIndex.Index == index);

				if (exist == -1)
					_scripts.hsGlob.Add(blah);
			}

			output.Write(expression.StringValue + globalindex);
			return true;
		}

		private bool GenerateParameterReference(ScriptExpression expression, IndentedTextWriter output)
		{
			_onNewLine = false;
			output.Write(expression.StringValue);
			return true;
		}

		private bool GenerateVariableReference(ScriptExpression expression, IndentedTextWriter output)
		{
			_onNewLine = false;
			output.Write(expression.StringValue);
			return true;
		}

		private bool GenerateVariableDecl(ScriptExpression expression, IndentedTextWriter output)
		{
			_onNewLine = false;
			output.Write("_VAR DECL");
			return true;
		}

		private bool GenerateScriptReference(ScriptExpression expression, IndentedTextWriter output)
		{
			var expressionIndex = new DatumIndex(expression.Value);

			_nextFunctionIsScript = true;
			GenerateCode(_scripts.Expressions.FindExpression(expressionIndex), output);
			_nextFunctionIsScript = false;
			return true;
		}

		private bool GenerateGroup(ScriptExpression expression, IndentedTextWriter output)
		{
			var childIndex = new DatumIndex(expression.Value);
			if (!childIndex.IsValid)
				throw new InvalidOperationException("Group expression has no child");

			GenerateCode(_scripts.Expressions.FindExpression(childIndex), output);
			return true;
		}

		private uint GetValue(ScriptExpression expression, ScriptValueType type)
		{
			return expression.Value >> (32 - (type.Size*8));
		}
	}
}

#endif
