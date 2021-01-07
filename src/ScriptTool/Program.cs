﻿using System;
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


namespace ScriptTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Parser.Default.ParseArguments<DumpOptions, FunctionOptions, EngineGlobalsOptions>(args)
                .WithParsed<DumpOptions>(RunDump)
                .WithParsed<FunctionOptions>(RunFunctions)
                .WithParsed<EngineGlobalsOptions>(RunEngineGlobals)
                .WithNotParsed(HandleParseError);
        }

        static void RunDump(DumpOptions options)
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] maps = Directory.GetFiles(options.MapFolder, "*.map");
            string outputFolder = options.OutputFolder == null ? exeDir : options.OutputFolder;
            ScriptDumper scriptDumper = new ScriptDumper();
            MapDatadumper mapDatadumper = new MapDatadumper();

            if (!Directory.Exists(options.MapFolder))
            {
                Console.WriteLine($"The maps directory \"{options.MapFolder}\" doesn't exist.");
                return;
            }

            if (maps.Length == 0)
            {
                Console.WriteLine($"The directory \"{options.MapFolder}\" doesn't contain any .map files.");
                return;
            }

            if (!Directory.Exists(outputFolder))
            {
                Console.WriteLine($"The output directory \"{outputFolder}\" doesn't exist. Please check your spelling.");
                return;
            }

            Console.WriteLine($"{maps.Length} maps found.\n");

            // Dump Expressions.
            if(options.DumpExpressions != null)
            {
                if (options.DumpExpressions == "ALL")
                {
                    scriptDumper.DumpExpressionsAll(maps, outputFolder);
                }
                else
                {
                    string outputFile = Path.Combine(outputFolder, options.DumpExpressions + "_expressions.xml");
                    scriptDumper.DumpExpressionsType(maps, options.DumpExpressions, outputFile);
                }
            }

            // Dump StringIDs.
            if(options.DumpStringIDs == true)
            {
                string outputFile = Path.Combine(outputFolder, "StringIDs.xml");
                mapDatadumper.DumpUniqueStringIDs(maps, outputFile);
            }

            // Dump Unit Seat Mappings.
            if(options.DumpUnitSeatMappings == true)
            {
                scriptDumper.DumpUnitSeatMappings(maps, outputFolder);
            }

            // Dump Type Casts
            if(options.DumpTypeCasts == true)
            {
                scriptDumper.DumpTypeCasts(maps, outputFolder);
            }
        }

        static void RunFunctions(FunctionOptions options)
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputFilePath = options.OutputFolder != null ? Path.Combine(options.OutputFolder, "function_opcodes.xml") : Path.Combine(exeDir, "function_opcodes.xml");

            if (!Directory.Exists(options.MapFolder))
            {
                Console.WriteLine($"The maps directory \"{options.MapFolder}\" doesn't exist.");
                return;
            }

            string[] maps = Directory.GetFiles(options.MapFolder, "*.map");
            if (maps.Length == 0)
            {
                Console.WriteLine($"The directory \"{options.MapFolder}\" doesn't contain any .map files.");
                return;
            }

            // Extract the opcodes from the map files.
            Console.WriteLine($"{maps.Length} maps found. Extracting function opcodes...");
            var extractor = new OpcodeExtractor();
            var opcodes = extractor.ExtractFunctionOpcodes(maps);

            // Save the opcodes to a file.
            XmlHelper.FunctionOpcodesToXml(opcodes, outputFilePath);
            Console.WriteLine($"\nThe extracted function opcodes were saved to {outputFilePath}.");

            // Update an existing _scripting.xml file with the extracted opcodes.
            if (options.UpdateFile != null)
            {
                if (!Path.GetFileName(options.UpdateFile).EndsWith("_Scripting.xml"))
                {
                    Console.WriteLine("\nWARNING: The Xml file, which is supposed to be updated with the extracted opcodes, doesn't appear to be an Assembly Scripting Definitions document." +
                        "These files have the ending \"_scripting.xml\" and can be found in the Formats folder. The operation will be skipped.");
                }
                else
                {
                    int updated = XmlHelper.UpdateFunctionOpcodes(opcodes, options.UpdateFile);
                    string message = updated == 0 ? "No Opcodes were updated. The file was already up to date." : $"{updated} Opcodes have been updated with the extracted ones.";
                    Console.WriteLine(message);
                }
            }
        }

        static void RunEngineGlobals(EngineGlobalsOptions options)
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputFilePath = options.OutputFolder != null ? Path.Combine(options.OutputFolder, "engine_globals_opcodes.xml") : Path.Combine(exeDir, "engine_globals_opcodes.xml");

            if (!Directory.Exists(options.MapFolder))
            {
                Console.WriteLine($"The maps directory \"{options.MapFolder}\" doesn't exist.");
                return;
            }

            string[] maps = Directory.GetFiles(options.MapFolder, "*.map");
            if (maps.Length == 0)
            {
                Console.WriteLine($"The directory \"{options.MapFolder}\" doesn't contain any .map files.");
                return;
            }

            // Extract the opcodes from the map files.
            Console.WriteLine($"{maps.Length} maps found. Extracting engine global opcodes...");
            var extractor = new OpcodeExtractor();
            var opcodes = extractor.ExtractEngineGlobals(maps);

            // Save the opcodes to a file.
            XmlHelper.EngineGlobalOpcodesToXml(opcodes, outputFilePath);
            Console.WriteLine($"\nThe extracted engine global opcodes were saved to {outputFilePath}.");

            // Update an existing _scripting.xml file with the extracted opcodes.
            if (options.UpdateFile != null)
            {
                if (!Path.GetFileName(options.UpdateFile).EndsWith("_Scripting.xml"))
                {
                    Console.WriteLine("\nWARNING: The Xml file, which is supposed to be updated with the extracted opcodes, doesn't appear to be an Assembly Scripting Definitions document." +
                        "These files have the ending \"_scripting.xml\" and can be found in the Formats folder. The operation will be skipped.");
                }
                else
                {
                    int updated = XmlHelper.UpdateEngineGlobals(opcodes, options.UpdateFile);
                    string message = updated == 0 ? "No Opcodes were updated. The file was already up to date." : $"{updated} Opcodes have been updated with the extracted ones.";
                    Console.WriteLine(message);
                }
            }
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            //handle errors
        }

    }
}
