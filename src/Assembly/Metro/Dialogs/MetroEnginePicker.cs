using System.Collections.Generic;
using System.Windows;
using Assembly.Metro.Dialogs.ControlDialogs;
using Blamite.Serialization;

namespace Assembly.Metro.Dialogs
{
	public class MetroEnginePicker
	{
		public static EngineDescription Show(string path, List<EngineDescription> engines)
		{
			App.AssemblyStorage.AssemblySettings.HomeWindow.ShowMask();
			var dialog = new EnginePicker(path, engines)
			{
				Owner = App.AssemblyStorage.AssemblySettings.HomeWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			dialog.ShowDialog();
			App.AssemblyStorage.AssemblySettings.HomeWindow.HideMask();
			return dialog.Selection;
		}
	}
}