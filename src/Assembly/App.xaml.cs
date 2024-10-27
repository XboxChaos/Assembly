#if !DEBUG
using Assembly.Metro.Dialogs;
#endif
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using Assembly.Helpers;
using Assembly.Helpers.Net;
using Blamite.RTE.Console;
using Microsoft.Shell;

namespace Assembly
{
	/// <summary>
	///     Interaction logic for App.xaml
	/// </summary>
	public partial class App : ISingleInstanceApp
	{
		#region ISingleInstanceApp Members

		public bool SignalExternalCommandLineArgs(IList<string> args)
		{
			return AssemblyStorage.AssemblySettings.HomeWindow == null ||
			       AssemblyStorage.AssemblySettings.HomeWindow.ProcessCommandLineArgs(args);
		}

		#endregion

		public static Storage AssemblyStorage { get; set; }

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
			// Reset Directory
			System.IO.Directory.SetCurrentDirectory(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));

			// Load version info
			VersionInfo.Load("version.json");

			// Create Assembly Storage
#if !DEBUG
			try
			{
				App.AssemblyStorage = new Storage();
			}
			catch (Exception ex)
			{
				Metro.Dialogs.MetroException.Show(ex);
			}
#endif
#if DEBUG
			App.AssemblyStorage = new Storage();
#endif

			// Update Assembly Protocol
			AssemblyProtocol.UpdateProtocol();

			// Create jumplist
			JumpLists.UpdateJumplists();

			// Create XBDM Instances
			AssemblyStorage.AssemblySettings.XenonConsole = new XeConsole(AssemblyStorage.AssemblySettings.XeConsoleNameIp);
			AssemblyStorage.AssemblySettings.XboxConsole = new XbConsole(AssemblyStorage.AssemblySettings.XbConsoleNameIp);
			AssemblyStorage.AssemblySettings.XenonFusionConsole = new XeConsole(AssemblyStorage.AssemblySettings.XeConsoleNameIp, true);

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
				AssemblyStorage.AssemblySettings.ApplicationSizeMaximize =
					(AssemblyStorage.AssemblySettings.HomeWindow.WindowState == WindowState.Maximized);
				if (AssemblyStorage.AssemblySettings.ApplicationSizeMaximize) return;

				AssemblyStorage.AssemblySettings.ApplicationSizeWidth = AssemblyStorage.AssemblySettings.HomeWindow.Width;
				AssemblyStorage.AssemblySettings.ApplicationSizeHeight = AssemblyStorage.AssemblySettings.HomeWindow.Height;
			};

		}
	}
}