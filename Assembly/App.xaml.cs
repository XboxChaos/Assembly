using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using Assembly.Backend;
using Assembly.Backend.Plugins;
using Assembly.Metro.Dialogs;
using XBDMCommunicator;
using Microsoft.Shell;
using Assembly.Windows;
using Assembly.Backend.Cryptography;

namespace Assembly
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        #region ISingleInstanceApp Members
        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            if (Settings.homeWindow != null)
                return Settings.homeWindow.ProcessCommandLineArgs(args);
            else 
                return true;
        }

        
        #endregion

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance("RecivedCommand"))
            {
                var application = new App();

                application.InitializeComponent();
                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
            }
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
            Settings.xbdm = new XBDM(Settings.XDKNameIP);
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
            Application.Current.Exit += (o, args) =>
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

            #region Hide Shitty Code
            // Check if Startup Params Exist
            // ERHMAGAWD - So much haxxy stuff >.>
//            if (e.Args != null && e.Args.Length > 0)
//            {
//                string[] args = new string[0];
//                if (e.Args[0].ToLower().Contains("assembly://"))
//                {
//                    e.Args[0] = e.Args[0].Replace("assembly://", "");
//                    args = e.Args[0].Split('/');
//                }
//                else
//                {
//                    if (File.Exists(e.Args[0]))
//                    {
//                        FileInfo fi = new FileInfo(e.Args[0]);
//                        args = new string[2];
//                        args[1] = fi.FullName;

//                        if (fi.Extension.ToLower() == ".map")
//                            args[0] = "map";
//                        else if (fi.Extension.ToLower() == ".blf")
//                            args[0] = "blf";
//                        else if (fi.Extension.ToLower() == ".mapinfo")
//                            args[0] = "mapinfo";
//                        else
//                            args = new string[2] { "", "" };
//                    }
//                    else
//                        args = new string[2] { "", "" };
//                }
//#if DEBUG
//                try { File.WriteAllLines(@"C:/args.txt", args); } catch { }
//#endif
//                appStartupManager = new StartupManager(args);
//            }
            #endregion
        }

        public class Validate
        {
            public string action = "valUsr";
            public Inner Content;

            
            public class Inner
            {
                public string machName { get; set; }
                public string machNameSec { get; set; }
                public string compName { get; set; }
                public string userName { get; set; }
            }
        }
    }
}
