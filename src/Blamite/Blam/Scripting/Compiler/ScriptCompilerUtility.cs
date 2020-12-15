using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using System.Xml;
using Blamite.IO;
using System.IO;
using System.Configuration;
using Blamite.Blam.Scripting.Context;
using Blamite.Util;
using System.Runtime.CompilerServices;

namespace Blamite.Blam.Scripting.Compiler
{
    public partial class ScriptCompiler : BS_ReachBaseListener
    {
        private FunctionInfo RetrieveFunctionInfo(string function, int parameterCount)
        {
            List<FunctionInfo> infos = _opcodes.GetFunctionInfo(function);
            if (infos == null || infos.Count == 0)
            {
                return null;
            }

            // Overloaded functions exist. Select the right one based on its parameter count and whether the function is implemented or not.
            if (infos.Count > 1)
            {
                return infos.Find(i => i.Implemented && i.ParameterTypes.Count() == parameterCount);

            }
            else
            {
                return infos[0];

            }
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
                XMLUtil.WriteScriptExpressionsToXml(_expressions, fileName);
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
        public void CloseDatum()
        {
            if (_openDatums.Count > 0)
            {
                int index = _openDatums.Pop();
                if (_debug)
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
        public void LinkDatum()
        {
            if (_openDatums.Count > 0)
            {
                int index = _openDatums.Pop();
                if (_debug)
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

        public void OpenDatum(int index)
        {
            _openDatums.Push(index);
            if (_debug)
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
            if (_debug)
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

        private void EqualityPush(string type)
        {
            if(_equality)
            {
                _expectedTypes.PushType(type);
                _equality = false;
            }
        }

        private void ReportProgress()
        {
            _processedDeclarations++;
            _progress.Report(_processedDeclarations * 100 / _declarationCount);
        }

        private short GetLineNumber(ParserRuleContext context)
        {
            return (short)context.Start.Line;
        }

        private bool TryGetObjectFromContext(out ScriptingContextObject scriptObject, params Tuple<string, string>[] path)
        {
            if(_scriptingContext.TryGetObjects(out IEnumerable<ScriptingContextObject> objects, path))
            {
                scriptObject = objects.First();
                if (objects.Count() > 1)
                {
                    var lastPathTuple = path.Last();
                    _logger.Warning($"The block \"{lastPathTuple.Item1}\" contained multiple objects with the name \"{lastPathTuple.Item2}\". " +
                        $"The first occurence with the index {scriptObject.Index} was chosen by the compiler.");
                }
                return true;
            }
            else
            {
                scriptObject = null;
                return false;
            }
        }

        private bool TryGetChildObjectFromObject(ScriptingContextObject parent, string blockName, string name, out ScriptingContextObject child)
        {
            var children = parent.GetChildObjects(blockName, name).ToArray();
            if(children.Length == 0)
            {
                child = null;
                return false;
            }

            child = children[0];

            if(children.Length > 1)
            {
                _logger.Warning($"The child block \"{blockName}\" contained multiple objects with the name \"{name}\". " +
                    $"The first occurence with the index {child.Index} was chosen by the compiler.");
            }
            return true;
        }

        private bool IsNone(string text)
        {
            return text == "none" || text == "None" || text == "NONE";
        }
    }
}
