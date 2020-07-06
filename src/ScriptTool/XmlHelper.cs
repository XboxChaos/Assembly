using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Blamite.Blam;
using Blamite.Blam.Scripting;
using Blamite.Serialization;
using Blamite.Serialization.Settings;
using Blamite.IO;

namespace ScriptTool
{
    public static class XmlHelper
    {
        public static void UpdateFunctionOpcodes(IDictionary<int, string> functions, string filePath)
        {
            XDocument doc = XDocument.Load(filePath);
            var xmlFunctions = doc.Descendants("function");
            foreach (var func in functions)
            {
                string op = "0x" + func.Key.ToString("X3");
                XElement matchingFunc = xmlFunctions.Single(e => e.Attribute("opcode").Value == op);
                matchingFunc.SetAttributeValue("name", func.Value);
            }

            doc.Save(filePath);
        }

        public static void UpdateEngineGlobals(IDictionary<int, string> functions, string filePath)
        {
            XDocument doc = XDocument.Load(filePath);
            var xmlFunctions = doc.Descendants("globals");
            foreach (var func in functions)
            {
                string op = "0x" + func.Key.ToString("X3");
                XElement matchingFunc = xmlFunctions.Single(e => e.Attribute("opcode").Value == op);
                matchingFunc.SetAttributeValue("name", func.Value);
            }

            doc.Save(filePath);
        }

        public static void FunctionOpcodesToXml(IDictionary<int, string> functions, string filePath)
        {
            if (functions.Count > 0)
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true
                };

                using (var writer = XmlWriter.Create(filePath, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("functions");

                    foreach (var func in functions)
                    {
                        writer.WriteStartElement("function");
                        writer.WriteAttributeString("opcode", "0x" + func.Key.ToString("X3"));
                        writer.WriteAttributeString("name", func.Value);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Close();
                }
            }
        }

        public static void EngineGlobalOpcodesToXml(IDictionary<int, string> functions, string filePath)
        {
            if (functions.Count > 0)
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true
                };

                using (var writer = XmlWriter.Create(filePath, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("globals");

                    foreach (var func in functions)
                    {
                        writer.WriteStartElement("global");
                        writer.WriteAttributeString("opcode", "0x" + func.Key.ToString("X3"));
                        writer.WriteAttributeString("name", func.Value);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Close();
                }
            }
        }

        public static void AllExpressionsToXml(Dictionary<string, ScriptTable> scriptFiles, string filePath)
        {
            if (scriptFiles.Count > 0)
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true
                };

                using (var writer = XmlWriter.Create(filePath, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("ScriptFiles");

                    foreach(var hsc in scriptFiles)
                    {
                        // Write file name.
                        writer.WriteStartElement("hsc");
                        writer.WriteAttributeString("Name", hsc.Key);
                        foreach(var expr in hsc.Value.Expressions.ExpressionsAsReadonly)
                        {
                            WriteExpression(writer, expr);
                        }
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Close();
                }
            }
        }

        public static void FilteredExpressionsToXml(Dictionary<string, ScriptExpression[]> expressions, string filePath)
        {
            if (expressions.Count > 0)
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true
                };

                using (var writer = XmlWriter.Create(filePath, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Maps");
                    foreach(var map in expressions)
                    {
                        // Write map name.
                        writer.WriteStartElement(map.Key);

                        // Write the expressions.
                        foreach(var expr in map.Value)
                        {
                            WriteExpression(writer, expr);
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Close();
                }
            }
        }

        private static void WriteExpression(XmlWriter writer, ScriptExpression expression)
        {
            writer.WriteStartElement("Expression");
            writer.WriteAttributeString("Num", expression.Index.Index.ToString("X4"));
            writer.WriteAttributeString("Salt", expression.Index.Salt.ToString("X4"));
            writer.WriteAttributeString("Opcode", expression.Opcode.ToString("X4"));
            writer.WriteAttributeString("ValueType", expression.ReturnType.ToString("X4"));
            switch (expression.Type)
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
            writer.WriteAttributeString("NextSalt", expression.Next.Salt.ToString("X4"));
            writer.WriteAttributeString("NextIndex", expression.Next.Index.ToString("X4"));
            writer.WriteAttributeString("StringOff", expression.StringOffset.ToString("X"));
            writer.WriteAttributeString("String", expression.StringValue);
            writer.WriteAttributeString("Value", expression.Value.ToString("X8"));
            writer.WriteAttributeString("LineNum", expression.LineNumber.ToString("X4"));
            writer.WriteEndElement();
        }
    }
}
