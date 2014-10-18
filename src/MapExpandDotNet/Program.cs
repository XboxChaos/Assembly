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
			if (args.Length != 3 && args.Length != 4)
			{
				CustomConsole.WriteLine("Usage: mapexpand <map file> <section> <page count> <verbose*>");
				CustomConsole.WriteLine();
				CustomConsole.WriteLine("Available sections:");
				CustomConsole.WriteLine("  stringidindex");
				CustomConsole.WriteLine("  stringiddata");
				CustomConsole.WriteLine("  tagnameindex");
				CustomConsole.WriteLine("  tagnamedata");
				CustomConsole.WriteLine("  resource");
				CustomConsole.WriteLine("  tag");
				CustomConsole.WriteLine();
				CustomConsole.WriteLine("*Verbose Options: (Optional, default is 0)");
				CustomConsole.WriteLine("  0 - Turns off logging to a local text file.");
				CustomConsole.WriteLine("  1 - Turns on logging to a local text file.");
				CustomConsole.WriteLine();
				return;
			}

			int pageCount;
			if (!int.TryParse(args[2], out pageCount) || pageCount <= 0)
			{
				CustomConsole.WriteLine("The page count must be a positive integer.");
				return;
			}

			var verbose = false;

			if (args.Length == 4)
			{
				if (args[3] != "1" && args[3] != "0")
				{
					CustomConsole.WriteLine("Invalid verbose option. It must be either `0`, or `1`.");
					return;
				}

				verbose = (args[3] == "1");
			}

			CustomConsole.WriteLine("Reading...");

			var stream = new EndianStream(File.Open(args[0], FileMode.Open, FileAccess.ReadWrite), Endian.BigEndian);
			var version = new CacheFileVersionInfo(stream);
			if (version.Engine != EngineType.ThirdGeneration)
			{
				CustomConsole.WriteLine("Only third-generation map files are supported.");
				return;
			}

			var database = XMLEngineDatabaseLoader.LoadDatabase("Formats/Engines.xml");
			var buildInfo = database.FindEngineByVersion(version.BuildString);
			var cacheFile = new ThirdGenCacheFile(stream, buildInfo, version.BuildString);

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
					CustomConsole.WriteLine("Invalid section name: \"{0}\"", args[1]);
					return;
			}

			CustomConsole.WriteLine("- Engine version: {0}", version.BuildString);
			CustomConsole.WriteLine("- Internal name: {0}", cacheFile.InternalName);
			CustomConsole.WriteLine("- Scenario name: {0}", cacheFile.ScenarioName);
			CustomConsole.WriteLine();

			CustomConsole.WriteLine("Injecting empty pages...");

			var injectSize = pageCount * pageSize;
			section.Resize(section.Size + injectSize, stream);

			CustomConsole.WriteLine("Adjusting the header...");

			cacheFile.SaveChanges(stream);
			stream.Close();

			CustomConsole.WriteLine();

			var offset = section.Offset;
			if (section.ResizeOrigin == SegmentResizeOrigin.End)
				offset += section.ActualSize - injectSize;

			if (area != null)
				CustomConsole.WriteLine("Successfully injected 0x{0:X} bytes at 0x{1:X} (offset 0x{2:X}).", injectSize, area.BasePointer,
					offset);
			else
				CustomConsole.WriteLine("Successfully injected 0x{0:X} bytes at offset 0x{1:X}.", injectSize, offset);

			// save
			try
			{
				if (verbose)
					File.WriteAllText(Environment.CurrentDirectory + "\\output.txt", CustomConsole.Log);
			}
			catch
			{
				CustomConsole.WriteLine(); 
				CustomConsole.WriteLine("Uh oh, Unable to save verbose output. Check permissions.");
			}
		}

		private static class CustomConsole
		{
			public static string Log = "";

			public static void WriteLine()
			{
				Console.WriteLine("");
			}
			public static void WriteLine(string text)
			{
				Console.WriteLine(text);

				Log += text + "\r\n";
			}
			public static void WriteLine(string text, object arg0)
			{
				WriteLine(string.Format(text, arg0));
			}
			public static void WriteLine(string text, object arg0, object arg1)
			{
				WriteLine(string.Format(text, arg0, arg1));
			}
			public static void WriteLine(string text, object arg0, object arg1, object arg2)
			{
				WriteLine(string.Format(text, arg0, arg1, arg2));
			}
		}
	}
}