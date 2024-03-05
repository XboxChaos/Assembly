using System.Collections.Generic;
using System.Windows;
using Assembly.Metro.Controls.PageTemplates.Games;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using Assembly.Metro.Dialogs.ControlDialogs;
using Blamite.Blam;
using Blamite.Serialization;
using Blamite.IO;

namespace Assembly.Metro.Dialogs
{
	public static class MetroViewValueAs
	{
		/// <summary>
		///     View the selected offset as every meta value type.
		/// </summary>
		/// <param name="cacheFile">The cache file which is being read from.</param>
		/// <param name="buildInfo">Build information for the engine.</param>
		/// <param name="streamManager">The stream manager to open the file with.</param>
		/// <param name="fields">The fields to display in the viewer.</param>
		/// <param name="cacheOffset">The initial offset to display.</param>
		public static void Show(ICacheFile cacheFile, EngineDescription buildInfo, IStreamManager streamManager,
			IList<MetaField> fields, uint cacheOffset, TagEntry srcTag)
		{
			var valueAs = new ViewValueAs(cacheFile, buildInfo, streamManager, fields, cacheOffset, srcTag)
			{
				Owner = App.AssemblyStorage.AssemblySettings.HomeWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			valueAs.Show();
		}
	}
}