using System.Collections.Generic;
using System.Windows;
using Atlas.Dialogs.Controls;
using Atlas.Views.Cache.TagEditorComponents.Data;
using Atlas.ViewModels;
using Atlas.ViewModels.Dialog;

namespace Atlas.Dialogs
{
	public static class MetroViewValueAs
	{
		public static void Show(CachePageViewModel cachePageViewModel, uint cacheOffsetOriginal,
			IList<TagDataField> tagDataFieldList)
		{
			var valueAs = new MetroViewValueAsWindow(new ViewValueAsViewModel(cachePageViewModel, cacheOffsetOriginal, 
				tagDataFieldList))
			{
				Owner = App.Storage.HomeWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			valueAs.Show();
		}
	}
}
