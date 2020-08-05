using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using System.Xml;
using Blamite.IO;
using System.IO;
using System.Configuration;

namespace Blamite.Blam.Scripting.Compiler
{
    public partial class ScriptCompiler : BS_ReachBaseListener
    {
        private FunctionInfo RetrieveFunctionInfo(string function, int parameterCount, int line)
        {
            List<FunctionInfo> infos = _opcodes.GetFunctionInfo(function);
            if (infos == null)
            {
                throw new CompilerException($"The opcode for function \"{function}\" with parameter count \"{parameterCount}\" couldn't be retrieved. Please ensure that this is a valid function name."
                        + "\nAlternatively, a script declaration could be missing in your .hsc file.", function, line);
            }

            // Overloaded functions exist. Select the right one based on its parameter count and whether the function is implemented or not.
            FunctionInfo result;
            if (infos.Count > 1)
            {
                result = infos.Find(i => i.Implemented && i.ParameterTypes.Count() == parameterCount);

            }
            else
            {
                result = infos[0];

            }

            return result;
        }

        private BS_ReachParser.ScriptDeclarationContext GetParentScriptContext(ParserRuleContext ctx)
        {
            RuleContext parent = ctx;

            for(int i = 0; i < ctx.Depth(); i++)
            {
                parent = parent.Parent;
                if(parent is BS_ReachParser.ScriptDeclarationContext scriptDeclaration)
                {
                    return scriptDeclaration;
                }
            }
            throw new CompilerException("Failed to retrieve the parent script declaration context.", ctx.GetText(), ctx.Start.Line);
        }

        private RuleContext GetParentContext(RuleContext context, int ruleIndex)
        {
            RuleContext parent = context.Parent;
            if (parent.RuleIndex == ruleIndex)
            {
                return parent;
            }
            while (parent.RuleIndex != BS_ReachParser.RULE_hsc)
            {
                parent = parent.Parent;
                if (parent.RuleIndex == ruleIndex)
                {
                    return parent;
                }
            }
            return null;
        }

