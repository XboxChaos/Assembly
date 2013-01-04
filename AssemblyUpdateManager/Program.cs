using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Assembly.Backend;
using ICSharpCode.SharpZipLib.Zip;

namespace AssemblyUpdateManager
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Error: not enough arguments");
                Console.Error.WriteLine("Usage: AssemblyUpdateManager <update zip> <assembly exe>");
                return;
            }
            string zipPath = args[0];
            string exePath = args[1];

            try
            {
                // Kill retail shit
                Process[] openAssemblys = Process.GetProcessesByName(Path.GetFileName(exePath));
                foreach (Process process in openAssemblys)
                {
                    if (!process.HasExited)
                        process.Kill();
                }

                // Kill dev shit
                openAssemblys = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exePath) + ".vshost.exe");
                foreach (Process process in openAssemblys)
                {
                    if (!process.HasExited)
                        process.Kill();
                }

                // Extract the update zip
                FastZip fz = new FastZip();
                fz.CreateEmptyDirectories = true;
                fz.ExtractZip(zipPath, Directory.GetCurrentDirectory(), null);
                File.Delete(zipPath);
            }
            catch (Exception ex)
            {
                // Write the exception data to a temporary file and run Assembly again, telling it to display it
                /*string filePath = Path.GetTempFileName();
                File.WriteAllText(filePath, ex.ToString());

                // The --updateError switch tells Assembly to display an exception message read from the text file
                Process.Start(exePath, "--updateError \"" + filePath + "\"");*/

                MessageBox.Show(ex.ToString(), "Assembly Update Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Launch "The New iPa... Assembly"
            Process.Start(exePath, "/fromUpdater " + Process.GetCurrentProcess().Id);
        }
    }
}
