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
			GenerateCode(expression, output);
		}

		public void WriteExpression(DatumIndex expressionIndex, IndentedTextWriter output)
		{
			WriteExpression(_scripts.Expressions.FindExpression(expressionIndex), output);
		}

		private void GenerateCode(ScriptExpression expression, IndentedTextWriter output)
		{
			int firstIndentedArg = int.MaxValue;
			bool isFunctionCall = false;

			if (expression.Type == ScriptExpressionType.Expression)
			{
				ScriptValueType type = _opcodes.GetTypeInfo((ushort) expression.ReturnType);
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
			switch (expression.Type)
			{
				case ScriptExpressionType.Expression:
					return GenerateExpressionCode(expression, output);

				case ScriptExpressionType.GlobalsReference:
					return GenerateGlobalsReference(expression, output);

				case ScriptExpressionType.ParameterReference:
					return GenerateParameterReference(expression, output);

				case ScriptExpressionType.ScriptReference:
					return GenerateScriptReference(expression, output);

				case ScriptExpressionType.Group:
					return GenerateGroup(expression, output);

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
						output.Write(_scripts.Scripts[expression.Opcode].Name);
						_nextFunctionIsScript = false;
					}
					else
					{
						ScriptFunctionInfo info = _opcodes.GetFunctionInfo(expression.Opcode);
						if (info == null)
							throw new InvalidOperationException("Unrecognized function opcode 0x" + expression.Opcode.ToString("X"));

						output.Write(info.Name);
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
			output.Write(expression.StringValue);
			return true;
		}

		private bool GenerateParameterReference(ScriptExpression expression, IndentedTextWriter output)
		{
			_onNewLine = false;
			output.Write(expression.StringValue);
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