        private void ExpressionsToXML()
        {
            if (_expressions.Count > 0)
            {
                string folder = "Compiler";
                string fileName = Path.Combine(folder, _cacheFile.InternalName + "_expressions.xml");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var settings = new XmlWriterSettings
                {
                    Indent = true
                };

                using (var writer = XmlWriter.Create(fileName, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Expressions");

                    for (int i = 0; i < _expressions.Count; i++)
                    {
                        var exp = _expressions[i];
                        writer.WriteStartElement("Expression");
                        writer.WriteAttributeString("Num", i.ToString("X4"));
                        writer.WriteAttributeString("Salt", exp.Index.Salt.ToString("X4"));
                        writer.WriteAttributeString("Opcode", exp.Opcode.ToString("X4"));
                        writer.WriteAttributeString("ValueType", exp.ReturnType.ToString("X4"));                       
                        switch (exp.Type)
                        {
                            case ScriptExpressionType.Group:
                                writer.WriteAttributeString("ExpressionType", "Call");
                                break;
                            case ScriptExpressionType.Expression:
                                writer.WriteAttributeString("ExpressionType", "Expression");
                                break;
                            case ScriptExpressionType.ScriptReference:
                                writer.WriteAttributeString("ExpressionType", "ScriptRef");
                                break;
                            case ScriptExpressionType.GlobalsReference:
                                writer.WriteAttributeString("ExpressionType", "GlobalRef");
                                break;
                            case ScriptExpressionType.ParameterReference:
                                writer.WriteAttributeString("ExpressionType", "ScriptPar");
                                break;

                        }
                        writer.WriteAttributeString("NextSalt", exp.Next.Salt.ToString("X4"));
                        writer.WriteAttributeString("NextIndex", exp.Next.Index.ToString("X4"));
                        writer.WriteAttributeString("StringOff", exp.StringOffset.ToString("X"));
                        writer.WriteAttributeString("Value", exp.Value.ToString("X8"));
                        writer.WriteAttributeString("LineNum", exp.LineNumber.ToString("X4"));
                        if(exp.StringOffset != _randomAddress)
                            writer.WriteAttributeString("String", _strings.GetString(exp.StringOffset));
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Close();
                }
            }
        }

        private void StringsToFile()
        {
            string folder = "Compiler";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            var path = Path.Combine(folder, _cacheFile.InternalName + "_Strings.bin");
            using (EndianWriter writer = new EndianWriter(new FileStream(path, FileMode.Create), Endian.BigEndian))
            {
                _strings.Write(writer);
            }
        }

        private void DeclarationsToXML()
        {
            string folder = "Compiler";
            string fileName = Path.Combine(folder, _cacheFile.InternalName + "_Declarations.xml");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var settings = new XmlWriterSettings
            {
                Indent = true
            };

            using (var writer = XmlWriter.Create(fileName, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("declarations");
                
                // Globals.
                writer.WriteStartElement("globals");
                foreach (ScriptGlobal glo in _globals)
                {
                    writer.WriteStartElement("global");
                    writer.WriteAttributeString("Name", glo.Name);
                    writer.WriteAttributeString("RetType", glo.Type.ToString("X4"));
                    writer.WriteAttributeString("Salt", glo.ExpressionIndex.Salt.ToString("X4"));
                    writer.WriteAttributeString("Index", glo.ExpressionIndex.Index.ToString("X4"));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                // Scripts.
                writer.WriteStartElement("scripts");
                foreach (Script scr in _scripts)
                {
                    writer.WriteStartElement("script");
                    writer.WriteAttributeString("Name", scr.Name);
                    writer.WriteAttributeString("Type", scr.ExecutionType.ToString("X4"));
                    writer.WriteAttributeString("RetType", scr.ReturnType.ToString("X4"));
                    writer.WriteAttributeString("Salt", scr.RootExpressionIndex.Salt.ToString("X4"));
                    writer.WriteAttributeString("Index", scr.RootExpressionIndex.Index.ToString("X4"));

                    if (scr.Parameters.Count > 0)
                    {
                        foreach (var p in scr.Parameters)
                        {
                            writer.WriteStartElement("parameter");
                            writer.WriteAttributeString("Name", p.Name);
                            writer.WriteAttributeString("Type", p.Type.ToString("X4"));
                            writer.WriteEndElement();
                        }
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
            }
        }

        /// <summary>
        /// Pops an index from the Datum stack and nulls the open Datum Index.
        /// </summary>
        private void CloseDatum()
        {
            if (_openDatums.Count > 0)
            {
                int index = _openDatums.Pop();
                if (OutputDebugInfo)
                {
                    _logger.Datum(index, CompilerDatumAction.Close);
                }

                // Null the Datum Index.
                _expressions[index].Next = DatumIndex.Null;
            }
            else
            {
                throw new InvalidOperationException("Failed to close a datum. The Datum stack is empty.");
            }
        }

        /// <summary>
        /// Pops an index from the Datum stack and links the open Datum index to the current expression.
        /// </summary>
        private void LinkDatum()
        {
            if (_openDatums.Count > 0)
            {
                int index = _openDatums.Pop();
                if (OutputDebugInfo)
                {
                    _logger.Datum(index, CompilerDatumAction.Link);
                }

                if (index != _globalPushIndex)
                {
                    _expressions[index].Next = _currentIndex;
                }
            }
            else
            {
                throw new InvalidOperationException("Failed to link a datum. The Datum stack is empty.");
            }
        }

        private void OpenDatum(int index)
        {
            _openDatums.Push(index);
            if (OutputDebugInfo)
            {
                _logger.Datum(index, CompilerDatumAction.Open);
            }
        }

        /// <summary>
        /// Pushes the current Datum index to the Datum stack and adds the expression to the table.
        /// </summary>
        /// <param name="expression"></param>
        private void OpenDatumAddExpressionIncrement(ScriptExpression expression)
        {
            int openIndex = _expressions.Count;
            if (OutputDebugInfo)
            {
                _logger.Datum(openIndex, CompilerDatumAction.Open);
            }

            _currentIndex.Increment();
            _openDatums.Push(openIndex);
            _expressions.Add(expression);
        }

        private void AddExpressionIncrement(ScriptExpression expression)
        {
            _currentIndex.Increment();
            _expressions.Add(expression);
        }

        /// <summary>
        /// Calculates the salt value of an index and returns it.
        /// </summary>
        /// <param name="index">The index belonging to the salt value.</param>
        /// <returns>The salt value.</returns>
        private ushort IndexToSalt(ushort index)
        {
            // Initial value for the expression reflexive.
            ushort init = SaltGenerator.GetSalt("script node");
            // This value is used to calculate the salt once it exceeds 0xFFFF.
            ushort restart = 0x7FFF;

            int salt = init + index;
            if (salt >= ushort.MaxValue)
            {
                salt = (salt % restart) + restart;
            }

            return (ushort)salt;
        }

        private void PushType(string type)
        {
            _expectedTypes.Push(type);
            if (OutputDebugInfo)
            {
                int index = _expectedTypes.Count - 1;
                _logger.TypeStack(type, index, StackAction.Push);
            }
        }

        private void PushTypes(params string[] types)
        {
            var reverse = types.Reverse();
            foreach(string type in reverse)
            {
                PushType(type);
            }
        }     

        private void PushTypes(string type, int count)
        {
            for(int i = 0; i < count; i++)
            {
                PushType(type);
            }
        }

        private void EqualityPush(string type)
        {
            if(_equality)
            {
                PushTypes(type);
                _equality = false;
            }
        }

        private string PopType()
        {
            if(_expectedTypes.Count > 0)
            {
                string type = _expectedTypes.Pop();
                if (OutputDebugInfo)
                {
                    int index = _expectedTypes.Count;
                    _logger.TypeStack(type, index, StackAction.Pop);
                }
                return type;
            }
            else
            {
                throw new InvalidOperationException("Failed to pop a value type. The expected types stack was empty.");
            }
        }



        private void ReportProgress()
        {
            _processedDeclarations++;
            int i = _processedDeclarations * 100 / _declarationCount;
            _progress.Report(i);
        }

        private short GetLineNumber(ParserRuleContext context)
        {
            return (short)context.Start.Line;
        }
    }
}
