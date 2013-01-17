using System;
using System.Collections.Generic;
using System.Windows;
using Assembly.Helpers;
using XBDMCommunicator;
using Microsoft.Shell;

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
	        return Settings.homeWindow == null || Settings.homeWindow.ProcessCommandLineArgs(args);
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

            // For test compiling.
            //AssemblyPluginLoader.LoadPlugin(XmlReader.Create(@"Plugins\Halo3\bipd.asm"), new TestPluginVisitor());

            // Create Settings
            Settings.LoadSettings(true);

            // Update Assembly Protocol
            AssemblyProtocol.UpdateProtocol();

            // Create XBDM Instance
            Settings.xbdm = new Xbdm(Settings.XDKNameIP);
            //try { Settings.xbdm.Connect(); } catch { }

            // Create Temporary FilePaths
            VariousFunctions.CreateTemporaryDirectories();

            // Try and delete all temp data
            VariousFunctions.CleanUpTemporaryFiles();
            VariousFunctions.EmptyUpdaterLocations();

            // Dubs, checkem
            _0xabad1dea.CheckServerStatus();

            // Update File Defaults
            FileDefaults.UpdateFileDefaults();

            // Set closing method
            Current.Exit += (o, args) =>
                {
                    // Update Settings with Window Width/Height
                    Settings.applicationSizeMaximize = (Settings.homeWindow.WindowState == WindowState.Maximized);
                    if (!Settings.applicationSizeMaximize)
                    {
                        Settings.applicationSizeWidth = Settings.homeWindow.Width;
                        Settings.applicationSizeHeight = Settings.homeWindow.Height;
                    }

                    // Save Settings
                    Settings.UpdateSettings();
                };

#if !DEBUG
            Application.Current.DispatcherUnhandledException += (o, args) =>
                {
                    MetroException.Show((Exception)args.Exception);

                    args.Handled = true;
                };
#endif
        }
    }
}
