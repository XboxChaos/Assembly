using System;
using System.IO;
using System.Reflection;
using System.Text;
using Blamite;
using Blamite.Blam;
using Blamite.Blam.Scripting;
using Blamite.Serialization;
using Blamite.Serialization.Settings;
using Blamite.IO;
using System.CodeDom.Compiler;
using Blamite.Util;

namespace ScriptWalker
{
    class Program
    {

        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (args.Length != 3)
            {
                Console.WriteLine("Usage: ScriptWalker <unmodified map> <modified map> <output dir>");
                return;
            }

            string origPath = args[0];
            string modPath = args[1];
            string outputDir = args[2];

            if (!File.Exists(origPath))
            {
                Console.WriteLine($"The path to the original map file is invalid \"{origPath}\".");
                return;
            }

            if (!File.Exists(modPath))
            {
                Console.WriteLine($"The path to the modified map file is invalid \"{modPath}\".");
                return;
            }

            if (!Directory.Exists(outputDir))
            {
                Console.WriteLine($"The output directory \"{outputDir}\" doesn't exist.");
                return;
            }

            string origName = Path.GetFileNameWithoutExtension(origPath);
            string modName = Path.GetFileNameWithoutExtension(modPath);
            string outputName = modName + "_to_" + origName + ".txt";
            string outputPath = Path.Combine(outputDir, outputName);

            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string formatsDir = Path.Combine(exeDir, "Formats");
            string supportedBuildsPath = Path.Combine(formatsDir, "Engines.xml");

            EngineDatabase db = XMLEngineDatabaseLoader.LoadDatabase(supportedBuildsPath);
            EngineDescription origDesc;
            EngineDescription modDesc;

            Console.WriteLine("Loading the original, unmodified map...");
            var origScripts = LoadScripts(origPath, db, out origDesc);
            Console.WriteLine("Done");
            Console.WriteLine();

            Console.WriteLine("Loading the modified map...");
            var modScripts = LoadScripts(modPath, db, out modDesc);
            Console.WriteLine("Done");
            Console.WriteLine();

            using (var writer = File.CreateText(outputPath))
            {
                var txtWriter = new IndentedTextWriter(writer, "   ");
                ScriptWalker walker = new ScriptWalker(origScripts, modScripts, txtWriter, origDesc);
                Console.WriteLine("Comparing the two maps...");
                walker.Analyze();
                Console.WriteLine("Done");
                Console.WriteLine();
            }

            Console.WriteLine($"The output was written to \"{outputPath}\".");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static ScriptTable LoadScripts(string path, EngineDatabase db, out EngineDescription desc)
        {
            using (var stream = File.OpenRead(path))
            {
                EngineDescription engine;

                var reader = new EndianReader(stream, Endian.BigEndian);
                var cache = CacheFileLoader.LoadCacheFile(reader, db, out engine);
                desc = engine;
                Console.WriteLine($"File: {path} | Endianness: {reader.Endianness}");
                var scripts = cache.ScriptFiles[0].LoadScripts(reader);

                return scripts;
            }
        }
    }
}
