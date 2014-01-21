using System.Collections.Generic;
using Atlas.Models;
using Blamite.Plugins;

namespace Atlas.ViewModels.Dialog
{
	public class PluginRevisionViewerViewModel : Base
	{
		public PluginRevisionViewerViewModel(string title, IList<PluginRevision> pluginRevisions)
		{
			Title = title;
			PluginRevisions = pluginRevisions;
		}

		public string Title
		{
			get { return _title; }
			set { SetField(ref _title, value); }
		}
		private string _title;

		public IList<PluginRevision> PluginRevisions
		{
			get { return _pluginRevisions; }
			set { SetField(ref _pluginRevisions, value); }
		}
		private IList<PluginRevision> _pluginRevisions;
	}
}
