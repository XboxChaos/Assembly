using Microsoft.Win32;

namespace Assembly.Helpers
{
    public static class AssemblyProtocol
    {
        public static void UpdateProtocol()
        {
            var keyProtoBase = Registry.CurrentUser.CreateSubKey("Software\\Classes\\assembly\\");
	        if (keyProtoBase != null)
	        {
		        keyProtoBase.SetValue("", "URL:Assembly Application Manager");
		        keyProtoBase.SetValue("AppUserModelID", "XboxChaos.Assembly.Default");
		        keyProtoBase.SetValue("FriendlyTypeName", "Assembly Application Manager");
		        keyProtoBase.SetValue("SourceFilter", "");
		        keyProtoBase.SetValue("URL Protocol", "");
	        }

	        var keyProtoDefaultIcon = keyProtoBase.CreateSubKey("DefaultIcon\\");
	        if (keyProtoDefaultIcon != null)
		        keyProtoDefaultIcon.SetValue("", VariousFunctions.GetApplicationLocation() + "AssemblyIconLibrary.dll,100");

#pragma warning disable 168
	        var keyProtoExtensions = keyProtoBase.CreateSubKey("Extensions\\");
            var keyProtoShell = keyProtoBase.CreateSubKey("shell\\");
            var keyProtoShellOpen = keyProtoBase.CreateSubKey("shell\\open\\");
#pragma warning restore 168
            var keyProtoShellOpenCommand = keyProtoBase.CreateSubKey("shell\\open\\command\\");
	        if (keyProtoShellOpenCommand != null)
		        keyProtoShellOpenCommand.SetValue("", string.Format("\"{0}\" %1", VariousFunctions.GetApplicationAssemblyLocation()));
        }
    }
}