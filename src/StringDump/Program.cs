using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Blamite.Blam;
using Blamite.Blam.LanguagePack;
using Blamite.Blam.ThirdGen;
using Blamite.Flexibility;
using Blamite.Flexibility.Settings;
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

            var engineDb = XMLEngineDatabaseLoader.LoadDatabase("Formats/Engines.xml");
            ICacheFile cacheFile;
            ILanguagePack locales;
            using (IReader reader = new EndianReader(File.OpenRead(mapPath), Endian.BigEndian))
            {
                Console.WriteLine("Loading cache file...");
                cacheFile = CacheFileLoader.LoadCacheFile(reader, engineDb);

                Console.WriteLine("Loading locales...");
                locales = cacheFile.Languages.LoadLanguage(GameLanguage.English, reader);
            }

            StreamWriter output = new StreamWriter(dumpPath);
            output.WriteLine("Input file: {0}.map", cacheFile.InternalName);
            output.WriteLine();

            // Sort locales by stringID
            List<LocalizedString> localesById = new List<LocalizedString>();
            localesById.AddRange(locales.StringLists.SelectMany(l => l.Strings).Where(s => s != null && s.Value != null));
            localesById.Sort((x, y) => x.Key.Value.CompareTo(y.Key.Value));

            // Dump locales
            Console.WriteLine("Dumping locales...");
            output.WriteLine("---------------");
            output.WriteLine("English Locales");
            output.WriteLine("---------------");
            foreach (LocalizedString str in localesById)
            {
                if (str != null)
                    output.WriteLine("{0} = \"{1}\"", str.Key, str.Value);
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
