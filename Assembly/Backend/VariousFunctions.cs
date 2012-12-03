using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Media.Imaging;

namespace Assembly.Backend
{
    public class VariousFunctions
    {
        /// <summary>
        /// Create a filename of a file that doesn't exist in temporary storage
        /// </summary>
        /// <param name="directory">The directory to create the file in</param>
        public static string CreateTemporaryFile(string directory)
        {
            string file = "";
            Random ran = new Random();
            while (true)
            {
                string filename = ran.Next(0, 200000).ToString() + ".tmp";
                file = directory + filename;

                if (!File.Exists(file))
                    break;
            }

            return file;
        }
        /// <summary>
        /// Clean up all temporary files stored by Assembly (Images, Error Logs)
        /// </summary>
        public static void CleanUpTemporaryFiles()
        {
            List<FileInfo> files = new List<FileInfo>();

            DirectoryInfo tmpImg = new DirectoryInfo(GetTemporaryImageLocation());
            files.AddRange(new List<FileInfo>(tmpImg.GetFiles("*.dds")));
            DirectoryInfo tmpLog = new DirectoryInfo(GetTemporaryErrorLogs());
            files.AddRange(new List<FileInfo>(tmpLog.GetFiles("*.tmp")));
            
            foreach (FileInfo fi in files)
                try { fi.Delete(); }
                catch { }
        }
        /// <summary>
        /// Gets the parent directory of the application's exe
        /// </summary>
        public static string GetApplicationLocation()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";
        }
        /// <summary>
        /// Gets the location of the applications assembly (lulz, assembly.exe)
        /// </summary>
        public static string GetApplicationAssemblyLocation()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().Location;
        }
        /// <summary>
        /// Get the temporary location to save update stuff
        /// </summary>
        public static string GetTemporaryInstallerLocation()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\XboxChaos_Assembly\\update_storage\\";
        }
        /// <summary>
        /// Get the temporary location to save images
        /// </summary>
        public static string GetTemporaryImageLocation()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\XboxChaos_Assembly\\temp_image_folder\\";
        }
        /// <summary>
        /// Get the temporary location to save error_logs
        /// </summary>
        public static string GetTemporaryErrorLogs()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\XboxChaos_Assembly\\error_logs\\";
        }
        /// <summary>
        /// Create the temporary Directories
        /// </summary>
        public static void CreateTemporaryDirectories()
        {
            if (!Directory.Exists(GetTemporaryImageLocation()))
                Directory.CreateDirectory(GetTemporaryImageLocation());

            if (!Directory.Exists(GetTemporaryErrorLogs()))
                Directory.CreateDirectory(GetTemporaryErrorLogs());

            if (!Directory.Exists(GetTemporaryInstallerLocation()))
                Directory.CreateDirectory(GetTemporaryInstallerLocation());
        }

        public enum UpdaterType { Assembly, Components }
        public static void EmptyUpdaterLocations()
        {
            DirectoryInfo tmpLog = new DirectoryInfo(GetTemporaryInstallerLocation());
            FileInfo[] files = tmpLog.GetFiles("*");

            foreach (FileInfo fi in files)
                try { fi.Delete(); }
                catch { }
        }
        public static string GetDownloadPath(UpdaterType type)
        {
            if (type == UpdaterType.Assembly)
                return GetTemporaryInstallerLocation() + "assembly_updated.inst";
            else
                return GetTemporaryInstallerLocation() + "assembly_components_updated.inst";
        }

        /// <summary>
        /// Convert a Bitmap to a BitmapImage
        /// </summary>
        public static BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.EndInit();

                return bi;
            }
        }


        public static char[] DisallowedPluginChars = new char[] { ' ', '>', '<', ':', '-', '_', '/', '\\', '&', ';', '!', '?', '|', '*', '"' };
        /// <summary>
        /// Remove disallowed chars from the game name
        /// </summary>
        /// <param name="gameName">Game Name from the XML file</param>
        public static string SteralizeGamePluginFolder(string gameName)
        {
            foreach(char disallowed in DisallowedPluginChars)
                gameName = gameName.Replace(disallowed.ToString(), "");

            return gameName;
        }
    }
}
