using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Blamite.Blam;
using Blamite.Blam.Resources;
using Blamite.Flexibility;
using Blamite.Flexibility.Settings;
using Blamite.IO;
using Blamite.Util;

namespace SharedDump
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine("Usage: SharedDump <map file(s)>");
				return;
			}

			// Locate the Formats folder and SupportedBuilds.xml
			var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var formatsDir = Path.Combine(exeDir, "Formats");
			var supportedBuildsPath = Path.Combine(formatsDir, "Engines.xml");
            var db = XMLEngineDatabaseLoader.LoadDatabase(supportedBuildsPath);

			// Dump each map file
			foreach (var arg in args)
			{
				Console.WriteLine("{0}...", arg);
				DumpSharedResources(arg, db);
			}
		}

		static void DumpSharedResources(string mapPath, EngineDatabase db)
		{
			ICacheFile cacheFile;
			ResourceTable resources;
			using (var reader = new EndianReader(File.OpenRead(mapPath), Endian.BigEndian))
			{
				cacheFile = CacheFileLoader.LoadCacheFile(reader, db);
				resources = cacheFile.Resources.LoadResourceTable(reader);
			}

			using (var output = new StreamWriter(Path.ChangeExtension(mapPath, ".txt")))
			{
				output.WriteLine("Shared resources referenced by {0}:", Path.GetFileName(mapPath));
				output.WriteLine();
				output.WriteLine("Rsrc Datum  Map File      Class  Tag");
				output.WriteLine("----------  --------      -----  ---");

				foreach (var resource in resources.Resources.Where(r => r.Location != null && r.ParentTag != null))
				{
					// If either page has a null file path, then it's shared
					var loc = resource.Location;
					var primaryFile = (loc.PrimaryPage != null) ? loc.PrimaryPage.FilePath : null;
					var secondaryFile = (loc.SecondaryPage != null) ? loc.SecondaryPage.FilePath : null;
					if (primaryFile != null || secondaryFile != null)
					{
						var className = CharConstant.ToString(resource.ParentTag.Class.Magic);
						var name = cacheFile.FileNames.GetTagName(resource.ParentTag) ?? resource.ParentTag.Index.ToString();
						var fileName = primaryFile ?? secondaryFile;
						fileName = fileName.Substring(fileName.IndexOf('\\') + 1);
						output.WriteLine("{0}  {1, -12}  {2}   {3}", resource.Index, fileName, className, name);
					}
				}
			}
		}
	}
}
