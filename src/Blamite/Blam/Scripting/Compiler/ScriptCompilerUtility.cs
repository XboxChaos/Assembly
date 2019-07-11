using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Runtime.Misc;
using System.Xml;
using Blamite.Blam.Scripting.Compiler.Expressions;
using Blamite.IO;
using System.IO;
using System.Diagnostics;

namespace Blamite.Blam.Scripting.Compiler
{
    public partial class ScriptCompiler : BS_ReachBaseListener
    {

        private ScriptFunctionInfo RetrieveFunctionInfo(string function, int parameterCount)
        {
            var infos = _opcodes.GetFunctionInfo(function);
            if (infos == null)
                throw new ArgumentException($"A Function opcode couldn't be retrieved. Name: {function} , Parameter count: {parameterCount}");

            ScriptFunctionInfo result;
            // overloaded functions exist. select the right one based on its parameter count
            if (infos.Count > 1)
                result = infos.Find(i => i.ParameterTypes.Count() == parameterCount);
            else
                result = infos.First();

            return result;
        }

        private void ExpressionsToXML()
        {
            if (_expressions.Count > 0)
            {
                string folder = "Compiler_Debug";
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                string fileName = Path.Combine(folder, _cashefile.InternalName + "_expressions.xml");

                var settings = new XmlWriterSettings();
                settings.Indent = true;

                using (var writer = XmlWriter.Create(fileName, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Expressions");

                    for (int i = 0; i < _expressions.Count; i++)
                    {
                        var exp = _expressions[i];
                        writer.WriteStartElement("Expression");
                        writer.WriteAttributeString("Num", i.ToString());
                        writer.WriteAttributeString("Salt", exp.Salt.ToString());
                        writer.WriteAttributeString("Opcode", exp.OpCode.ToString());
                        writer.WriteAttributeString("ValueType", exp.ValueType.ToString());                       
                        switch (exp.ExpressionType)
                        {
                            case 8:
                                writer.WriteAttributeString("ExpressionType", "Call");
                                break;
                            case 9:
                                writer.WriteAttributeString("ExpressionType", "Expression");
                                break;
                            case 10:
                                writer.WriteAttributeString("ExpressionType", "ScriptRef");
                                break;
                            case 13:
                                writer.WriteAttributeString("ExpressionType", "GlobalRef");
                                break;
                            case 41:
                                writer.WriteAttributeString("ExpressionType", "ScriptVar");
                                break;

                        }
                        writer.WriteAttributeString("NextSalt", exp.NextExpression.Salt.ToString());
                        writer.WriteAttributeString("NextIndex", exp.NextExpression.Index.ToString());
                        writer.WriteAttributeString("StringAddr", exp.StringAddress.ToString());
                        writer.WriteAttributeString("Value", exp.ValueToString);
                        writer.WriteAttributeString("LineNum", exp.LineNumber.ToString());
                        if(exp.StringAddress != _randomAddress)
                            writer.WriteAttributeString("String", _strings.GetString(exp.StringAddress));
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
            string folder = "Compiler_Debug";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            var path = Path.Combine(folder, _cashefile.InternalName + "_Strings.bin");
            using (EndianWriter writer = new EndianWriter(new FileStream(path, FileMode.Create), Endian.BigEndian))
            {
                _strings.Write(writer);
            }

        }

        /// <summary>
        /// Increments the current Datum.
        /// </summary>
        private void IncrementDatum()
        {
            _currentExpressionIndex++;
            _currentSalt++;

            if (_currentSalt == 0xFFFF)
                _currentSalt = 0x8000;
        }

        private void DeclarationsToXML()
        {
            string folder = "Compiler_Debug";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string fileName = Path.Combine(folder, _cashefile.InternalName + "_Declarations.xml");

            var settings = new XmlWriterSettings();
            settings.Indent = true;
            using (var writer = XmlWriter.Create(fileName, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("declarations");
                
                // globals
                writer.WriteStartElement("globals");
                foreach (ScriptGlobal glo in _globals)
                {
                    writer.WriteStartElement("global");
                    writer.WriteAttributeString("Name", glo.Name);
                    writer.WriteAttributeString("RetType", glo.Type.ToString());
                    writer.WriteAttributeString("Salt", glo.ExpressionIndex.Salt.ToString());
                    writer.WriteAttributeString("Index", glo.ExpressionIndex.Index.ToString());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                // scripts
                writer.WriteStartElement("scripts");
                foreach (Script scr in _scripts)
                {
                    writer.WriteStartElement("script");
                    writer.WriteAttributeString("Name", scr.Name);
                    writer.WriteAttributeString("Type", scr.ExecutionType.ToString());
                    writer.WriteAttributeString("RetType", scr.ReturnType.ToString());
                    writer.WriteAttributeString("Salt", scr.RootExpressionIndex.Salt.ToString());
                    writer.WriteAttributeString("Index", scr.RootExpressionIndex.Index.ToString());

                    if (scr.Parameters.Count > 0)
                    {
                        foreach (var p in scr.Parameters)
                        {
                            writer.WriteStartElement("parameter");
                            writer.WriteAttributeString("Name", p.Name);
                            writer.WriteAttributeString("Type", p.Type.ToString());
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
                var index = _openDatums.Pop();
                Debug.Print($"Close! Index: {index}");
                _expressions[index].NextExpression = DatumIndex.Null;
            }

            else
                throw new InvalidOperationException("Failed to close a datum. The Datum stack is empty.");
        }

        /// <summary>
        /// Pops an index from the Datum stack and links the open Datum index to the current expression.
        /// </summary>
        private void LinkDatum()
        {
            if (_openDatums.Count > 0)
            {
                var index = _openDatums.Pop();
                Debug.Print($"Link! Index: {index}");
                if (index != -1)        // -1 means that this expression belongs to a global declaration
                    _expressions[index].NextExpression = new DatumIndex(_currentSalt, _currentExpressionIndex);
            }

            else
                throw new InvalidOperationException("Failed to link a datum. The Datum stack is empty.");
        }

        /// <summary>
        /// Pushes the current Datum index to the Datum stack and adds the expression to the table.
        /// </summary>
        /// <param name="expression"></param>
        private void OpenDatumAndAdd(ExpressionBase expression)
        {
            Int32 openIndex = _expressions.Count;
            Debug.Print($"Open! Index: {openIndex}");
            _openDatums.Push(openIndex);
            _expressions.Add(expression);
        }

        /// <summary>
        /// Calculates the salt value of an index and returns it.
        /// </summary>
        /// <param name="index">The index belonging to the salt value.</param>
        /// <returns>The salt value.</returns>
        private ushort IndexToSalt(ushort index)
        {
            ushort init = 0xE373;           // initial value for the expression reflexive
            ushort restart = 0x7FFF;        // the value which is used to calculate the salt once it exceeds 0xFFFF

            var salt = init + index;

            if (salt >= ushort.MaxValue)
                salt = (salt % restart) + restart;

            return (ushort)salt;
        }

        private void PushTypes(params string[] types)
        {
            var rev = types.Reverse();
            foreach(string type in rev)
            {
                Debug.WriteLine($"Type Push: {type}");
                _expectedTypes.Push(type);
            }
        }

        private void PushTypes(string type, int count)
        {
            for(int i = 0; i < count; i++)
            {
                Debug.WriteLine($"Type Push: {type}");
                _expectedTypes.Push(type);
            }
        }

        private string PopType()
        {
            string str = _expectedTypes.Pop();
            Debug.WriteLine($"Type Pop: {str}");
            return str;
        }
    }
}
