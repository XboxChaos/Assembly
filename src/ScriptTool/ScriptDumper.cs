using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using CommandLine;
using Blamite.Blam;
using Blamite.Blam.Scripting;
using Blamite.Serialization;
using Blamite.Serialization.Settings;
using Blamite.IO;
using System.Text;
using System.Xml.Linq;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScriptTool
{
    public class ScriptDumper
    {
        private readonly string _exeDir;
        private readonly string _formatsDir;
        private readonly string _supportedBuildsPath;
        private readonly EngineDatabase _db;

        public ScriptDumper()
        {
            _exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _formatsDir = Path.Combine(_exeDir, "Formats");
            _supportedBuildsPath = Path.Combine(_formatsDir, "Engines.xml");
            _db = XMLEngineDatabaseLoader.LoadDatabase(_supportedBuildsPath);
        }

        public void DumpExpressionsAll(string[] mapPaths, string outputDirectory)
        {
            foreach (string map in mapPaths)
            {
                Console.WriteLine($"Dumping the script expressions of {Path.GetFileName(map)}.");
                var scriptTables = MapLoader.LoadAllScriptFiles(map, _db, out EngineDescription engine);
                string fileName = Path.GetFileNameWithoutExtension(map) + "_Script_Expressions.xml";
                string outputPath = Path.Combine(outputDirectory, fileName);
                XmlHelper.AllExpressionsToXml(scriptTables, outputPath);
            }
            Console.WriteLine($"\nAll Script Expressions have been dumped.");
        }

        public void DumpExpressionsType(string[] mapPaths, string type, string outputPath)
        {
            Dictionary<string, ScriptExpression[]> mapExpressions = new Dictionary<string, ScriptExpression[]>();

            // Collect the data.
            foreach (string map in mapPaths)
            {
                var scriptTables = MapLoader.LoadAllScriptFiles(map, _db, out EngineDescription engine);
                ScriptValueType info = engine.ScriptInfo.GetTypeInfo(type);

                if(info is null)
                {
                    Console.WriteLine($"WARNING: \"{type}\" is not a valid expression value type. " +
                        $"A list of all valid value types can be found in the scripting xml files in the formats folder. " +
                        $"The operation will be skipped.");
                    return;
                }

                Console.WriteLine($"Collecting Expressions from {Path.GetFileName(map)}.");

                // Collect all matching expressions.
                List<ScriptExpression> matchingExpressions = new List<ScriptExpression>();

                if(info.Name == "function_name")
                {
                    foreach (ScriptTable data in scriptTables.Values)
                    {
                        var expressions = data.Expressions.ExpressionsAsReadonly.Where(expr =>
                        expr.Type == ScriptExpressionType.Expression && (expr.Opcode == info.Opcode || expr.ReturnType == info.Opcode));
                        matchingExpressions.AddRange(expressions);
                    }
                }
                else
                {
                    ScriptValueType funcNameInfo = engine.ScriptInfo.GetTypeInfo("function_name");
                    foreach (ScriptTable data in scriptTables.Values)
                    {
                        var expressions = data.Expressions.ExpressionsAsReadonly.Where(expr =>
                        expr.Type == ScriptExpressionType.Expression && expr.ReturnType != funcNameInfo.Opcode && (expr.Opcode == info.Opcode || expr.ReturnType == info.Opcode));
                        matchingExpressions.AddRange(expressions);
                    }
                }



                // Add the expressions to the collection.
                if(matchingExpressions.Count > 0)
                {
                    mapExpressions.Add(Path.GetFileNameWithoutExtension(map), matchingExpressions.ToArray());
                }
            }

            if(mapExpressions.Count > 0)
            {
                // Write the data to xml.
                XmlHelper.FilteredExpressionsToXml(mapExpressions, outputPath);
                Console.WriteLine($"\nAll {type} Expressions have been saved to {outputPath}.");
            }
            else
            {
                Console.WriteLine($"\nNo {type} Expressions were found in the map files.");
            }
        }

        public void DumpUnitSeatMappings(string[] mapPaths, string outputDirectory)
        {
            foreach(string path in mapPaths)
            {
                Console.WriteLine($"Dumping the unit seat mappings from {Path.GetFileName(path)}.");
                var mappings = MapLoader.LoadAllSeatMappings(path, _db, out _);
                string fileName = Path.GetFileNameWithoutExtension(path) + "_Unit_Seat_Mappings.xml";
                string outputPath = Path.Combine(outputDirectory, fileName);
                XmlHelper.SeatMappingsToXml(mappings, outputPath);
            }
        }
    }
}
