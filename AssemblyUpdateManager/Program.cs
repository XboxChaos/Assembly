using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Assembly.Backend;
using ICSharpCode.SharpZipLib.Zip;

namespace AssemblyUpdateManager
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Kill retail shit
                System.Diagnostics.Process[] openAssemblys = Process.GetProcessesByName("Assembly.exe");
                foreach (Process process in openAssemblys)
                    if (!process.HasExited)
                        process.Kill();
                // Kill dev shit
                openAssemblys = Process.GetProcessesByName("Assembly.vshost.exe");
                foreach (Process process in openAssemblys)
                    if (!process.HasExited)
                        process.Kill();

                INIFile ini = new INIFile(GetTemporaryInstallerLocation() + "install.asmini");
                string basePath = ini.IniReadValue("Assembly Update", "BasePath");
                string assemblyPath = ini.IniReadValue("Assembly Update", "AssemblyPath");

                // Delete and Move the new Assembly
                if (File.Exists(assemblyPath))
                    File.Delete(assemblyPath);
                File.Move(GetTemporaryInstallerLocation() + "assembly_updated.inst", assemblyPath);

                // Extract Components
                FastZip fz = new FastZip();
                fz.CreateEmptyDirectories = true;
                fz.ExtractZip(GetTemporaryInstallerLocation() + "assembly_components_updated.inst", basePath, null);

                // Launch "The New iPa... Assembly"
                Process.Start(assemblyPath);
            }
            catch (Exception ex)
            {
                File.WriteAllText(GetTemporaryInstallerLocation() + "error.txt", ex.ToString());
            }
        }

        /// <summary>
        /// Get the temporary location to save update stuff
        /// </summary>
        public static string GetTemporaryInstallerLocation()
        {
            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\XboxChaos_Assembly\\update_storage\\path.txt", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\XboxChaos_Assembly\\update_storage\\");
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\XboxChaos_Assembly\\update_storage\\";
        }
    }
}
