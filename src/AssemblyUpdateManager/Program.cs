using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;

namespace AssemblyUpdateManager
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			if (args.Length != 3)
			{
				Console.Error.WriteLine("Error: not enough arguments");
				Console.Error.WriteLine("Usage: AssemblyUpdateManager <update zip> <assembly exe> <parent pid>");
				return;
			}
			var zipPath = args[0];
			var exePath = args[1];
			var pid = Convert.ToInt32(args[2]);

			try
			{
				// Wait for Assembly to close
				try
				{
					if (pid != -1)
					{
						var process = Process.GetProcessById(pid);
						process.WaitForExit();
						process.Close();
					}
				}
				catch
				{
				}

				// Delete the old backup
				try
				{
					Directory.Delete(@"Backup\Formats", true);
					Directory.Delete(@"Backup\Plugins", true);
				}
				catch
				{
				}

				// Copy formats and plugins to backup folder
				try
				{
					DirectoryCopy("Formats", @"Backup\Formats", true);
					DirectoryCopy("Plugins", @"Backup\Plugins", true);
					Directory.Delete("Formats", true);
					Directory.Delete("Plugins", true);
				}
				catch
				{
				}

				// Extract the update zip
				var fz = new FastZip();
				fz.CreateEmptyDirectories = true;
				for (var i = 0; i < 5; i++)
				{
					try
					{
						fz.ExtractZip(zipPath, Directory.GetCurrentDirectory(), null);
						break;
					}
					catch (IOException)
					{
						Thread.Sleep(1000);
						if (i == 4)
						{
							throw;
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Assembly Update Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			try
			{
				File.Delete(zipPath);
			}
			catch
			{
			}

			// Launch "The New iPa... Assembly"
			Process.Start(exePath);
		}

		// http://msdn.microsoft.com/en-us/library/bb762914%28v=vs.110%29.aspx
		private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			var dir = new DirectoryInfo(sourceDirName);
			var dirs = dir.GetDirectories();

			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);
			}

			// If the destination directory doesn't exist, create it. 
			if (!Directory.Exists(destDirName))
				Directory.CreateDirectory(destDirName);

			// Get the files in the directory and copy them to the new location.
			var files = dir.GetFiles();
			foreach (var file in files)
			{
				var temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, false);
			}

			// If copying subdirectories, copy them and their contents to new location. 
			if (!copySubDirs)
				return;
			foreach (var subdir in dirs)
			{
				var temppath = Path.Combine(destDirName, subdir.Name);
				DirectoryCopy(subdir.FullName, temppath, true);
			}
		}
	}
}