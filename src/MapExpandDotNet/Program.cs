using System;
using System.IO;
using Blamite.Blam;
using Blamite.Blam.ThirdGen;
using Blamite.Serialization.Settings;
using Blamite.IO;

namespace MapExpandDotNet
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			if (args.Length != 3)
			{
				Console.WriteLine("Usage: mapexpand <map file> <section> <page count>");
				Console.WriteLine();
				Console.WriteLine("Available sections:");
				Console.WriteLine("  stringidindex");
				Console.WriteLine("  stringiddata");
				Console.WriteLine("  tagnameindex");
				Console.WriteLine("  tagnamedata");
				Console.WriteLine("  resource");
				Console.WriteLine("  tag");
				return;
			}

			int pageCount;
			if (!int.TryParse(args[2], out pageCount) || pageCount <= 0)
			{
				Console.WriteLine("The page count must be a positive integer.");
				return;
			}

			Console.WriteLine("Reading...");

			var stream = new EndianStream(File.Open(args[0], FileMode.Open, FileAccess.ReadWrite), Endian.BigEndian);

			var database = XMLEngineDatabaseLoader.LoadDatabase("Formats/Engines.xml");
			var buildInfo = CacheFileLoader.FindEngineDescription(stream, database);

			if (buildInfo.Engine != EngineType.ThirdGeneration)
			{
				Console.WriteLine("Only third-generation map files are supported.");
				return;
			}

			var cacheFile = new ThirdGenCacheFile(stream, buildInfo, args[0]);

			FileSegmentGroup area;
			FileSegment section;
			int pageSize;
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

			Console.WriteLine("- Engine version: {0}", buildInfo.BuildVersion);
			Console.WriteLine("- Internal name: {0}", cacheFile.InternalName);
			Console.WriteLine("- Scenario name: {0}", cacheFile.ScenarioName);
			Console.WriteLine();

			Console.WriteLine("Injecting empty pages...");

			uint injectSize = (uint)(pageCount * pageSize);
			section.Resize(section.Size + injectSize, stream);

			Console.WriteLine("Adjusting the header...");

			cacheFile.SaveChanges(stream);
			stream.Dispose();

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