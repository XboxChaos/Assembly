using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.Scripting
{
    // This code sucks...
    public class BlamScriptGenerator
    {
        private IOpcodeLookup _opcodes;
        private IScenario _scenario;
        private bool _nextFunctionIsScript = false;
        private bool _onNewLine = true;

        public BlamScriptGenerator(IScenario scenario, IOpcodeLookup opcodes)
        {
            _scenario = scenario;
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

        private void GenerateCode(ScriptExpression expression, IndentedTextWriter output)
        {
            int firstIndentedArg = int.MaxValue;
            bool isFunctionCall = false;

            if (expression.Type == ScriptExpressionType.Expression)
            {
                ScriptValueType type = _opcodes.GetTypeInfo((ushort)expression.ReturnType);
                if (type.Name == "function_name")
                {
                    isFunctionCall = true;

                    if (!_nextFunctionIsScript)
                    {
                        string func = _opcodes.GetFunctionName(expression.Opcode);
                        if (func.StartsWith("begin"))
                        {
                            firstIndentedArg = 0;
                            if (expression.LineNumber > 0 && !_onNewLine)
                            {
                                output.Indent++;
                                output.WriteLine();
                            }
                        }
                        else if (func == "if")
                        {
                            firstIndentedArg = 1;
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
            ScriptValueType type = _opcodes.GetTypeInfo((ushort)expression.ReturnType);
            ScriptValueType actualType = type;
            if (type.Name != "function_name")
            {
                // Check if a typecast is occurring
                actualType = _opcodes.GetTypeInfo((ushort)expression.Opcode);

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
                    output.Write((int)value);
                    break;
                case "real":
                    // Eww
                    byte[] floatBytes = new byte[4];
                    floatBytes[0] = (byte)(value & 0xFF);
                    floatBytes[1] = (byte)((value >> 8) & 0xFF);
                    floatBytes[2] = (byte)((value >> 16) & 0xFF);
                    floatBytes[3] = (byte)((value >> 24) & 0xFF);
                    output.Write(BitConverter.ToSingle(floatBytes, 0));
                    break;
                case "function_name":
                    if (_nextFunctionIsScript)
                    {
                        output.Write(_scenario.Scripts[(int)expression.Opcode].Name);
                        _nextFunctionIsScript = false;
                    }
                    else
                    {
                        string name = _opcodes.GetFunctionName(expression.Opcode);
                        output.Write(name);
                    }
                    break;
                default:
                    string enumValue = actualType.GetEnumValue(value);
                    if (enumValue != null)
                    {
                        output.Write(enumValue);
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
            DatumIndex expressionIndex = new DatumIndex(expression.Value);

            _nextFunctionIsScript = true;
            GenerateCode(_scenario.ScriptExpressions.FindExpression(expressionIndex), output);
            _nextFunctionIsScript = false;
            return true;
        }

        private bool GenerateGroup(ScriptExpression expression, IndentedTextWriter output)
        {
            DatumIndex childIndex = new DatumIndex(expression.Value);
            if (!childIndex.IsValid)
                throw new InvalidOperationException("Group expression has no child");

            GenerateCode(_scenario.ScriptExpressions.FindExpression(childIndex), output);
            return true;
        }

        private uint GetValue(ScriptExpression expression, ScriptValueType type)
        {
            return expression.Value >> (32 - (type.Size * 8));
        }
    }
}
