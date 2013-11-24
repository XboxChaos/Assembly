using System;
using System.Collections.Generic;
using System.Windows;
using Assembly.Helpers;
using Assembly.Helpers.Net;
#if !DEBUG
using Assembly.Metro.Dialogs;
#endif
using XBDMCommunicator;
using Microsoft.Shell;
using System.Threading;

namespace Assembly
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : ISingleInstanceApp
	{
		#region ISingleInstanceApp Members
		public bool SignalExternalCommandLineArgs(IList<string> args)
		{
			return Stuff.Rawr.HomeWindow == null || Stuff.Rawr.HomeWindow.ProcessCommandLineArgs(args);
		}

		#endregion

		[STAThread]
		public static void Main()
		{
			if (!SingleInstance<App>.InitializeAsFirstInstance("RecivedCommand")) return;

			var application = new App();

			application.InitializeComponent();
			application.Run();

			// Allow single instance code to perform cleanup operations
			SingleInstance<App>.Cleanup();
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

#if !DEBUG
			Current.DispatcherUnhandledException += (o, args) =>
			{
				MetroException.Show(args.Exception);

				args.Handled = true;
			};
#endif

			// For test compiling.
			//AssemblyPluginLoader.LoadPlugin(XmlReader.Create(@"Plugins\Halo3\bipd.asm"), new TestPluginVisitor());

			// Create Settings
			Stuff.Rawr = new Settings(true);

			// Update Assembly Protocol
			AssemblyProtocol.UpdateProtocol();

			// Create jumplist
			JumpLists.UpdateJumplists();

			// Create XBDM Instance
			Stuff.Rawr.Xbdm = new Xbdm(Settings.XDKNameIP);

			// Try and delete all temp data
			VariousFunctions.EmptyUpdaterLocations();

			// Dubs, checkem
			_0xabad1dea.CheckServerStatus();

			// Update File Defaults
			FileDefaults.UpdateFileDefaults();

			// Set closing method
			Current.Exit += (o, args) =>
				{
					// Update Settings with Window Width/Height
					Stuff.Rawr.ApplicationSizeMaximize = (Stuff.Rawr.HomeWindow.WindowState == WindowState.Maximized);
					if (!Stuff.Rawr.ApplicationSizeMaximize)
					{
						Stuff.Rawr.ApplicationSizeWidth = Stuff.Rawr.HomeWindow.Width;
						Stuff.Rawr.ApplicationSizeHeight = Stuff.Rawr.HomeWindow.Height;
					}

					// Save Settings
					Stuff.Rawr.UpdateSettings();
				};

			// Start Caching Blam Cache MetaData
			var metadataCacheThread = new Thread(BlamCacheMetaData.BeginCachingData);
			metadataCacheThread.Start();
		}
	}
}
