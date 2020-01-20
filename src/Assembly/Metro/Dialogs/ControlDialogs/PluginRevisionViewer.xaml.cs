using System.Collections.Generic;
using System.Windows;
using Assembly.Helpers.Native;
using Blamite.Plugins;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	///     Interaction logic for PluginRevisionViewer.xaml
	/// </summary>
	public partial class PluginRevisionViewer
	{
		private readonly string _pluginGroup;
		private readonly string _pluginGroupDescription;
		private readonly IList<PluginRevision> _revisions = new List<PluginRevision>();

		public PluginRevisionViewer(IList<PluginRevision> revisions, string pluginGroup)
		{
			InitializeComponent();
			DwmDropShadow.DropShadowToWindow(this);

			_revisions = revisions;
			_pluginGroup = pluginGroup;
			_pluginGroupDescription = string.Format("Revision history for the {0} plugin:", _pluginGroup);

			DataContext = this;
		}

		public IList<PluginRevision> Revisions
		{
			get { return _revisions; }
		}

		public string PluginGroup
		{
			get { return _pluginGroup; }
		}

		public string PluginGroupDescription
		{
			get { return _pluginGroupDescription; }
		}

		private void btnActionClose_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}