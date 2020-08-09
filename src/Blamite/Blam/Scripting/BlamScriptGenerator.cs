#if NET45

using Blamite.IO;
using System;
using System.CodeDom.Compiler;
using System.Globalization;

namespace Blamite.Blam.Scripting
{
	// This code sucks...
	public class BlamScriptGenerator
	{
		private readonly OpcodeLookup _opcodes;
		private readonly ScriptTable _scripts;
		private readonly Endian _endian;
		private bool _nextFunctionIsScript;
		private bool _onNewLine = true;
		private bool _h4;

		private bool _nextExpressionIsVar;
		private bool _varTypeWritten;
		private int localVarCounter;

		public BlamScriptGenerator(ScriptTable scripts, OpcodeLookup opcodes, Endian endian)
		{
			_endian = endian;
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
			localVarCounter = 0;
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
						FunctionInfo info = _opcodes.GetFunctionInfo(expression.Opcode);
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




			bool wroteAnything = wroteAnything = HandleExpression(expression, output);
			int startIndent = output.Indent;

			int currentArg = 0;

			if (_h4 && firstrun)
			{
				firstIndentedArg = 0;
				currentArg = 1;
				_h4 = false;
			}

			ScriptExpression sibling = expression.NextExpression;
			while (sibling != null)
			{
				if (wroteAnything && !_nextExpressionIsVar)
				{
					if (currentArg == firstIndentedArg)
						output.Indent++;
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

				if (!_nextExpressionIsVar)
					wroteAnything = HandleExpression(sibling, output);
				else if ((_nextExpressionIsVar && sibling.Opcode != 0xFFFF))
				{
					if (!_varTypeWritten)
					{
						ScriptValueType type = _opcodes.GetTypeInfo((ushort)sibling.ReturnType);
						output.Write(type.Name + " var_" + localVarCounter.ToString() + " ");
						_varTypeWritten = true;
					}
					
					wroteAnything = HandleExpression(sibling, output);
				}

				sibling = sibling.NextExpression;
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

		private bool HandleExpression(ScriptExpression expression, IndentedTextWriter output)
		{
			short realtype = (short)expression.Type;
			short clippedtype = (short)((short)expression.Type & 0xFF);

			switch ((ScriptExpressionType) clippedtype)
			{
				case ScriptExpressionType.Expression:
				case ScriptExpressionType.Expression4:
					return GenerateExpressionCode(expression, output);

				case ScriptExpressionType.GlobalsReference:
				case ScriptExpressionType.GlobalsReference4:
					{
						if ((realtype & 0xFF00) > 0)
							return GenerateVariableReference(expression, output, true);
						else
						return GenerateGlobalsReference(expression, output);
					}
					

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
			}

			uint value = GetValue(expression, type, _endian);

			byte[] val = BitConverter.GetBytes(value);

			switch (type.Name)
			{
				case "void":
					return false;
				case "boolean":
					if (BitConverter.ToBoolean(val,0))
						output.Write("true");
					else
						output.Write("false");
					break;
				case "short":
					output.Write(BitConverter.ToInt16(val,0));
					break;
				case "long":
					// Signed integer
					output.Write((int) value);
					break;
				case "real":
					// Eww
					//var floatBytes = new byte[4];
					//floatBytes[0] = (byte) (value & 0xFF);
					//floatBytes[1] = (byte) ((value >> 8) & 0xFF);
					//floatBytes[2] = (byte) ((value >> 16) & 0xFF);
					//floatBytes[3] = (byte) ((value >> 24) & 0xFF);
					var fl = BitConverter.ToSingle(val, 0);
					output.Write(fl.ToString("0.0#######", CultureInfo.InvariantCulture));
					break;
				case "function_name":
					if (_nextFunctionIsScript)
					{
						if (expression.Opcode >= _scripts.Scripts.Count)
							output.Write("import#" + expression.StringValue);
						else
							output.Write(expression.StringValue); // todo: there are cases (h3 xbox mainmenu's campaign_cam specifically) where the function_name expression's opcode value is +1 from what it should be, and the expression prior has the right index.
							// the current state of this script code doesnt seem to be good enough to step back so here is the hacky fix implemented in the HO fork.

							//output.Write(_scripts.Scripts[expression.Opcode].Name);

						_nextFunctionIsScript = false;
					}
					else
					{
						FunctionInfo info = _opcodes.GetFunctionInfo(expression.Opcode);
						if (info == null)
							output.Write("UNNAMED_OPCODE_" + expression.Opcode.ToString("X4") + "#" + expression.StringValue);
						else
							output.Write(info.Name);
						//throw new InvalidOperationException("Unrecognized function opcode 0x" + expression.Opcode.ToString("X"));
					}
					break;
				case "unit_seat_mapping":
					// This isn't the technical way of doing this,
					// but since seat mapping names aren't stored anywhere,
					// it would be tricky to resolve them unless we just use an index for now
					if (expression.Value != 0xFFFFFFFF)
						output.Write(expression.StringValue);
					else
						output.Write("none");
					break;
				case "unparsed":
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
			output.Write(expression.StringValue);
			return true;
		}

		private bool GenerateParameterReference(ScriptExpression expression, IndentedTextWriter output)
		{
			_onNewLine = false;
			output.Write(expression.StringValue);
			return true;
		}

		private bool GenerateVariableReference(ScriptExpression expression, IndentedTextWriter output, bool islocal = false)
		{
			_onNewLine = false;
			string varDesc = islocal ? ("var_" + expression.Value.ToString() + "#") : "";
			output.Write(varDesc + expression.StringValue);
			return true;
		}

		private bool GenerateVariableDecl(ScriptExpression expression, IndentedTextWriter output)
		{
			_onNewLine = false;
			output.Write("(local ");
			var expressionIndex = new DatumIndex(expression.Value);
			_nextExpressionIsVar = true;
			_varTypeWritten = false;
			GenerateCode(_scripts.Expressions.FindExpression(expressionIndex), output);
			_nextExpressionIsVar = false;
			localVarCounter++;
			output.Write(")");
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

		private uint GetValue(ScriptExpression expression, ScriptValueType type, Endian endian)
		{
			if (endian == Endian.BigEndian)
				return expression.Value >> (32 - (type.Size * 8));
			else
				return expression.Value;
		}
	}
}

#endif
