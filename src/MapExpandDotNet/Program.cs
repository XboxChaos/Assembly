using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Blamite.Blam;
using Blamite.Blam.ThirdGen;
using Blamite.Flexibility;
using Blamite.IO;

namespace MapExpandDotNet
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2 && args.Length != 3)
            {
                Console.WriteLine("Usage: mapexpand <map file> [section] <page count>");
                Console.WriteLine();
                Console.WriteLine("Available sections:");
                Console.WriteLine("  stringidindex");
                Console.WriteLine("  stringiddata");
                Console.WriteLine("  tagnameindex");
                Console.WriteLine("  tagnamedata");
                Console.WriteLine("  resource");
                Console.WriteLine("  tag");
                Console.WriteLine();
                Console.WriteLine("Section defaults to \"tag\" if not specified.");
                return;
            }

            int pageCount;
            if (!int.TryParse(args[args.Length - 1], out pageCount) || pageCount <= 0)
            {
                Console.WriteLine("The page count must be a positive integer.");
                return;
            }

            Console.WriteLine("Reading...");

            EndianStream stream = new EndianStream(File.Open(args[0], FileMode.Open, FileAccess.ReadWrite), Endian.BigEndian);
            CacheFileVersionInfo version = new CacheFileVersionInfo(stream);
            if (version.Engine != EngineType.ThirdGeneration)
            {
                Console.WriteLine("Only third-generation map files are supported.");
                return;
            }

            BuildInfoLoader infoLoader = new BuildInfoLoader(XDocument.Load("Formats/SupportedBuilds.xml"), "Formats/");
            BuildInformation buildInfo = infoLoader.LoadBuild(version.BuildString);
            ThirdGenCacheFile cacheFile = new ThirdGenCacheFile(stream, buildInfo, version.BuildString);

            var area = cacheFile.MetaArea;
            var section = cacheFile.MetaArea.Segments[0];
            int pageSize = 0x10000;
            if (args.Length == 3)
            {
                switch (args[1])
                {
                    case "stringidindex":
                        area = cacheFile.StringArea;
                        section = cacheFile.StringIDIndexTable;
                        pageSize = 0x1000;
                        break;
                    case "stringiddata":
                        area = cacheFile.StringArea;
                        section = cacheFile.StringIDDataTable;
                        pageSize = 0x1000;
                        break;
                    case "tagnameindex":
                        area = cacheFile.StringArea;
                        section = cacheFile.FileNameIndexTable;
                        pageSize = 0x1000;
                        break;
                    case "tagnamedata":
                        area = cacheFile.StringArea;
                        section = cacheFile.FileNameDataTable;
                        pageSize = 0x1000;
                        break;
                    case "resource":
                        area = null;
                        section = cacheFile.RawTable;
                        pageSize = 0x1000;
                        break;
                    case "tag":
                        area = cacheFile.MetaArea;
                        section = cacheFile.MetaArea.Segments[0];
                        pageSize = 0x10000;
                        break;
                    default:
                        Console.WriteLine("Invalid section name: \"{0}\"", args[1]);
                        return;
                }
            }

            Console.WriteLine("- Engine version: {0}", version.BuildString);
            Console.WriteLine("- Internal name: {0}", cacheFile.InternalName);
            Console.WriteLine("- Scenario name: {0}", cacheFile.ScenarioName);
            Console.WriteLine();

            Console.WriteLine("Injecting empty pages...");

            int injectSize = pageCount * pageSize;
            section.Resize(section.Size + injectSize, stream);

            Console.WriteLine("Adjusting the header...");

            cacheFile.SaveChanges(stream);
            stream.Close();

            Console.WriteLine();

            var offset = section.Offset;
            if (section.ResizeOrigin == SegmentResizeOrigin.End)
                offset += section.ActualSize - injectSize;

            if (area != null)
                Console.WriteLine("Successfully injected 0x{0:X} bytes at 0x{1:X} (offset 0x{2:X}).", injectSize, area.BasePointer, offset);
            else
                Console.WriteLine("Successfully injected 0x{0:X} bytes at offset 0x{1:X}.", injectSize, offset);
        }
    }
}
