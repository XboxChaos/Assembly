using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Assembly.Helpers;
using ICSharpCode.SharpZipLib.Zip;

namespace AssemblyUpdateManager
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Error: not enough arguments");
                Console.Error.WriteLine("Usage: AssemblyUpdateManager <update zip> <assembly exe> <parent pid>");
                return;
            }
            string zipPath = args[0];
            string exePath = args[1];
            int pid = Convert.ToInt32(args[2]);

            try
            {
                // Wait for Assembly to close
                try
                {
                    Process process = Process.GetProcessById(pid);
                    process.WaitForExit();
                    process.Close();
                }
                catch { }

                // Extract the update zip
                FastZip fz = new FastZip();
                fz.CreateEmptyDirectories = true;
                fz.ExtractZip(zipPath, Directory.GetCurrentDirectory(), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Assembly Update Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            try
            {
                File.Delete(zipPath);
            }
            catch { }

            // Launch "The New iPa... Assembly"
            Process.Start("Assembly://post-update");
        }
    }
}
