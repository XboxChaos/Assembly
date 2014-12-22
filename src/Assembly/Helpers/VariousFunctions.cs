using System;
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
		public static readonly char[] DisallowedPluginChars =
		{
			' ', '>', '<', ':', '-', '_', '/', '\\', '&', ';', '!', '?',
			'|', '*', '"'
		};

		private static readonly string InvalidFileNameChars = new string(Path.GetInvalidFileNameChars());

		public static void EmptyUpdaterLocations()
		{
			string tempDir = Path.GetTempPath();
			string updaterPath = Path.Combine(tempDir, "AssemblyUpdateManager.exe");
			string dllPath = Path.Combine(tempDir, "ICSharpCode.SharpZipLib.dll");

			// Wait for the updater to close
			Process[] updaterProcesses = Process.GetProcessesByName("AssemblyUpdateManager.exe");
			foreach (Process process in updaterProcesses.Where(process => !process.HasExited))
			{
				process.WaitForExit();
			}

			if (File.Exists(updaterPath))
				File.Delete(updaterPath);
			if (File.Exists(dllPath))
				File.Delete(dllPath);
		}

		/// <summary>
		///     Gets the parent directory of the application's exe
		/// </summary>
		public static string GetApplicationLocation()
		{
			return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";
		}

		/// <summary>
		///     Gets the location of the applications assembly (lulz, assembly.exe)
		/// </summary>
		public static string GetApplicationAssemblyLocation()
		{
			return System.Reflection.Assembly.GetExecutingAssembly().Location;
		}

		/// <summary>
		///     Convert a Bitmap to a BitmapImage
		/// </summary>
		public static BitmapImage BitmapToBitmapImage(Bitmap bitmap)
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

		/// <summary>
		///     Convert a BitmapImage to a Bitmap
		/// </summary>
		public static Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
		{
			using (var outStream = new MemoryStream())
			{
				BitmapEncoder enc = new BmpBitmapEncoder();
				enc.Frames.Add(BitmapFrame.Create(bitmapImage));
				enc.Save(outStream);
				var bitmap = new Bitmap(outStream);

				// return bitmap; <-- leads to problems, stream is closed/closing ...
				return new Bitmap(bitmap);
			}
		}
		
		public static byte[] StreamToByteArray(Stream input)
		{
			var buffer = new byte[16*1024];
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

		/// <summary>
		/// Silently executes a program and waits for it to finish.
		/// </summary>
		/// <param name="path">The path to the program to execute.</param>
		/// <param name="arguments">Command-line arguments to pass to the program.</param>
		/// <param name="workingDirectory">The working directory to run in the program in.</param>
		public static string RunProgramSilently(string path, string arguments, string workingDirectory)
		{
			var info = new ProcessStartInfo(path, arguments)
			{
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				WorkingDirectory = workingDirectory
			};


			var proc = Process.Start(info);
			if (proc == null)
				throw new InvalidOperationException();
			proc.WaitForExit();


			return proc.StandardError.ReadToEnd();
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

		/// <summary>
		///     Remove disallowed chars from the game name
		/// </summary>
		/// <param name="gameName">Game Name from the XML file</param>
		public static string SterilizeGamePluginFolder(string gameName)
		{
			return DisallowedPluginChars.Aggregate(gameName,
				(current, disallowed) => current.Replace(disallowed.ToString(CultureInfo.InvariantCulture), ""));
		}

		/// <summary>
		///     Replaces invalid filename characters in a tag class with an underscore (_) so that it can be used as part of a
		///     path.
		/// </summary>
		/// <param name="name">The tag class string to replace invalid characters in.</param>
		/// <returns>The "sterilized" name.</returns>
		public static string SterilizeTagClassName(string name)
		{
			// http://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name
			string regex = string.Format(@"(\.+$)|([{0}])", InvalidFileNameChars);
			return Regex.Replace(name, regex, "_");
		}
	}
}