using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace Assembly.Backend
{
    public class AssemblyProtocol
    {
        public static void UpdateProtocol()
        {
            RegistryKey keyProtoBase = Registry.ClassesRoot.CreateSubKey("assembly\\");
            keyProtoBase.SetValue("", "URL:Assembly Application Manager");
            keyProtoBase.SetValue("AppUserModelID", "XboxChaos.Assembly.Default");
            keyProtoBase.SetValue("FriendlyTypeName", "Assembly Application Manag5er");
            keyProtoBase.SetValue("SourceFilter", "");
            keyProtoBase.SetValue("URL Protocol", "");

            RegistryKey keyProtoDefaultIcon = Registry.ClassesRoot.CreateSubKey("assembly\\DefaultIcon\\");
            keyProtoDefaultIcon.SetValue("", VariousFunctions.GetApplicationLocation() + "AssemblyIconLibrary.dll,100");

            RegistryKey keyProtoExtensions = Registry.ClassesRoot.CreateSubKey("assembly\\Extensions\\");
            RegistryKey keyProtoShell = Registry.ClassesRoot.CreateSubKey("assembly\\shell\\");
            RegistryKey keyProtoShellOpen = Registry.ClassesRoot.CreateSubKey("assembly\\shell\\open\\");
            RegistryKey keyProtoShellOpenCommand = Registry.ClassesRoot.CreateSubKey("assembly\\shell\\open\\command\\");
            keyProtoShellOpenCommand.SetValue("", string.Format("\"{0}\" %1", VariousFunctions.GetApplicationAssemblyLocation()));
        }
    }
}
