using Microsoft.Win32;

namespace Assembly.Helpers
{
    public class FileDefaults
    {
        public static void UpdateFileDefaults()
        {
            // Allocation to apptypes
			var keyAppMap = Registry.CurrentUser.CreateSubKey(@"Software\Classes\assembly.xboxchaos.map\shell\open\command\");
			var keyAppBLF = Registry.CurrentUser.CreateSubKey(@"Software\Classes\assembly.xboxchaos.blf\shell\open\command\");
			var keyAppMIF = Registry.CurrentUser.CreateSubKey(@"Software\Classes\assembly.xboxchaos.mif\shell\open\command\");

            // Assign apptypes
	        if (keyAppMap != null) keyAppMap.SetValue("", string.Format("\"{0}\"open \"%1\"", VariousFunctions.GetApplicationAssemblyLocation()));
	        if (keyAppBLF != null) keyAppBLF.SetValue("", string.Format("\"{0}\"open \"%1\"", VariousFunctions.GetApplicationAssemblyLocation()));
	        if (keyAppMIF != null) keyAppMIF.SetValue("", string.Format("\"{0}\"open \"%1\"", VariousFunctions.GetApplicationAssemblyLocation()));

	        // Re-allocate to file extensions
            keyAppMap = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.map\");
            keyAppBLF = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.blf\");
            keyAppMIF = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.mapinfo\");

            // Assign Valid apptypes
	        if (keyAppMap != null) keyAppMap.SetValue("", Settings.defaultMAP ? "assembly.xboxchaos.map" : "");

	        if (keyAppBLF != null) keyAppBLF.SetValue("", Settings.defaultBLF ? "assembly.xboxchaos.blf" : "");

	        if (keyAppMIF != null) keyAppMIF.SetValue("", Settings.defaultMIF ? "assembly.xboxchaos.mif" : "");
        }
    }
}
