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
		private readonly string _pluginClass;
		private readonly string _pluginClassDescription;
		private readonly IList<PluginRevision> _revisions = new List<PluginRevision>();

		public PluginRevisionViewer(IList<PluginRevision> revisions, string pluginClass)
		{
			InitializeComponent();
			DwmDropShadow.DropShadowToWindow(this);

			_revisions = revisions;
			_pluginClass = pluginClass;
			_pluginClassDescription = string.Format("Revision history for the {0} plugin:", _pluginClass);

			DataContext = this;
		}

		public IList<PluginRevision> Revisions
		{
			get { return _revisions; }
		}

		public string PluginClass
		{
			get { return _pluginClass; }
		}

		public string PluginClassDescription
		{
			get { return _pluginClassDescription; }
		}

		private void btnActionClose_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}