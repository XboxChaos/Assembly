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
using System.Security;
using System.Diagnostics;

namespace ScriptTool
{
    public static class XmlHelper
    {
        public static int UpdateFunctionOpcodes(IDictionary<int, string> functions, string filePath)
        {
            int updatedOpcodes = 0;
            XDocument doc = XDocument.Load(filePath);
            var xmlFunctions = doc.Descendants("function");
            foreach (var func in functions)
            {
                string funcName = func.Value;
                string op = "0x" + func.Key.ToString("X3");
                XElement matchingFunc = xmlFunctions.Single(e => e.Attribute("opcode").Value == op);
                if (matchingFunc.Attribute("name").Value != func.Value)
                {
                    Debug.WriteLine($"Functions: Overwriting {matchingFunc.Attribute("name").Value} with {func.Value}.");
                    matchingFunc.SetAttributeValue("name", func.Value);
                    updatedOpcodes++;
                }
            }
            doc.Save(filePath);
            return updatedOpcodes;
        }

        public static int UpdateEngineGlobals(IDictionary<int, string> functions, string filePath)
        {
            int updatedOpcodes = 0;
            XDocument doc = XDocument.Load(filePath);
            var xmlGlobals = doc.Descendants("global");
            foreach (var func in functions)
            {
                string op = "0x" + func.Key.ToString("X3");
                XElement matchingGlobal = xmlGlobals.Single(e => e.Attribute("opcode").Value == op);
                // If the names don't match, overwrite the old one with the new one.
                if(matchingGlobal.Attribute("name").Value != func.Value)
                {
                    Debug.WriteLine($"Globals: Overwriting {matchingGlobal.Attribute("name").Value} with {func.Value}.");
                    matchingGlobal.SetAttributeValue("name", func.Value);
                    updatedOpcodes++;
                }
            }
            doc.Save(filePath);
            return updatedOpcodes;
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
                    writer.WriteStartElement("Map_" + map.Key);

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

        public static void StringIDsToXml(IEnumerable<string> ids, string filePath)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.Unicode
            };

            using var writer = XmlWriter.Create(filePath, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("StringIDs");
            foreach (var id in ids)
            {
                XElement idElement = new XElement("StringID", id);
                idElement.WriteTo(writer);
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();
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
            writer.WriteAttributeString("Value", expression.Value.ToString());
            writer.WriteAttributeString("LineNum", expression.LineNumber.ToString("X4"));
            writer.WriteEndElement();
        }

        public static void SeatMappingsToXml(Dictionary<string, IEnumerable<UnitSeatMapping>> mappings, string filePath)
        {
            if (mappings.Count > 0)
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true
                };

                using (var writer = XmlWriter.Create(filePath, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("ScriptFiles");

                    foreach (var mapping in mappings)
                    {
                        // Write file name.
                        writer.WriteStartElement("hsc");
                        writer.WriteAttributeString("Name", mapping.Key);
                        foreach (UnitSeatMapping m in mapping.Value)
                        {
                            writer.WriteStartElement("Mapping");
                            writer.WriteAttributeString("Index", m.Index.ToString());
                            writer.WriteAttributeString("Name", m.Name);
                            writer.WriteAttributeString("Count", m.Count.ToString());
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Close();
                }
            }
        }

        public static void TypeCastsToXml(Dictionary<string, List<string>> casts, string filePath)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true
            };

            using (var writer = XmlWriter.Create(filePath, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("typecasting");
                foreach (KeyValuePair<string, List<string>> to in casts)
                {
                    writer.WriteStartElement("to");
                    writer.WriteAttributeString("name", to.Key);

                    foreach (string from in to.Value)
                    {
                        writer.WriteStartElement("from");
                        writer.WriteString(from);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
            }
        }
    }
}
