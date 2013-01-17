using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace Assembly.Helpers
{
    public class FileDefaults
    {
        public static void UpdateFileDefaults()
        {
            // Allocation to apptypes
            RegistryKey keyAppMap = Registry.CurrentUser.CreateSubKey(@"Software\Classes\assembly.xboxchaos.map\shell\open\command\");
            RegistryKey keyAppBLF = Registry.CurrentUser.CreateSubKey(@"Software\Classes\assembly.xboxchaos.blf\shell\open\command\");
            RegistryKey keyAppMIF = Registry.CurrentUser.CreateSubKey(@"Software\Classes\assembly.xboxchaos.mif\shell\open\command\");

            // Assign apptypes
            keyAppMap.SetValue("", string.Format("\"{0}\"open \"%1\"", VariousFunctions.GetApplicationAssemblyLocation()));
            keyAppBLF.SetValue("", string.Format("\"{0}\"open \"%1\"", VariousFunctions.GetApplicationAssemblyLocation()));
            keyAppMIF.SetValue("", string.Format("\"{0}\"open \"%1\"", VariousFunctions.GetApplicationAssemblyLocation()));

            // Re-allocate to file extensions
            keyAppMap = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.map\");
            keyAppBLF = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.blf\");
            keyAppMIF = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.mapinfo\");

            // Assign Valid apptypes
            if (Settings.defaultMAP)
                keyAppMap.SetValue("", "assembly.xboxchaos.map");
            else
                keyAppMap.SetValue("", "");

            if (Settings.defaultBLF)
                keyAppBLF.SetValue("", "assembly.xboxchaos.blf");
            else
                keyAppBLF.SetValue("", "");

            if (Settings.defaultMIF)
                keyAppMIF.SetValue("", "assembly.xboxchaos.mif");
            else
                keyAppMIF.SetValue("", "");
        }
    }
}
