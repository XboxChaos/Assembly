using System.Collections.Generic;
using System.Windows;
using Assembly.Helpers;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using Assembly.Metro.Dialogs.ControlDialogs;
using Blamite.Blam;

namespace Assembly.Metro.Dialogs
{
	public class MetroTagBlockReallocator
	{
		public static int? Show(ICacheFile cache, TagBlockData data)
		{
			App.AssemblyStorage.AssemblySettings.HomeWindow.ShowMask();
			var dialog = new TagBlockReallocator(data)
			{
				Owner = App.AssemblyStorage.AssemblySettings.HomeWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			dialog.ShowDialog();
			App.AssemblyStorage.AssemblySettings.HomeWindow.HideMask();
			return dialog.NewCount;
		}
	}
}