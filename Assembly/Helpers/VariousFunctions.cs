using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace Assembly.Helpers
{
    public static class VariousFunctions
    {
        /// <summary>
        /// Create a filename of a file that doesn't exist in temporary storage
        /// </summary>
        /// <param name="directory">The directory to create the file in</param>
        public static string CreateTemporaryFile(string directory)
        {
	        string file;
            var ran = new Random();
            while (true)
            {
                var filename = ran.Next(0, 200000) + ".tmp";
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
            var files = new List<FileInfo>();

            var tmpImg = new DirectoryInfo(GetTemporaryImageLocation());
            files.AddRange(new List<FileInfo>(tmpImg.GetFiles("*.dds")));
            var tmpLog = new DirectoryInfo(GetTemporaryErrorLogs());
            files.AddRange(new List<FileInfo>(tmpLog.GetFiles("*.tmp")));

			foreach (var fi in files)
// ReSharper disable EmptyGeneralCatchClause
				try { fi.Delete(); } catch (Exception) { }
// ReSharper restore EmptyGeneralCatchClause
        }

        public static void EmptyUpdaterLocations()
        {
			var tempDir = Path.GetTempPath();
			var updaterPath = Path.Combine(tempDir, "AssemblyUpdateManager.exe");
			var dllPath = Path.Combine(tempDir, "ICSharpCode.SharpZipLib.dll");

            // Wait for the updater to close
			var updaterProcesses = Process.GetProcessesByName("AssemblyUpdateManager.exe");
			foreach (var process in updaterProcesses.Where(process => !process.HasExited))
			{
				process.WaitForExit();
			}

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
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                ms.Position = 0;
				var bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.EndInit();

                return bi;
            }
        }

		public static byte[] StreamToByteArray(Stream input)
		{
			var buffer = new byte[16 * 1024];
			using (var ms = new MemoryStream())
			{
				int read;
				while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
				{
					ms.Write(buffer, 0, read);
				}
				return ms.ToArray();
			}
		}

		public static bool CheckIfFileLocked(FileInfo file)
		{
			FileStream stream = null;

			try
			{
				stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			}
			catch (IOException)
			{
				//the file is unavailable because it is:
				//still being written to
				//or being processed by another thread
				//or does not exist (has already been processed)
				return true;
			}
			finally
			{
				if (stream != null)
					stream.Close();
			}

			//file is not locked
			return false;
		}

        public static readonly char[] DisallowedPluginChars = new[] { ' ', '>', '<', ':', '-', '_', '/', '\\', '&', ';', '!', '?', '|', '*', '"' };
        /// <summary>
        /// Remove disallowed chars from the game name
        /// </summary>
        /// <param name="gameName">Game Name from the XML file</param>
        public static string SterilizeGamePluginFolder(string gameName)
        {
	        return DisallowedPluginChars.Aggregate(gameName, (current, disallowed) => current.Replace(disallowed.ToString(CultureInfo.InvariantCulture), ""));
        }

	    /// <summary>
        /// Replaces invalid filename characters in a tag class with an underscore (_) so that it can be used as part of a path.
        /// </summary>
        /// <param name="name">The tag class string to replace invalid characters in.</param>
        /// <returns>The "sterilized" name.</returns>
        public static string SterilizeTagClassName(string name)
        {
            // http://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name
            var regex = string.Format(@"(\.+$)|([{0}])", InvalidFileNameChars);
            return Regex.Replace(name, regex, "_");
        }

        private static readonly string InvalidFileNameChars = new string(Path.GetInvalidFileNameChars());

		public static bool IsJpegImage(string filename)
		{
			try
			{
				var img = Image.FromFile(filename);

				// Two image formats can be compared using the Equals method
				// See http://msdn.microsoft.com/en-us/library/system.drawing.imaging.imageformat.aspx
				//
				return img.RawFormat.Equals(ImageFormat.Jpeg);
			}
			catch (OutOfMemoryException)
			{
				// Image.FromFile throws an OutOfMemoryException 
				// if the file does not have a valid image format or
				// GDI+ does not support the pixel format of the file.
				//
				return false;
			}
		}
		public static bool IsJpegImage(Stream fileStream)
		{
			try
			{
				var img = Image.FromStream(fileStream);

				// Two image formats can be compared using the Equals method
				// See http://msdn.microsoft.com/en-us/library/system.drawing.imaging.imageformat.aspx
				//
				return img.RawFormat.Equals(ImageFormat.Jpeg);
			}
			catch (OutOfMemoryException)
			{
				// Image.FromFile throws an OutOfMemoryException 
				// if the file does not have a valid image format or
				// GDI+ does not support the pixel format of the file.
				//
				return false;
			}
		}
    }
}
