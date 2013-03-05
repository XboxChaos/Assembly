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
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: mapexpand <map file> <page count>");
                Console.WriteLine("Pages are multiples of 0x10000 bytes.");
                return;
            }

            int pageCount;
            if (!int.TryParse(args[1], out pageCount) || pageCount <= 0)
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

            Console.WriteLine("- Engine version: {0}", version.BuildString);
            Console.WriteLine("- Internal name: {0}", cacheFile.InternalName);
            Console.WriteLine("- Scenario name: {0}", cacheFile.ScenarioName);
            Console.WriteLine("- File size: 0x{0:X}", cacheFile.FileSize);
            Console.WriteLine("- Virtual size: 0x{0:X}", cacheFile.MetaArea.Size);
            for (int i = 0; i < cacheFile.Partitions.Length; i++)
            {
                var partition = cacheFile.Partitions[i];
                if (partition.BasePointer != null)
                    Console.WriteLine("  - Partition {0} at 0x{1:X}-0x{2:X} (size=0x{3:X})", i, partition.BasePointer.AsPointer(), partition.BasePointer.AsPointer() + partition.Size - 1, partition.Size);
            }
            Console.WriteLine("- Meta pointer mask: 0x{0:X}", cacheFile.MetaArea.PointerMask);
            Console.WriteLine("- Locale pointer mask: 0x{0:X}", (uint)-cacheFile.LocaleArea.PointerMask);
            Console.WriteLine("- String pointer mask: 0x{0:X}", cacheFile.StringArea.PointerMask);
            Console.WriteLine();

            Console.WriteLine("Injecting empty pages...");

            int injectSize = pageCount * 0x10000;
            Console.WriteLine("- Start address: 0x{0:X} (offset 0x{1:X})", cacheFile.MetaArea.BasePointer - injectSize, cacheFile.MetaArea.Offset);

            cacheFile.MetaArea.Resize(cacheFile.MetaArea.Size + injectSize, stream);

            Console.WriteLine();
            Console.WriteLine("Adjusting the header...");

            cacheFile.SaveChanges(stream);

            Console.WriteLine();
            Console.WriteLine("Successfully injected 0x{0:X} bytes at 0x{1:X} (offset 0x{2:X}).", injectSize, cacheFile.MetaArea.BasePointer, cacheFile.MetaArea.Offset);

            stream.Close();
        }
    }
}
