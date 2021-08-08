using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using Blamite.Blam;
using Blamite.Blam.Scripting;
using Blamite.Serialization;
using Blamite.Serialization.Settings;
using Blamite.IO;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Diagnostics;

namespace ScriptTool
{
    public class MapDatadumper
    {
        private readonly string _exeDir;
        private readonly string _formatsDir;
        private readonly string _supportedBuildsPath;
        private readonly EngineDatabase _db;

        public MapDatadumper()
        {
            _exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _formatsDir = Path.Combine(_exeDir, "Formats");
            _supportedBuildsPath = Path.Combine(_formatsDir, "Engines.xml");
            _db = XMLEngineDatabaseLoader.LoadDatabase(_supportedBuildsPath);
        }

        public void DumpUniqueStringIDs(string[] mapPaths, string outputPath)
        {
            HashSet<string> ids = new HashSet<string>();
            IEnumerable<ICacheFile> caches = MapLoader.LoadAllMaps(mapPaths, _db);
            foreach(var cache in caches)
            {
                if(cache.StringIDs is null)
                {
                    Console.WriteLine($"{Path.GetFileName(cache.FilePath)} doesn't contain StringIDs and will be skipped.");
                }
                else
                {
                    Console.WriteLine($"Collecting StringIDs from {Path.GetFileName(cache.FilePath)}.");
                    IEnumerable<string> filtered = cache.StringIDs?.Select(t => t.Replace("\u001f", "\t"));
                    foreach (string id in filtered)
                    {
                        ids.Add(id);
                    }
                }
            }
            var sorted = ids.ToList();
            sorted.Sort();
            XmlHelper.StringIDsToXml(sorted, outputPath);
            Console.WriteLine($"\nAll StringIDs have been saved to {outputPath}.");
        }
    }
}
