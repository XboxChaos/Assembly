using System;
using Assembly.Helpers.Native;
using Microsoft.Win32;

namespace Assembly.Helpers
{
	public class FileDefaults
	{
		public static void UpdateFileDefaults()
		{
			string assemblyPath = VariousFunctions.GetApplicationAssemblyLocation();
			bool changed = false;

			// Assign open commands
			changed |= RegisterOpenCommand("assembly.xboxchaos.map", App.AssemblyStorage.AssemblySettings.DefaultMap,
				"Blam Cache File", string.Format("\"{0}\" open \"%1\"", assemblyPath));
			changed |= RegisterOpenCommand("assembly.xboxchaos.blf", App.AssemblyStorage.AssemblySettings.DefaultBlf,
				"Blam BLF File", string.Format("\"{0}\" open \"%1\"", assemblyPath));
			changed |= RegisterOpenCommand("assembly.xboxchaos.mif", App.AssemblyStorage.AssemblySettings.DefaultMif,
				"Blam Map Information File", string.Format("\"{0}\" open \"%1\"", assemblyPath));
			changed |= RegisterOpenCommand("assembly.xboxchaos.amp", App.AssemblyStorage.AssemblySettings.DefaultAmp,
				"Assembly Patch File", string.Format("\"{0}\" open \"%1\"", assemblyPath));

			// Assign Valid apptypes
			changed |= RegisterExtension(".map", App.AssemblyStorage.AssemblySettings.DefaultMap, "assembly.xboxchaos.map",
				"assembly/map", "");
			changed |= RegisterExtension(".blf", App.AssemblyStorage.AssemblySettings.DefaultBlf, "assembly.xboxchaos.blf",
				"assembly/blf", "");
			changed |= RegisterExtension(".mapinfo", App.AssemblyStorage.AssemblySettings.DefaultMif, "assembly.xboxchaos.mif",
				"assembly/mapinfo", "");
			changed |= RegisterExtension(".asmp", App.AssemblyStorage.AssemblySettings.DefaultAmp, "assembly.xboxchaos.amp",
				"assembly/patch", "");

			if (changed)
				ShellChanges.SHChangeNotify(HChangeNotifyEventID.SHCNE_ASSOCCHANGED, HChangeNotifyFlags.SHCNF_IDLIST, IntPtr.Zero,
					IntPtr.Zero);
		}

		private static bool RegisterOpenCommand(string clazz, bool register, string description, string command)
		{
			if (string.IsNullOrWhiteSpace(clazz))
				throw new ArgumentException("Invalid class");

			string pathBase = @"Software\Classes\" + clazz;
			string path = pathBase + @"\shell\open\command\";

			if (!register)
				return DeleteKey(Registry.CurrentUser, pathBase);

			// Set description
			bool changed = false;
			using (RegistryKey key = Registry.CurrentUser.CreateSubKey(pathBase))
			{
				changed |= SetKeyValue(key, "", description);
			}

			// Set command
			using (RegistryKey key = Registry.CurrentUser.CreateSubKey(path))
			{
				changed |= SetKeyValue(key, "", command);
			}
			return changed;
		}

		private static bool RegisterExtension(string ext, bool register, string clazz, string contentType,
			string perceivedType)
		{
			string path = @"Software\Classes\" + ext;

			if (!register)
				return DeleteKey(Registry.CurrentUser, path);

			bool changed = false;
			using (RegistryKey key = Registry.CurrentUser.CreateSubKey(path))
			{
				changed |= SetKeyValue(key, "", clazz);
				changed |= SetKeyValue(key, "Content Type", contentType);
				changed |= SetKeyValue(key, "PerceivedType", perceivedType);
			}
			return changed;
		}

		private static bool KeyExists(RegistryKey parent, string path)
		{
			RegistryKey key = parent.OpenSubKey(path);
			if (key != null)
			{
				key.Close();
				return true;
			}
			return false;
		}

		private static bool DeleteKey(RegistryKey parent, string path)
		{
			if (KeyExists(parent, path))
			{
				parent.DeleteSubKeyTree(path);
				return true;
			}
			return false;
		}

		private static bool SetKeyValue(RegistryKey key, string name, object newValue)
		{
			object oldValue = key.GetValue(name);
			key.SetValue(name, newValue);
			return (oldValue == null || !oldValue.Equals(newValue));
		}
	}
}