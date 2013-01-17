using Microsoft.Win32;

namespace Assembly.Helpers
{
    public static class AssemblyProtocol
    {
        public static void UpdateProtocol()
        {
            var keyProtoBase = Registry.ClassesRoot.CreateSubKey("assembly\\");
	        if (keyProtoBase != null)
	        {
		        keyProtoBase.SetValue("", "URL:Assembly Application Manager");
		        keyProtoBase.SetValue("AppUserModelID", "XboxChaos.Assembly.Default");
		        keyProtoBase.SetValue("FriendlyTypeName", "Assembly Application Manager");
		        keyProtoBase.SetValue("SourceFilter", "");
		        keyProtoBase.SetValue("URL Protocol", "");
	        }

	        var keyProtoDefaultIcon = Registry.ClassesRoot.CreateSubKey("assembly\\DefaultIcon\\");
	        if (keyProtoDefaultIcon != null)
		        keyProtoDefaultIcon.SetValue("", VariousFunctions.GetApplicationLocation() + "AssemblyIconLibrary.dll,100");

#pragma warning disable 168
	        var keyProtoExtensions = Registry.ClassesRoot.CreateSubKey("assembly\\Extensions\\");
			var keyProtoShell = Registry.ClassesRoot.CreateSubKey("assembly\\shell\\");
			var keyProtoShellOpen = Registry.ClassesRoot.CreateSubKey("assembly\\shell\\open\\");
#pragma warning restore 168
			var keyProtoShellOpenCommand = Registry.ClassesRoot.CreateSubKey("assembly\\shell\\open\\command\\");
	        if (keyProtoShellOpenCommand != null)
		        keyProtoShellOpenCommand.SetValue("", string.Format("\"{0}\" %1", VariousFunctions.GetApplicationAssemblyLocation()));
        }
    }
}
