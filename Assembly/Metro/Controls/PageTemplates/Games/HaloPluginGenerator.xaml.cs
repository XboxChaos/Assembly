using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Assembly.Metro.Dialogs;

namespace Assembly.Metro.Controls.PageTemplates.Games
{
    /// <summary>
    /// Interaction logic for PluginGenerator.xaml
    /// </summary>
    public partial class HaloPluginGenerator : UserControl
    {
        public ObservableCollection<MapEntry> GeneratorMaps = new ObservableCollection<MapEntry>();

        public HaloPluginGenerator()
        {
            InitializeComponent();
            this.DataContext = GeneratorMaps;
        }

        public class MapEntry
        {
            public string MapName { get; set; }
            public string LocalMapPath { get; set; }
            public bool? IsSelected { get; set; }
        }

        public bool Close()
        {
            return true;
        }

        private void btnInputFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            if (Directory.Exists(txtInputFolder.Text))
                fbd.SelectedPath = txtInputFolder.Text;
            else
                fbd.SelectedPath = Backend.VariousFunctions.GetApplicationLocation();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                GeneratorMaps.Clear();
                txtInputFolder.Text = fbd.SelectedPath;

                DirectoryInfo di = new DirectoryInfo(txtInputFolder.Text);
                FileInfo[] fis = di.GetFiles("*.map");
                foreach (FileInfo fi in fis)
                {
                    GeneratorMaps.Add(new MapEntry()
                    {
                        IsSelected = true,
                        LocalMapPath = fi.FullName,
                        MapName = fi.Name
                    });
                }
            }
        }

        private void btnOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            if (Directory.Exists(txtOutputFolder.Text))
                fbd.SelectedPath = txtOutputFolder.Text;
            else
                fbd.SelectedPath = Backend.VariousFunctions.GetApplicationLocation() + "\\plugins\\";
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtOutputFolder.Text = fbd.SelectedPath;
            }
        }

        private void btnGeneratePlugins_Click_1(object sender, RoutedEventArgs e)
        {
            MetroMessageBox.Show("Not implemented yet. Use PluginGenerator.exe for now.");
        }
    }
}
