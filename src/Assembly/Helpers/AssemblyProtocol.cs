using Microsoft.Win32;

namespace Assembly.Helpers
{
	public static class AssemblyProtocol
	{
		public static void UpdateProtocol()
		{
			RegistryKey keyProtoBase = Registry.CurrentUser.CreateSubKey("Software\\Classes\\assembly\\");
			if (keyProtoBase != null)
			{
				keyProtoBase.SetValue("", "URL:Assembly Application Manager");
				keyProtoBase.SetValue("AppUserModelID", "XboxChaos.Assembly.Default");
				keyProtoBase.SetValue("FriendlyTypeName", "Assembly Application Manager");
				keyProtoBase.SetValue("SourceFilter", "");
				keyProtoBase.SetValue("URL Protocol", "");
			}

			RegistryKey keyProtoDefaultIcon = keyProtoBase.CreateSubKey("DefaultIcon\\");
			if (keyProtoDefaultIcon != null)
				keyProtoDefaultIcon.SetValue("", VariousFunctions.GetApplicationLocation() + "AssemblyIconLibrary.dll,100");

#pragma warning disable 168
			RegistryKey keyProtoExtensions = keyProtoBase.CreateSubKey("Extensions\\");
			RegistryKey keyProtoShell = keyProtoBase.CreateSubKey("shell\\");
			RegistryKey keyProtoShellOpen = keyProtoBase.CreateSubKey("shell\\open\\");
#pragma warning restore 168
			RegistryKey keyProtoShellOpenCommand = keyProtoBase.CreateSubKey("shell\\open\\command\\");
			if (keyProtoShellOpenCommand != null)
				keyProtoShellOpenCommand.SetValue("", string.Format("\"{0}\" %1", VariousFunctions.GetApplicationAssemblyLocation()));
		}
	}
}