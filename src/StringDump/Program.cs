using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Blamite.Blam;
using Blamite.Blam.ThirdGen;
using Blamite.Flexibility;
using Blamite.IO;

namespace StringDump
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open Cache File";
            ofd.Filter = "Blam Cache Files|*.map";
            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Save String Dump";
            sfd.Filter = "Text Files|*.txt";
            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            string mapPath = ofd.FileName;
            string dumpPath = sfd.FileName;

            BuildInfoLoader loader = new BuildInfoLoader(@"Formats\SupportedBuilds.xml", "Formats");
            ICacheFile cacheFile;
            LocaleTable locales;
            using (IReader reader = new EndianReader(File.OpenRead(mapPath), Endian.BigEndian))
            {
                Console.WriteLine("Loading cache file...");
                cacheFile = CacheFileLoader.LoadCacheFile(reader, loader);

                Console.WriteLine("Loading locales...");
                locales = cacheFile.Languages[LocaleLanguage.English].LoadStrings(reader);
            }

            StreamWriter output = new StreamWriter(dumpPath);
            output.WriteLine("Input file: {0}.map", cacheFile.InternalName);
            output.WriteLine();

            // Sort locales by stringID
            List<Locale> localesById = new List<Locale>();
            foreach (Locale str in locales.Strings)
            {
                if (str != null)
                    localesById.Add(str);
            }
            localesById.Sort((x, y) => x.ID.Value.CompareTo(y.ID.Value));

            // Dump locales
            Console.WriteLine("Dumping locales...");
            output.WriteLine("---------------");
            output.WriteLine("English Locales");
            output.WriteLine("---------------");
            foreach (Locale str in localesById)
            {
                if (str != null)
                    output.WriteLine("{0} = \"{1}\"", str.ID, str.Value);
            }
            output.WriteLine();

            // Dump stringIDs
            Console.WriteLine("Dumping stringIDs...");
            output.WriteLine("---------");
            output.WriteLine("StringIDs");
            output.WriteLine("---------");
            int index = 0;
            foreach (string str in cacheFile.StringIDs)
            {
                if (str != null)
                    output.WriteLine("0x{0:X} = \"{1}\"", index, str);
                else
                    output.WriteLine("0x{0:X} = (null)", index);

                index++;
            }
            output.Close();

            Console.WriteLine("Done!");
        }
    }
}
