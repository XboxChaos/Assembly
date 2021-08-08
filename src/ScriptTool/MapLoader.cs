using System.Collections.Generic;
using Blamite.Blam;
using Blamite.Blam.Scripting;
using Blamite.Serialization;
using System.Linq;
using Blamite.IO;
using System.IO;
using Blamite.Blam.Util;
using Blamite.Blam.ThirdGen.Structures;

namespace ScriptTool
{
    public static class MapLoader
    {
        public static IEnumerable<ICacheFile> LoadAllMaps(string[] paths, EngineDatabase db)
        {
            List<ICacheFile> result = new List<ICacheFile>();
            foreach(string path in paths)
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                var reader = new EndianReader(stream, Endian.BigEndian);
                var cache = CacheFileLoader.LoadCacheFile(reader, path, db);
                result.Add(cache);
            }
            return result;
        }


        public static Dictionary<string, ScriptTable> LoadAllScriptFiles(string path, EngineDatabase db, out EngineDescription engineInfo)
        {
            Dictionary<string, ScriptTable> result = new Dictionary<string, ScriptTable>();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var reader = new EndianReader(stream, Endian.BigEndian);
                var cache = CacheFileLoader.LoadCacheFile(reader, path, db, out engineInfo);
                if (cache.Type != CacheFileType.Shared && cache.Type != CacheFileType.SinglePlayerShared && cache.ScriptFiles.Length > 0)
                {
                    foreach(var file in cache.ScriptFiles)
                    {
                        result.Add(file.Name, file.LoadScripts(reader));
                    }
                }
            }
            return result;
        }


        public static Dictionary<string, IEnumerable<UnitSeatMapping>> LoadAllSeatMappings(string path, EngineDatabase db, out EngineDescription engineInfo)
        {
            Dictionary<string, IEnumerable<UnitSeatMapping>> result = new Dictionary<string, IEnumerable<UnitSeatMapping>>();

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var reader = new EndianReader(stream, Endian.BigEndian);
                var cache = CacheFileLoader.LoadCacheFile(reader, path, db, out engineInfo);
                if (cache.Type != CacheFileType.Shared && cache.Type != CacheFileType.SinglePlayerShared && cache.ScriptFiles.Length > 0)
                {
                    foreach (var file in cache.ScriptFiles)
                    {
                        if(file is ScnrScriptFile scnr)
                        {
                            var mappings = SeatMappingNameExtractor.ExtractScnrSeatMappings(scnr, reader, engineInfo.ScriptInfo);

                            if (mappings.Any())
                            {
                                result[file.Name] = mappings;
                            }
                        }
                    }
                }
            }

            return result;
        }

    }
}
