using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Xml.Linq;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;
using ExtryzeDLL.Patching;
using System;
using Assembly.Metro.Dialogs;

namespace Assembly.Metro.Controls.PageTemplates
{
    /// <summary>
    /// Interaction logic for PatchControl.xaml
    /// </summary>
    public partial class PatchControl
    {
// ReSharper disable ConvertToConstant.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
        private bool _doingWork = false;
// ReSharper restore FieldCanBeMadeReadOnly.Local
// ReSharper restore ConvertToConstant.Local

        public PatchControl()
        {
            InitializeComponent();
        }
        
        public bool Close()
        {
            return !_doingWork;
		}

		#region Patch Creation Functions
		// Enums

		// File Selectors
		private void btnCreatePatchUnModdifiedMap_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
				          {
					          Title = "Assembly - Select a Unmoddified (Clean) Map file",
							  Filter = "Blam Cache File (*.map)|*.map"
				          };
			if (ofd.ShowDialog() == DialogResult.OK)
				txtCreatePatchUnModdifiedMap.Text = ofd.FileName;
		}
		private void btnCreatePatchModdifiedMap_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Select a Moddified Map file",
				Filter = "Blam Cache File (*.map)|*.map"
			};
			if (ofd.ShowDialog() == DialogResult.OK)
				txtCreatePatchModdifiedMap.Text = ofd.FileName;
		}
		private void btnCreatePatchOutputPatch_Click(object sender, RoutedEventArgs e)
		{
			var sfd = new SaveFileDialog
			{
				Title = "Assembly - Select where to save the patch file",
				Filter = "Assembly Patch File (*.asmp)|*.asmp"
			};
			if (sfd.ShowDialog() == DialogResult.OK)
				txtCreatePatchOutputPatch.Text = sfd.FileName;
		}
		private void btnCreatePatchPreviewImage_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Select the mod preview image",
				Filter = "JPEG Image|*.jpg|PNG Image|*.png"
			};
			if (ofd.ShowDialog() == DialogResult.OK)
				txtCreatePatchPreviewImage.Text = ofd.FileName;
		}

		// Meta Sorting
		private void cboxCreatePatchTargetGame_SelectionChanged(object sender, SelectionChangedEventArgs e) { PatchCreationMetaOptionsVisibility(); }
		private void cbCreatePatchHasCustomMeta_Modified(object sender, RoutedEventArgs e) { PatchCreationMetaOptionsVisibility(); }
		private void PatchCreationMetaOptionsVisibility()
		{
			// Check if meta should be shown or not
			if (cbCreatePatchHasCustomMeta == null) return;

			// Meta Grids Cleanup
			PatchCreationNoMetaSelected.Visibility = Visibility.Collapsed;
			PatchCreationH3.Visibility = Visibility.Collapsed;
			PatchCreationHR.Visibility = Visibility.Collapsed;
			PatchCreationH4.Visibility = Visibility.Collapsed;
			
			// Check if custom meta is asked for
			if (cbCreatePatchHasCustomMeta.IsChecked == null || !(bool)cbCreatePatchHasCustomMeta.IsChecked)
			{
				PatchCreationNoMetaSelected.Visibility = Visibility.Visible;
				return;
			}

			// Set meta visibility depending on Targeted Game
			switch(cboxCreatePatchTargetGame.SelectedIndex)
			{
				case (int)TargetGame.Halo3:
				case (int)TargetGame.Halo3ODST: PatchCreationH3.Visibility = Visibility.Visible;
					break;

				case (int)TargetGame.HaloReach: 
					PatchCreationHR.Visibility = Visibility.Visible;
					break;

				case (int)TargetGame.Halo4:
					PatchCreationH4.Visibility = Visibility.Visible;
					break;

				default: PatchCreationNoMetaSelected.Visibility = Visibility.Visible;
					break;
			}
		}

		// Patch Creation
		private void btnCreatePatch_Click(object sender, RoutedEventArgs e)
		{
#if !DEBUG
			try
			{
#endif
				// Paths
				var cleanMapPath = txtCreatePatchUnModdifiedMap.Text;
				var moddedMapPath = txtCreatePatchModdifiedMap.Text;
				var outputPath = txtCreatePatchOutputPatch.Text;
				var previewImage = txtCreatePatchPreviewImage.Text;

				// Details
				var author = txtCreatePatchContentAuthor.Text;
				var desc = txtCreatePatchContentDescription.Text;
				var name = txtCreatePatchContentName.Text;

				// Make dat patch
				var patch = new Patch
				{
					Author = author,
					Description = desc,
					Name = name,
					Screenshot = String.IsNullOrEmpty(previewImage) ?
						null :
						File.ReadAllBytes(previewImage)
				};

				IReader originalReader = new EndianReader(File.OpenRead(cleanMapPath), Endian.BigEndian);
				IReader newReader = new EndianReader(File.OpenRead(moddedMapPath), Endian.BigEndian);

				var version = new ThirdGenVersionInfo(originalReader);
				var loader = new BuildInfoLoader(XDocument.Load(@"Formats\SupportedBuilds.xml"), @"Formats\");
				var buildInfo = loader.LoadBuild(version.BuildString);
				var originalFile = new ThirdGenCacheFile(originalReader, buildInfo, version.BuildString);
				var newFile = new ThirdGenCacheFile(newReader, buildInfo, version.BuildString);

				patch.MapInternalName = originalFile.Info.InternalName;
				MetaComparer.CompareMeta(originalFile, originalReader, newFile, newReader, patch);
				LocaleComparer.CompareLocales(originalFile, originalReader, newFile, newReader, patch);

				originalReader.Close();
				newReader.Close();

				IWriter output = new EndianWriter(File.OpenWrite(outputPath), Endian.BigEndian);
				AssemblyPatchWriter.WritePatch(patch, output);
				output.Close();

				MetroMessageBox.Show("Patch Created!", "Your patch has been created in the designated location. Happy Sailing Modder!");
#if !DEBUG
			}
			catch (Exception ex)
			{
				MetroException.Show(ex);
			}
#endif
		}
		#endregion

		#region Patch Applying Functions
		#endregion

		#region Patch Convertion Functions
		#endregion
	}
}
