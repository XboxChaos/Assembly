using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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

        public static void EmptyUpdaterLocations()
        {
            string tempDir = Path.GetTempPath();
            string updaterPath = Path.Combine(tempDir, "AssemblyUpdateManager.exe");
            string dllPath = Path.Combine(tempDir, "ICSharpCode.SharpZipLib.dll");

            if (File.Exists(updaterPath))
                File.Delete(updaterPath);
            if (File.Exists(dllPath))
                File.Delete(dllPath);
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
        public static string SterilizeGamePluginFolder(string gameName)
        {
            foreach(char disallowed in DisallowedPluginChars)
                gameName = gameName.Replace(disallowed.ToString(), "");

            return gameName;
        }

        /// <summary>
        /// Replaces invalid filename characters in a tag class with an underscore (_) so that it can be used as part of a path.
        /// </summary>
        /// <param name="name">The tag class string to replace invalid characters in.</param>
        /// <returns>The "sterilized" name.</returns>
        public static string SterilizeTagClassName(string name)
        {
            // http://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name
            string regex = string.Format(@"(\.+$)|([{0}])", InvalidFileNameChars);
            return Regex.Replace(name, regex, "_");
        }

        private static string InvalidFileNameChars = new string(Path.GetInvalidFileNameChars());
    }
}
