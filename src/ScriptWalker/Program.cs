using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Blamite;
using Blamite.Blam;
using Blamite.Blam.Scripting;
using Blamite.Serialization;
using Blamite.Serialization.Settings;
using Blamite.IO;
using System.CodeDom.Compiler;
using Blamite.Util;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ScriptWalker
{
    class Program
    {

        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (args.Length != 2 && args.Length != 3)
            {
                Console.WriteLine("Usage: ScriptWalker <unmodified map directory> <modified map directory> <[OPTIONAL] output directory>");
                return;
            }

            string cleanFolder = args[0];
            string modifiedFolder = args[1];
            string outputFolder = args.Length == 3 ? args[2] : Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (!Directory.Exists(cleanFolder))
            {
                Console.WriteLine($"The path to the folder containing the unmodied cache files is invalid: \"{cleanFolder}\".");
                return;
            }

            if (!Directory.Exists(modifiedFolder))
            {
                Console.WriteLine($"The path to the folder containing the modified map files is invalid: \"{modifiedFolder}\".");
                return;
            }

            if (!Directory.Exists(outputFolder))
            {
                Console.WriteLine($"The output directory \"{outputFolder}\" doesn't exist.");
                return;
            }

            WalkFiles(cleanFolder, modifiedFolder, outputFolder).Wait();
        }

        private static async Task WalkFiles(string cleanFolder, string modifiedFolder, string outputFolder)
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string formatsDir = Path.Combine(exeDir, "Formats");
            string supportedBuildsPath = Path.Combine(formatsDir, "Engines.xml");
            EngineDatabase db = XMLEngineDatabaseLoader.LoadDatabase(supportedBuildsPath);

            List<string> cleanFileNames = Directory.EnumerateFiles(cleanFolder, "*.map").Select(Path.GetFileName).ToList();
            List<string> modifiedFileNames = Directory.EnumerateFiles(modifiedFolder, "*.map").Select(Path.GetFileName).ToList();

            Console.WriteLine($"{modifiedFileNames.Count} modified cache files were found. Processing the files now.");
            List<Task> tasks = new List<Task>();

            foreach (string fileName in modifiedFileNames)
            {
                if (cleanFileNames.Contains(fileName))
                {
                    string cleanFile = Path.Combine(cleanFolder, fileName);
                    string modifiedFile = Path.Combine(modifiedFolder, fileName);
                    string outputName = Path.GetFileNameWithoutExtension(fileName) + "_walk.txt";
                    string outputFile = Path.Combine(outputFolder, outputName);
                    tasks.Add(Task.Run(() => WalkFile(cleanFile, modifiedFile, outputFile, db)));
                }
                else
                {
                    Console.WriteLine($"An unmodified cache file with the name \"{fileName}\" could not be found. The operation will be skipped.");
                }
            }

            await Task.WhenAll(tasks);
            Console.WriteLine("Finished processing the files.");
        }

        private static void WalkFile(string cleanFile, string modifiedFile, string outputFile, EngineDatabase db)
        {
            var cleanScripts = LoadScripts(cleanFile, db, out EngineDescription desc);
            var modifiedScripts = LoadScripts(modifiedFile, db, out _);

            using (var writer = File.CreateText(outputFile))
            {
                var txtWriter = new IndentedTextWriter(writer);
                ScriptWalker walker = new ScriptWalker(cleanScripts, modifiedScripts, txtWriter, desc);
                walker.Analyze();
            }

            Console.WriteLine($"Compared {Path.GetFileName(modifiedFile)}.");
        }

        private static ScriptTable LoadScripts(string path, EngineDatabase db, out EngineDescription desc)
        {
            using (var stream = File.OpenRead(path))
            {
                EngineDescription engine;

                var reader = new EndianReader(stream, Endian.BigEndian);
                var cache = CacheFileLoader.LoadCacheFile(reader, Path.GetFileName(path), db, out engine);
                desc = engine;
                var scripts = cache.ScriptFiles[0].LoadScripts(reader);

                return scripts;
            }
        }
    }
}
