using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using Blamite.Blam;
using Blamite.Blam.Scripting;
using Blamite.Serialization;
using Blamite.Serialization.Settings;
using Blamite.IO;
using System.Text;
using System.Xml.Linq;


namespace FunctionOpcodeDumper
{
    class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (args.Length != 1)
            {
                Console.WriteLine("Usage: FunctionOpcodeDumper <map directory>. " +
                    "To update a Scripting.xml, place it in the same folder as this program.");
                return;
            }

            string mapsPath = args[0];

            if (!Directory.Exists(mapsPath))
            {
                Console.WriteLine($"The maps directory \"{mapsPath}\" doesn't exist.");
                return;
            }

            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string formatsDir = Path.Combine(exeDir, "Formats");
            string supportedBuildsPath = Path.Combine(formatsDir, "Engines.xml");
            EngineDatabase db = XMLEngineDatabaseLoader.LoadDatabase(supportedBuildsPath);

            SortedDictionary<int, string> functions = new SortedDictionary<int, string>();

            string[] maps = Directory.GetFiles(mapsPath, "*.map");

            if(maps.Length == 0)
            {
                Console.WriteLine($"The directory \"{mapsPath}\" doesn't contain any .map files.");
                return;
            }

            // search the opcodes...
            Console.WriteLine("Searching Opcodes...");
            SortedDictionary<int, string> opcodes = SearchOpcodes(maps, db);

            // save the opcodes to an xml document...
            Console.WriteLine("\nSaving opcodes...\n");
            string outputPath = Path.Combine(exeDir, "opcodes.xml");
            WriteFunctionsToXML(functions, outputPath);
            Console.WriteLine($"The functions and their opcodes have been saved to {outputPath}.");

            // update existing scripting definitions...
            string[] xmls = Directory.GetFiles(exeDir, "*_Scripting.xml");
            if(xmls.Length == 1)
            {
                Console.WriteLine("\nA scripting Xml-File was found. Its function names will be updated.");
                UpdateXML(opcodes, xmls[0]);
                Console.WriteLine($"The file {Path.GetFileName(xmls[0])} was updated.");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static ScriptExpressionTable LoadExpressions(string path, EngineDatabase db, out EngineDescription desc)
        {
            using (var stream = File.OpenRead(path))
            {
                EngineDescription engine;

                var reader = new EndianReader(stream, Endian.BigEndian);
                var cache = CacheFileLoader.LoadCacheFile(reader, db, out engine);
                desc = engine;
                if(cache.Type != CacheFileType.Shared && cache.Type != CacheFileType.SinglePlayerShared && cache.ScriptFiles.Length > 0)
                {
                    var scripts = cache.ScriptFiles[0].LoadScripts(reader);
                    return scripts.Expressions;
                }
                else
                {
                    return null;
                }
            }
        }

        private static SortedDictionary<int, string> SearchOpcodes(string[] mapPaths, EngineDatabase db)
        {
            SortedDictionary<int, string> result = new SortedDictionary<int, string>();

            for (int i = 1; i <= mapPaths.Length; i++)
            {
                string path = mapPaths[i - 1];
                string file = Path.GetFileName(path);

                var expressions = LoadExpressions(path, db, out _);
                if (expressions == null)
                    continue;

                foreach (ScriptExpression exp in expressions.ExpressionsAsReadonly)
                {
                    if (exp.Type == ScriptExpressionType.Group)
                    {
                        DatumIndex index = new DatumIndex(exp.Value);
                        ScriptExpression name = expressions.FindExpression(index);

                        if (exp.Opcode != name.Opcode)
                        {
                            Console.WriteLine($"Warning: Non-matching opcodes! File: {file} Expression: {exp.Index.Index}");
                        }

                        if (result.TryGetValue(name.Opcode, out string funcName))
                        {
                            if (funcName != name.StringValue)
                                Console.WriteLine($"\nWarning: Duplicate opcodes! Opcode: {name.Opcode.ToString("X3")} Name: \"{name.StringValue}\" " +
                                    $"File: {file} Expression: {exp.Index.Index}\n");
                        }
                        else
                        {
                            result[name.Opcode] = name.StringValue;
                        }
                    }
                }
                double percentage = ((double)i / mapPaths.Length) * 100;
                Console.WriteLine($"{Math.Round(percentage, 2)}% - File: {file}");
            }

            return result;
        }

        private static void WriteFunctionsToXML(SortedDictionary<int, string> functions, string filePath)
        {
            if (functions.Count > 0)
            {
                var settings = new XmlWriterSettings();
                settings.Indent = true;

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

        private static void UpdateXML(SortedDictionary<int, string> functions, string filePath)
        {
            XDocument doc = XDocument.Load(filePath);
            var xmlFunctions = doc.Descendants("function");
            foreach(var func in functions)
            {
                string op = "0x" + func.Key.ToString("X3");
               // var matchingFunc = funcs.Elements("function").Where(e => e.Attribute("opcode").Value == op);
                XElement matchingFunc = xmlFunctions.Single(e => e.Attribute("opcode").Value == op);
                matchingFunc.SetAttributeValue("name", func.Value);
            }

            doc.Save(filePath);
        }
    }
}
