using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Assembly.Backend;
using Assembly.Metro.Native;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for Updater.xaml
    /// </summary>
    public partial class Updater : Window
    {
        private Backend.Updater.UpdateFormat _updateFormat;

        public Updater(Backend.Updater.UpdateFormat updateFormat)
        {
            InitializeComponent();
            DwmDropShadow.DropShadowToWindow(this);

            _updateFormat = updateFormat;

            LoadDataFromFormat();

            // Set up UI
            this.Width = 600;
            this.Height = 400;
            updateInfo.Visibility = System.Windows.Visibility.Visible;
            updateProgress.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void LoadDataFromFormat()
        {
            lblCurrentVersion.Text = _updateFormat.AssemblyVersionSpecial;
            lblServerVersion.Text = _updateFormat.ServerVersionSpecial;
            lblServerHash.Text = _updateFormat.Hash.ToUpper();

            lblChangeLog.Text = _updateFormat.ChangeLog;
        }
        private void headerThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Left = Left + e.HorizontalChange;
            Top = Top + e.VerticalChange;
        }

        private void btnApplyUpdate_Click(object sender, RoutedEventArgs e)
        {
            updateInfo.Visibility = System.Windows.Visibility.Collapsed;
            updateProgress.Visibility = System.Windows.Visibility.Visible;
            lblTitle.Text = "Installing Update...";

            Storyboard storyboard = (Storyboard)this.Resources["ResizeWindowToUpdate"];
            storyboard.Begin();

            // Begin Update Downloading...
            VariousFunctions.EmptyUpdaterLocations();
            DownloadAssembly();
        }
        private void btnIgnoreUpdate_Click(object sender, RoutedEventArgs e) { this.Close(); }

        #region Update Installing
        private void DownloadAssembly()
        {
            WebClient wb = new WebClient();
            wb.DownloadFileCompleted += (o, args) =>
                {
                    pbDownloadProgress.IsIndeterminate = true;
                    DownloadComponents();
                };
            wb.DownloadProgressChanged += (o, args) =>
                {
                    lblDownloadProgress.Text = string.Format("Currently Downloading: {0} -- ({1}%)", "Assembly's Assembly", args.ProgressPercentage);
                    pbDownloadProgress.Value = args.ProgressPercentage;
                };
            wb.DownloadFileAsync(new Uri(_updateFormat.EXELocation), VariousFunctions.GetDownloadPath(VariousFunctions.UpdaterType.Assembly));
            pbDownloadProgress.Value = 0;
            pbDownloadProgress.IsIndeterminate = false;
        }
        private void DownloadComponents()
        {
            WebClient wb = new WebClient();
            wb.DownloadFileCompleted += (o, args) =>
            {
                pbDownloadProgress.IsIndeterminate = true;
                CreateINI();
            };
            wb.DownloadProgressChanged += (o, args) =>
            {
                lblDownloadProgress.Text = string.Format("Currently Downloading: {0} -- ({1}%)", "Required Components {2}", args.ProgressPercentage, 
                    Settings.applicationEasterEggs ? "for prokids" : "");
                pbDownloadProgress.Value = args.ProgressPercentage;
            };
            wb.DownloadFileAsync(new Uri(_updateFormat.ComponentsLocation), VariousFunctions.GetDownloadPath(VariousFunctions.UpdaterType.Components));
            pbDownloadProgress.Value = 0;
            pbDownloadProgress.IsIndeterminate = false;
        }
        private void CreateINI()
        {
            INIFile ini = new INIFile(VariousFunctions.GetTemporaryInstallerLocation() + "install.asmini");
            ini.IniWriteValue("Assembly Update", "BasePath", VariousFunctions.GetApplicationLocation());
            ini.IniWriteValue("Assembly Update", "AssemblyPath", VariousFunctions.GetApplicationAssemblyLocation());

            ExtractUpdateManager();
        }
        private void ExtractUpdateManager()
        {
            Stream zipDLL = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Extryze.Update.ICSharpCode.SharpZipLib.dll");
            Stream exeUpd = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Extryze.Update.AssemblyUpdateManager.exe");

            using (var zipFileStream = new FileStream(VariousFunctions.GetTemporaryInstallerLocation() + "ICSharpCode.SharpZipLib.dll", FileMode.Create))
                zipDLL.CopyTo(zipFileStream);
            using (var exeFileStream = new FileStream(VariousFunctions.GetTemporaryInstallerLocation() + "AssemblyUpdateManager.exe", FileMode.Create))
                exeUpd.CopyTo(exeFileStream);

            Process.Start(VariousFunctions.GetTemporaryImageLocation() + "AssemblyUpdateManager.exe");
            Application.Current.Shutdown();
        }
        #endregion
    }
}
