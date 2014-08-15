using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Assembly.Metro.Dialogs;
using Blamite.Blam;
using Blamite.Flexibility;
using Blamite.Flexibility.Settings;
using Blamite.IO;
using Blamite.Patching;

namespace Assembly.Metro.Controls.PageTemplates
{
	/// <summary>
	///     Interaction logic for PatchControl.xaml
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
			ApplyPatchControls.Visibility = Visibility.Collapsed;
			PatchApplicationPatchExtra.Visibility = Visibility.Collapsed;
		}

		public PatchControl(string pathPath)
		{
			InitializeComponent();
			PatchApplicationPatchExtra.Visibility = Visibility.Collapsed;

			tabPanel.SelectedIndex = 1;
			txtApplyPatchFile.Text = pathPath;
			bool isAlteration = txtApplyPatchFile.Text.EndsWith(".patchdat");
			LoadPatch(isAlteration);
		}

		// ReSharper disable UnusedMember.Global
		public bool Close()
		{
			return !_doingWork;
		}

		#region Patch Creation Functions

		// File Selectors
		private void btnCreatePatchUnModifiedMap_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Select a UnModified (Clean) Map file",
				Filter = "Blam Cache Files|*.map"
			};
			if (ofd.ShowDialog() == DialogResult.OK)
				txtCreatePatchUnModifiedMap.Text = ofd.FileName;
		}

		private void btnCreatePatchModifiedMap_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Select a Modified Map file",
				Filter = "Blam Cache Files|*.map"
			};
			if (ofd.ShowDialog() == DialogResult.OK)
				txtCreatePatchModifiedMap.Text = ofd.FileName;
		}

		private void btnCreatePatchOutputPatch_Click(object sender, RoutedEventArgs e)
		{
			var sfd = new SaveFileDialog
			{
				Title = "Assembly - Select where to save the patch file",
				Filter = "Assembly Patch Files|*.asmp"
			};
			if (sfd.ShowDialog() == DialogResult.OK)
				txtCreatePatchOutputPatch.Text = sfd.FileName;
		}

		private void btnCreatePatchPreviewImage_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Select the mod preview image",
				Filter = "Image Files|*.png;*.jpg;*.jpeg"
			};
			if (ofd.ShowDialog() == DialogResult.OK)
				txtCreatePatchPreviewImage.Text = ofd.FileName;
		}

		private void btnCreatePatchMapInfo_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Select a Modified MapInfo",
				Filter = "MapInfo File (*.mapinfo)|*.mapinfo"
			};
			if (ofd.ShowDialog() == DialogResult.OK)
				txtCreatePatchMapInfo.Text = ofd.FileName;
		}

		private void btnCreatePatchblf0_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Select a Modified BLF Container",
				Filter = "BLF Containers|*.blf"
			};
			if (ofd.ShowDialog() == DialogResult.OK)
				txtCreatePatchblf0.Text = ofd.FileName;
		}

		private void btnCreatePatchblf1_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Select a Modified BLF Container",
				Filter = "BLF Containers|*.blf"
			};
			if (ofd.ShowDialog() == DialogResult.OK)
				txtCreatePatchblf1.Text = ofd.FileName;
		}

		private void btnCreatePatchblf2_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Select a Modified BLF Container",
				Filter = "BLF Containers|*.blf"
			};
			if (ofd.ShowDialog() == DialogResult.OK)
				txtCreatePatchblf2.Text = ofd.FileName;
		}

		private void btnCreatePatchblf3_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Select a Modified BLF Container",
				Filter = "BLF Containers|*.blf"
			};
			if (ofd.ShowDialog() == DialogResult.OK)
				txtCreatePatchblf3.Text = ofd.FileName;
		}

		private void btnCreatePatchblf4_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Select a Modified BLF Container",
				Filter = "BLF Containers|*.blf"
			};
			if (ofd.ShowDialog() == DialogResult.OK)
				txtCreatePatchblf4.Text = ofd.FileName;
		}

		// Meta Sorting
		private void cboxCreatePatchTargetGame_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			PatchCreationMetaOptionsVisibility();
		}

		private void cbCreatePatchHasCustomMeta_Modified(object sender, RoutedEventArgs e)
		{
			PatchCreationMetaOptionsVisibility();
		}

		private void PatchCreationMetaOptionsVisibility()
		{
			// Check if meta should be shown or not
			if (cbCreatePatchHasCustomMeta == null) return;

			// Meta Grids Cleanup
			PatchCreationNoMetaSelected.Visibility = Visibility.Collapsed;
			PatchCreationExtras.Visibility = Visibility.Collapsed;

			if (cboxCreatePatchTargetGame.SelectedIndex == (int) TargetGame.Halo2Vista)
			{
				cbCreatePatchHasCustomMeta.IsEnabled = false;
				PatchCreationEmbeddedFiles.Visibility = Visibility.Collapsed;
				return;
			}
			cbCreatePatchHasCustomMeta.IsEnabled = true;
			PatchCreationEmbeddedFiles.Visibility = Visibility.Visible;

			// Check if custom meta is asked for
			if (cbCreatePatchHasCustomMeta.IsChecked == null || !(bool) cbCreatePatchHasCustomMeta.IsChecked)
			{
				PatchCreationNoMetaSelected.Visibility = Visibility.Visible;
				return;
			}

			// Set meta visibility depending on Targeted Game
			switch (cboxCreatePatchTargetGame.SelectedIndex)
			{
				case (int) TargetGame.Halo3:
				case (int) TargetGame.Halo3ODST:
					PrepHalo3();
					break;

				case (int) TargetGame.HaloReach:
					PrepHaloReach();
					break;

				case (int) TargetGame.Halo4:
					PrepHalo4();
					break;

				default:
					PatchCreationNoMetaSelected.Visibility = Visibility.Visible;
					break;
			}
		}

		private void PrepHalo3()
		{
			// Un-Hide Extras grid
			PatchCreationExtras.Visibility = Visibility.Visible;

			// Hide/Show fields
			PatchCreationBlfOption0.Visibility =
				PatchCreationBlfOption1.Visibility =
					PatchCreationBlfOption2.Visibility =
						PatchCreationBlfOption3.Visibility =
							PatchCreationBlfOption4.Visibility = Visibility.Visible;

			// Re-name fields
			lblCreatePatchTitleblf1.Text = "Modified blf_clip:";
			lblCreatePatchTitleblf1.Tag = "blf_clip";
			lblCreatePatchTitleblf2.Text = "Modified blf_film:";
			lblCreatePatchTitleblf1.Tag = "blf_film";
			lblCreatePatchTitleblf3.Text = "Modified blf_sm:";
			lblCreatePatchTitleblf4.Text = "Modified blf_variant:";

			// Reset fields
			txtCreatePatchMapInfo.Text = "";
			txtCreatePatchblf0.Text = "";
			txtCreatePatchblf1.Text = "";
			txtCreatePatchblf2.Text = "";
			txtCreatePatchblf3.Text = "";
			txtCreatePatchblf4.Text = "";
		}

		private void PrepHaloReach()
		{
			// Un-Hide Extras grid
			PatchCreationExtras.Visibility = Visibility.Visible;

			// Hide/Show fields
			PatchCreationBlfOption0.Visibility =
				PatchCreationBlfOption3.Visibility = Visibility.Visible;
			PatchCreationBlfOption1.Visibility =
				PatchCreationBlfOption2.Visibility =
					PatchCreationBlfOption4.Visibility = Visibility.Collapsed;

			// Re-name fields
			lblCreatePatchTitleblf3.Text = "Modified blf_sm:";


			// Reset fields
			txtCreatePatchMapInfo.Text = "";
			txtCreatePatchblf0.Text = "";
			txtCreatePatchblf1.Text = "";
			txtCreatePatchblf2.Text = "";
			txtCreatePatchblf3.Text = "";
			txtCreatePatchblf4.Text = "";
		}

		private void PrepHalo4()
		{
			// Un-Hide Extras grid
			PatchCreationExtras.Visibility = Visibility.Visible;

			// Hide/Show fields
			PatchCreationBlfOption0.Visibility =
				PatchCreationBlfOption3.Visibility =
					PatchCreationBlfOption1.Visibility =
						PatchCreationBlfOption2.Visibility = Visibility.Visible;
			PatchCreationBlfOption4.Visibility = Visibility.Collapsed;

			// Re-name fields
			lblCreatePatchTitleblf1.Text = "Modified blf_card:";
			lblCreatePatchTitleblf1.Tag = "blf_card";
			lblCreatePatchTitleblf2.Text = "Modified blf_lobby:";
			lblCreatePatchTitleblf1.Tag = "blf_lobby";
			lblCreatePatchTitleblf3.Text = "Modified blf_sm:";

			// Reset fields
			txtCreatePatchMapInfo.Text = "";
			txtCreatePatchblf0.Text = "";
			txtCreatePatchblf1.Text = "";
			txtCreatePatchblf2.Text = "";
			txtCreatePatchblf3.Text = "";
			txtCreatePatchblf4.Text = "";
		}

		// Utilities
		private bool CheckAllCreateMandatoryFields()
		{
			bool error = false;

			if (txtCreatePatchUnModifiedMap.Text == null) return false;

			// Check Un-modified map exists
			if (String.IsNullOrEmpty(txtCreatePatchUnModifiedMap.Text) || !File.Exists(txtCreatePatchUnModifiedMap.Text))
				error = true;

			// Check Modified map exists
			if (String.IsNullOrEmpty(txtCreatePatchModifiedMap.Text) || !File.Exists(txtCreatePatchModifiedMap.Text))
				error = true;

			// Check Content Name is entered
			if (String.IsNullOrEmpty(txtCreatePatchContentName.Text))
				error = true;

			// Check Content Author is entered
			if (String.IsNullOrEmpty(txtCreatePatchContentAuthor.Text))
				error = true;

			// Check Content Desc is entered
			if (String.IsNullOrEmpty(txtCreatePatchContentDescription.Text))
				error = true;

			if (error)
				MetroMessageBox.Show("Unable to make patch",
					"Mandatory fields are missing. Please make sure that you have filled out all required fields.");

			return !error;
		}

		private bool CheckAllCreateMetaFilesExists()
		{
			bool error = false;

			if (cbCreatePatchHasCustomMeta.IsChecked == null || !(bool) cbCreatePatchHasCustomMeta.IsChecked ||
			    cboxCreatePatchTargetGame.SelectedIndex >= 4) return true;

			// Check Map Info exists
			if (String.IsNullOrEmpty(txtCreatePatchMapInfo.Text) || !File.Exists(txtCreatePatchMapInfo.Text))
				error = true;

			if (error)
				MetroMessageBox.Show("Unable to make patch",
					"You are missing required blf/mapinfo files. All listed entries must be filled.");

			return !error;
		}

		// Patch Creation
		private void btnCreatePatch_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				// Check the user isn't completly retarded
				if (!CheckAllCreateMandatoryFields())
					return;

				// Check the user isn't a skid
				if (!CheckAllCreateMetaFilesExists())
					return;

				// Paths
				string cleanMapPath = txtCreatePatchUnModifiedMap.Text;
				string moddedMapPath = txtCreatePatchModifiedMap.Text;
				string outputPath = txtCreatePatchOutputPatch.Text;
				string previewImage = txtCreatePatchPreviewImage.Text;

				// Details
				string author = txtCreatePatchContentAuthor.Text;
				string desc = txtCreatePatchContentDescription.Text;
				string name = txtCreatePatchContentName.Text;

				// Make dat patch
				var patch = new Patch
				{
					Author = author,
					Description = desc,
					Name = name,
					Screenshot = String.IsNullOrEmpty(previewImage)
						? null
						: File.ReadAllBytes(previewImage)
				};

				EndianReader originalReader = null;
				EndianReader newReader = null;
				try
				{
					originalReader = new EndianReader(File.OpenRead(cleanMapPath), Endian.BigEndian);
					newReader = new EndianReader(File.OpenRead(moddedMapPath), Endian.BigEndian);

					ICacheFile originalFile = CacheFileLoader.LoadCacheFile(originalReader,
						App.AssemblyStorage.AssemblySettings.DefaultDatabase);
					ICacheFile newFile = CacheFileLoader.LoadCacheFile(newReader, App.AssemblyStorage.AssemblySettings.DefaultDatabase);

					if (cbCreatePatchHasCustomMeta.IsChecked != null && (bool) cbCreatePatchHasCustomMeta.IsChecked &&
					    cboxCreatePatchTargetGame.SelectedIndex < 4)
					{
						var targetGame = (TargetGame) cboxCreatePatchTargetGame.SelectedIndex;
						byte[] mapInfo = File.ReadAllBytes(txtCreatePatchMapInfo.Text);
						var mapInfoFileInfo = new FileInfo(txtCreatePatchMapInfo.Text);
						FileInfo blfFileInfo;

						patch.CustomBlfContent = new BlfContent(mapInfoFileInfo.FullName, mapInfo, targetGame);

						#region Blf Data

						if (PatchCreationBlfOption0.Visibility == Visibility.Visible)
						{
							blfFileInfo = new FileInfo(txtCreatePatchblf0.Text);
							patch.CustomBlfContent.BlfContainerEntries.Add(new BlfContainerEntry(blfFileInfo.Name,
								File.ReadAllBytes(blfFileInfo.FullName)));
						}
						if (PatchCreationBlfOption1.Visibility == Visibility.Visible)
						{
							blfFileInfo = new FileInfo(txtCreatePatchblf1.Text);
							patch.CustomBlfContent.BlfContainerEntries.Add(new BlfContainerEntry(blfFileInfo.Name,
								File.ReadAllBytes(blfFileInfo.FullName)));
						}
						if (PatchCreationBlfOption2.Visibility == Visibility.Visible)
						{
							blfFileInfo = new FileInfo(txtCreatePatchblf2.Text);
							patch.CustomBlfContent.BlfContainerEntries.Add(new BlfContainerEntry(blfFileInfo.Name,
								File.ReadAllBytes(blfFileInfo.FullName)));
						}
						if (PatchCreationBlfOption3.Visibility == Visibility.Visible)
						{
							blfFileInfo = new FileInfo(txtCreatePatchblf3.Text);
							patch.CustomBlfContent.BlfContainerEntries.Add(new BlfContainerEntry(blfFileInfo.Name,
								File.ReadAllBytes(blfFileInfo.FullName)));
						}
						if (PatchCreationBlfOption4.Visibility == Visibility.Visible && cbHasblf4.IsChecked == true)
							{
								blfFileInfo = new FileInfo(txtCreatePatchblf4.Text);
								patch.CustomBlfContent.BlfContainerEntries.Add(new BlfContainerEntry(blfFileInfo.Name,
									File.ReadAllBytes(blfFileInfo.FullName)));
							}

						#endregion
					}

					PatchBuilder.BuildPatch(originalFile, originalReader, newFile, newReader, patch);
				}
				finally
				{
					if (originalReader != null)
						originalReader.Close();
					if (newReader != null)
						newReader.Close();
				}

				IWriter output = new EndianWriter(File.Open(outputPath, FileMode.Create, FileAccess.Write), Endian.BigEndian);
				AssemblyPatchWriter.WritePatch(patch, output);
				output.Close();

				MetroMessageBox.Show("Patch Created!",
					"Your patch has been created in the designated location. Happy sailing, modder!");
			}
			catch (Exception ex)
			{
				MetroException.Show(ex);
			}
		}

		#endregion

		#region Patch Applying Functions

		// File Selectors
		private Patch currentPatch;

		private void btnApplyPatchFile_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Select a Patch file",
				Filter = "Patch Files|*.asmp;*.ascpatch;*.patchdat"
			};
			if (ofd.ShowDialog() != DialogResult.OK) return;
			txtApplyPatchFile.Text = ofd.FileName;
			bool isAlteration = txtApplyPatchFile.Text.EndsWith(".patchdat");

			LoadPatch(isAlteration);
		}

		private void btnApplyPatchUnmodifiedMap_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Select an Unmodified Map File",
				Filter = "Blam Cache Files|*.map"
			};
			if (ofd.ShowDialog() == DialogResult.OK)
				txtApplyPatchUnmodifiedMap.Text = ofd.FileName;
		}

		private void btnApplyPatchOutputMap_Click(object sender, RoutedEventArgs e)
		{
			var sfd = new SaveFileDialog
			{
				Title = "Assembly - Save Modded Map",
				Filter = "Blam Cache Files|*.map"
			};
			if (sfd.ShowDialog() == DialogResult.OK)
				txtApplyPatchOutputMap.Text = sfd.FileName;
		}

		// Meta Sorting

		private void LoadPatch(bool isAlteration)
		{
			try
			{
				using (var reader = new EndianReader(File.OpenRead(txtApplyPatchFile.Text), Endian.LittleEndian))
				{
					string magic = reader.ReadAscii(4);
					reader.SeekTo(0);

					if (magic == "asmp")
					{
						// Load into UI
						reader.Endianness = Endian.BigEndian;
						currentPatch = AssemblyPatchLoader.LoadPatch(reader);
						txtApplyPatchAuthor.Text = currentPatch.Author;
						txtApplyPatchDesc.Text = currentPatch.Description;
						txtApplyPatchName.Text = currentPatch.Name;
						txtApplyPatchInternalName.Text = currentPatch.MapInternalName;
						//txtApplyPatchMapID.Text = currentPatch.MapID.ToString(CultureInfo.InvariantCulture);

						// Set Visibility
						PatchApplicationPatchExtra.Visibility =
							currentPatch.CustomBlfContent != null
								? Visibility.Visible
								: Visibility.Collapsed;
						ApplyPatchControls.Visibility = Visibility.Visible;
					}
					else
					{
						currentPatch = OldPatchLoader.LoadPatch(reader, isAlteration);
						txtApplyPatchAuthor.Text = currentPatch.Author;
						txtApplyPatchDesc.Text = currentPatch.Description;
						txtApplyPatchName.Text = "Ascension/Alteration Patch";
						txtApplyPatchInternalName.Text = "Ascension/Alteration Patch";

						ApplyPatchControls.Visibility = Visibility.Visible;
						PatchApplicationPatchExtra.Visibility = Visibility.Collapsed;
					}
				}

				// Set Screenshot
				if (currentPatch.Screenshot == null)
				{
					// Set default
					var source = new Uri(@"/Assembly;component/Metro/Images/super_patcher.png", UriKind.Relative);
					imgApplyPreview.Source = new BitmapImage(source);
				}
				else
				{
					var image = new BitmapImage();
					image.BeginInit();
					image.StreamSource = new MemoryStream(currentPatch.Screenshot);
					image.EndInit();
					imgApplyPreview.Source = image;
				}
			}
			catch (Exception ex)
			{
				MetroException.Show(ex);
			}
		}

		// Utilities
		private bool CheckAllApplyMandatoryFields()
		{
			bool error = false;

			if (txtApplyPatchFile.Text == null) return false;

			// Check Patch file exists
			if (String.IsNullOrEmpty(txtApplyPatchFile.Text) || !File.Exists(txtApplyPatchFile.Text))
				error = true;

			// Check Modified map exists
			if (String.IsNullOrEmpty(txtApplyPatchUnmodifiedMap.Text) || !File.Exists(txtApplyPatchUnmodifiedMap.Text))
				error = true;

			// Check Content Name is entered
			if (String.IsNullOrEmpty(txtApplyPatchOutputMap.Text))
				error = true;

			if (error)
				MetroMessageBox.Show("Unable to apply patch",
					"Mandatory fields are missing. Please make sure that you have filled out all required fields.");

			return !error;
		}

		// Patch Applying
		private void btnApplyPatch_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				// Check the user isn't completly retarded
				if (!CheckAllApplyMandatoryFields() || currentPatch == null)
					return;

				// Paths
				string unmoddedMapPath = txtApplyPatchUnmodifiedMap.Text;
				string outputPath = txtApplyPatchOutputMap.Text;

				// Copy the original map to the destination path
				File.Copy(unmoddedMapPath, outputPath, true);

				// Open the destination map
				using (var stream = new EndianStream(File.Open(outputPath, FileMode.Open, FileAccess.ReadWrite), Endian.BigEndian))
				{
					EngineDatabase engineDb = XMLEngineDatabaseLoader.LoadDatabase("Formats/Engines.xml");
					ICacheFile cacheFile = CacheFileLoader.LoadCacheFile(stream, engineDb);
					if (currentPatch.MapInternalName != null && cacheFile.InternalName != currentPatch.MapInternalName)
					{
						MetroMessageBox.Show("Unable to apply patch",
							"Hold on there! That patch is for " + currentPatch.MapInternalName +
							".map, and the unmodified map file you selected doesn't seem to match that. Find the correct file and try again.");
						return;
					}

					// Apply the patch!
					if (currentPatch.MapInternalName == null)
						currentPatch.MapInternalName = cacheFile.InternalName;
					// Because Ascension doesn't include this, and ApplyPatch() will complain otherwise

					PatchApplier.ApplyPatch(currentPatch, cacheFile, stream);

					// Check for blf snaps
					if (cbApplyPatchBlfExtraction.IsChecked != null &&
					    (PatchApplicationPatchExtra.Visibility == Visibility.Visible && (bool) cbApplyPatchBlfExtraction.IsChecked))
					{
						string extractDir = Path.GetDirectoryName(outputPath);
						string blfDirectory = Path.Combine(extractDir, "images");
						string infDirectory = Path.Combine(extractDir, "info");
						if (!Directory.Exists(blfDirectory))
							Directory.CreateDirectory(blfDirectory);
						if (!Directory.Exists(infDirectory))
							Directory.CreateDirectory(infDirectory);

						string infPath = Path.Combine(infDirectory, Path.GetFileName(currentPatch.CustomBlfContent.MapInfoFileName));
						File.WriteAllBytes(infPath, currentPatch.CustomBlfContent.MapInfo);

						foreach (BlfContainerEntry blfContainerEntry in currentPatch.CustomBlfContent.BlfContainerEntries)
						{
							string blfPath = Path.Combine(blfDirectory, Path.GetFileName(blfContainerEntry.FileName));
							File.WriteAllBytes(blfPath, blfContainerEntry.BlfContainer);
						}
					}
				}

				MetroMessageBox.Show("Patch Applied!", "Your patch has been applied successfully. Have fun!");
			}
			catch (Exception ex)
			{
				MetroException.Show(ex);
			}
		}

		#endregion

		#region Patch Convertion Functions

		#endregion

		#region Patch Poking Functions

		private Patch currentPatchToPoke;

		private void btnPokePatchFile_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Select a Patch file",
				Filter = "Patch Files|*.asmp"
			};
			if (ofd.ShowDialog() != DialogResult.OK) return;
			txtPokePatchFile.Text = ofd.FileName;

			LoadPatchToPoke();
		}

		private void btnPokePatch_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				// Check the user isn't completly retarded
				if (currentPatchToPoke == null)
					return;

				if (App.AssemblyStorage.AssemblySettings.Xbdm == null ||
				    String.IsNullOrEmpty(App.AssemblyStorage.AssemblySettings.Xbdm.DeviceIdent))
				{
					MetroMessageBox.Show("No Xbox 360 Console Detected",
						"Make sure your xbox 360 console is turned on, and the IP is entered in the App.AssemblyStorage.AssemblySettings.");
					return;
				}

				if (currentPatchToPoke.MetaChangesIndex >= 0)
				{
					if (currentPatchToPoke.SegmentChanges.Count > 1)
						if (MetroMessageBox.Show("Possible unexpected results ahead!",
							"This patch contains edits to segments other than the meta, ie locales and the file header, it could crash if you continue. \n\nDo you wish to continue?",
							MetroMessageBox.MessageBoxButtons.YesNo) == MetroMessageBox.MessageBoxResult.No)
							return;

					SegmentChange changes = currentPatchToPoke.SegmentChanges[currentPatchToPoke.MetaChangesIndex];
					if (changes.OldSize != changes.NewSize)
					{
						// can't poke, patch injects meta
						MetroMessageBox.Show("Unable to Poke Patch",
							"This patch contains meta that has been injected, and can't be poked.");
						return;
					}

					foreach (DataChange change in changes.DataChanges)
					{
						App.AssemblyStorage.AssemblySettings.Xbdm.MemoryStream.Seek(currentPatchToPoke.MetaPokeBase + change.Offset,
							SeekOrigin.Begin);
						App.AssemblyStorage.AssemblySettings.Xbdm.MemoryStream.Write(change.Data, 0x00, change.Data.Length);
					}
				}
				else if (currentPatchToPoke.MetaChanges.Count > 0)
				{
					foreach (DataChange change in currentPatchToPoke.MetaChanges)
					{
						App.AssemblyStorage.AssemblySettings.Xbdm.MemoryStream.Seek(change.Offset, SeekOrigin.Begin);
						App.AssemblyStorage.AssemblySettings.Xbdm.MemoryStream.Write(change.Data, 0x00, change.Data.Length);
					}
				}

				MetroMessageBox.Show("Patch Poked!", "Your patch has been poked successfully. Have fun!");
			}
			catch (Exception ex)
			{
				MetroException.Show(ex);
			}
		}

		private void LoadPatchToPoke()
		{
			try
			{
				using (var reader = new EndianReader(File.OpenRead(txtPokePatchFile.Text), Endian.LittleEndian))
				{
					string magic = reader.ReadAscii(4);
					reader.SeekTo(0);

					if (magic == "asmp")
					{
						// Load into UI
						reader.Endianness = Endian.BigEndian;
						currentPatchToPoke = AssemblyPatchLoader.LoadPatch(reader);
						txtPokePatchAuthor.Text = currentPatchToPoke.Author;
						txtPokePatchDesc.Text = currentPatchToPoke.Description;
						txtPokePatchName.Text = currentPatchToPoke.Name;
						txtPokePatchInternalName.Text = currentPatchToPoke.MapInternalName;
						//txtPokePatchMapID.Text = currentPatchToPoke.MapID.ToString(CultureInfo.InvariantCulture);
					}
					else
					{
						MetroMessageBox.Show("You can't poke a patch from Alteration/Ascention. Convert it to a Assembly Patch first");
						return;
					}
				}

				// Set Screenshot
				if (currentPatchToPoke.Screenshot == null)
				{
					// Set default
					var source = new Uri(@"/Assembly;component/Metro/Images/super_patcher.png", UriKind.Relative);
					imgPokePreview.Source = new BitmapImage(source);
				}
				else
				{
					var image = new BitmapImage();
					image.BeginInit();
					image.StreamSource = new MemoryStream(currentPatchToPoke.Screenshot);
					image.EndInit();
					imgPokePreview.Source = image;
				}
			}
			catch (Exception ex)
			{
				MetroException.Show(ex);
			}
		}

		#endregion

		// ReSharper restore UnusedMember.Global
	}
}