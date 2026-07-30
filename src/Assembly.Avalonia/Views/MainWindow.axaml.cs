using System;
using System.Linq;
using System.Threading.Tasks;
using Assembly.Avalonia.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

namespace Assembly.Avalonia.Views
{
	public partial class MainWindow : Window
	{
		// Remembered pane sizes so a collapse/expand round-trip restores the size the user had,
		// rather than resetting to a hardcoded default every time.
		private double _tagTreeWidth = 280;
		private double _valueSidebarWidth = 320;
		private double _consoleHeight = 170;

		private Grid? _bodyGrid;
		private Grid? _rootGrid;
		private ItemsControl? _tabStrip;
		private PropertiesPanel? _propertiesPanel;

		public MainWindow()
		{
			InitializeComponent();

			_bodyGrid = this.FindControl<Grid>("BodyGrid");
			_rootGrid = this.FindControl<Grid>("RootGrid");
			_propertiesPanel = this.FindControl<PropertiesPanel>("PropertiesPanelControl");

			DataContextChanged += (_, _) =>
			{
				if (Vm != null)
					Vm.PropertyChanged += OnVmPropertyChanged;
			};

			// Optional automated screenshot hook (see docs/dev notes).
			var shot = Environment.GetEnvironmentVariable("ASM_SHOT");
			if (!string.IsNullOrEmpty(shot))
				Opened += async (_, _) => await CaptureAsync(shot);
		}

		private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

		private MainViewModel? Vm => DataContext as MainViewModel;

		private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(MainViewModel.ShowTagTree): ApplyTagTreeWidth(); break;
				case nameof(MainViewModel.ShowValueSidebar): ApplyValueSidebarWidth(); break;
				case nameof(MainViewModel.ShowConsole): ApplyConsoleHeight(); break;
				case nameof(MainViewModel.ActiveDocument): RefreshTabStyles(); break;
				case nameof(MainViewModel.Documents): RefreshTabStyles(); break;
			}
		}

		private void ApplyTagTreeWidth()
		{
			if (_bodyGrid == null || Vm == null) return;
			var col = _bodyGrid.ColumnDefinitions[0];
			if (Vm.ShowTagTree) col.Width = new GridLength(_tagTreeWidth);
			else { if (col.Width.Value > 0) _tagTreeWidth = col.Width.Value; col.Width = new GridLength(0); }
		}

		private void ApplyValueSidebarWidth()
		{
			if (_bodyGrid == null || Vm == null) return;
			var col = _bodyGrid.ColumnDefinitions[4];
			if (Vm.ShowValueSidebar) col.Width = new GridLength(_valueSidebarWidth);
			else { if (col.Width.Value > 0) _valueSidebarWidth = col.Width.Value; col.Width = new GridLength(0); }
		}

		private void ApplyConsoleHeight()
		{
			if (_rootGrid == null || Vm == null) return;
			var row = _rootGrid.RowDefinitions[4];
			if (Vm.ShowConsole) row.Height = new GridLength(_consoleHeight);
			else { if (row.Height.Value > 0) _consoleHeight = row.Height.Value; row.Height = new GridLength(0); }
		}

		// ---- menu / toolbar ----
		private async void OnMenuOpenFile(object? sender, EventArgs e) => await OpenFileViaPickerAsync();
		private async void OnOpenFileClick(object? sender, RoutedEventArgs e) => await OpenFileViaPickerAsync();

		private async void OnMenuOpenFolder(object? sender, EventArgs e) => await OpenFolderViaPickerAsync();
		private async void OnOpenFolderClick(object? sender, RoutedEventArgs e) => await OpenFolderViaPickerAsync();

		private async void OnMenuOpenZip(object? sender, EventArgs e) => await OpenZipViaPickerAsync();
		private async void OnOpenZipClick(object? sender, RoutedEventArgs e) => await OpenZipViaPickerAsync();

		private void OnMenuSave(object? sender, EventArgs e) => SaveAndRefresh();
		private void OnSaveClick(object? sender, RoutedEventArgs e) => SaveAndRefresh();

		private void SaveAndRefresh()
		{
			// Save() reseeds every dirty row's edit state from the freshly-written disk bytes and
			// calls MetaRowViewModel.NotifyEdited() for each one, so the properties sidebar (which
			// subscribes to each row it shows) picks up "unchanged" on its own - nothing further
			// to nudge here.
			Vm?.SaveActive();
		}

		private void OnMenuCloseAll(object? sender, EventArgs e) => Vm?.CloseAll();

		private void OnMenuFocusSearch(object? sender, EventArgs e)
			=> this.FindControl<TextBox>("SearchBox")?.Focus();

		private void OnMenuToggleTagTree(object? sender, EventArgs e) { if (Vm != null) Vm.ShowTagTree = !Vm.ShowTagTree; }
		private void OnMenuToggleValueSidebar(object? sender, EventArgs e) { if (Vm != null) Vm.ShowValueSidebar = !Vm.ShowValueSidebar; }
		private void OnMenuToggleConsole(object? sender, EventArgs e) { if (Vm != null) Vm.ShowConsole = !Vm.ShowConsole; }

		private void OnRevertAllClick(object? sender, RoutedEventArgs e) => Vm?.ActiveDocument?.RevertAll();

		private void OnConsoleClearClick(object? sender, RoutedEventArgs e) => Vm?.Log.Clear();

		private async Task OpenFileViaPickerAsync()
		{
			var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = "Assembly - Open Cache File",
				AllowMultiple = false,
				FileTypeFilter = new[]
				{
					new FilePickerFileType("Halo cache file") { Patterns = new[] { "*.map", "*.yelo", "*.campaign" } },
					FilePickerFileTypes.All
				}
			});

			if (files.FirstOrDefault()?.TryGetLocalPath() is string path && Vm != null)
				await Vm.OpenFileAsync(path);
		}

		private async Task OpenFolderViaPickerAsync()
		{
			var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = "Assembly - Mount Folder (scans recursively for .map / .yelo / .campaign)",
				AllowMultiple = false
			});

			if (folders.FirstOrDefault()?.TryGetLocalPath() is string path && Vm != null)
				await Vm.OpenFolderAsync(path);
		}

		private async Task OpenZipViaPickerAsync()
		{
			var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = "Assembly - Mount Zip",
				AllowMultiple = false,
				FileTypeFilter = new[] { new FilePickerFileType("Zip archive") { Patterns = new[] { "*.zip" } } }
			});

			if (files.FirstOrDefault()?.TryGetLocalPath() is string path && Vm != null)
				await Vm.OpenZipAsync(path);
		}

		// ---- tag tree ----
		private void OnTreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
		{
			if (Vm == null) return;
			if ((sender as TreeView)?.SelectedItem is TagNode node)
				Vm.SelectedTag = node;
		}

		// ---- tabs ----
		private void OnTabClick(object? sender, RoutedEventArgs e)
		{
			if (Vm != null && (sender as Button)?.Tag is TagDocumentViewModel doc)
				Vm.ActiveDocument = doc;
		}

		private void OnTabCloseClick(object? sender, RoutedEventArgs e)
		{
			e.Handled = true; // don't let the click bubble to the parent tab button
			if (Vm != null && (sender as Button)?.Tag is TagDocumentViewModel doc)
				Vm.CloseDocument(doc);
		}

		private void RefreshTabStyles()
		{
			if (_tabStrip == null)
				_tabStrip = this.GetVisualDescendants().OfType<ItemsControl>()
					.FirstOrDefault(ic => ic.ItemsSource == Vm?.Documents);
			if (_tabStrip == null) return;

			foreach (var container in _tabStrip.GetRealizedContainers())
			{
				var button = (container as ContentPresenter)?.GetVisualDescendants().OfType<Button>().FirstOrDefault()
				             ?? container as Button;
				if (button?.Tag is TagDocumentViewModel doc)
				{
					if (doc == Vm?.ActiveDocument) button.Classes.Add("active");
					else button.Classes.Remove("active");
				}
			}
		}

		// ---- field table / tag block navigation ----
		//
		// The properties sidebar (PropertiesPanel) now binds directly to ActiveDocument and
		// reacts to TagDocumentViewModel.SelectedRow / MetaRowViewModel property-change
		// notifications on its own, so selecting a row or expanding a block no longer needs an
		// explicit rebuild call from here - see PropertiesPanel.axaml.cs. This handler stays only
		// because FieldList's SelectionChanged is wired to it in XAML outside the region this
		// file's owner may touch; ListBox.SelectedItem's own TwoWay binding already keeps
		// ActiveDocument.SelectedRow in sync without any code here.
		private void OnRowSelectionChanged(object? sender, SelectionChangedEventArgs e)
		{
		}

		private void OnBlockExpandClick(object? sender, RoutedEventArgs e)
		{
			if ((sender as ToggleButton)?.Tag is MetaRowViewModel row && Vm?.ActiveDocument != null)
				Vm.ActiveDocument.ToggleExpand(row);
		}

		private async Task CaptureAsync(string path)
		{
			var open = Environment.GetEnvironmentVariable("ASM_OPEN");
			var folder = Environment.GetEnvironmentVariable("ASM_FOLDER");
			var zip = Environment.GetEnvironmentVariable("ASM_ZIP");

			if (Vm != null)
			{
				if (!string.IsNullOrEmpty(open)) await Vm.OpenFileAsync(open);
				if (!string.IsNullOrEmpty(folder)) await Vm.OpenFolderAsync(folder);
				if (!string.IsNullOrEmpty(zip)) await Vm.OpenZipAsync(zip);
				await Task.Delay(200);

				var mode = Environment.GetEnvironmentVariable("ASM_MODE");
				if (!string.IsNullOrEmpty(mode)) Vm.TreeMode = mode;

				var select = Environment.GetEnvironmentVariable("ASM_SELECT");
				if (!string.IsNullOrEmpty(select))
				{
					var tree = this.FindControl<TreeView>("TagTree")!;
					UpdateLayout();
					foreach (var c in tree.GetRealizedContainers())
						if (c is TreeViewItem tvi) tvi.IsExpanded = true;
					UpdateLayout();
					await Task.Delay(200);

					// Comma-separated: opens one tab per name, in order, so a single
					// screenshot can show several tabs (the "tabs inside the main view"
					// requirement) rather than just one document.
					foreach (var needle in select.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
					{
						var tag = Vm.FindTag(needle);
						if (tag != null) Vm.SelectedTag = tag;
					}
				}

				await Task.Delay(150);

				var expand = Environment.GetEnvironmentVariable("ASM_EXPAND");
				if (!string.IsNullOrEmpty(expand) && Vm.ActiveDocument != null)
				{
					var blockRow = Vm.ActiveDocument.Rows.FirstOrDefault(r => r.IsBlock && r.Name.Contains(expand, StringComparison.OrdinalIgnoreCase));
					if (blockRow != null)
					{
						Vm.ActiveDocument.ToggleExpand(blockRow);
						Vm.ActiveDocument.SelectedRow = blockRow;

						var elementIndexStr = Environment.GetEnvironmentVariable("ASM_ELEMENT");
						if (!string.IsNullOrEmpty(elementIndexStr) && int.TryParse(elementIndexStr, out var elIdx))
							Vm.ActiveDocument.SetElementIndex(blockRow, elIdx);
					}
				}

				var editField = Environment.GetEnvironmentVariable("ASM_EDIT");
				if (!string.IsNullOrEmpty(editField) && Vm.ActiveDocument != null)
				{
					var target = Vm.ActiveDocument.Rows.FirstOrDefault(r => r.Name.Equals(editField, StringComparison.OrdinalIgnoreCase));
					if (target != null)
					{
						Vm.ActiveDocument.SelectedRow = target;
						var fieldList = this.FindControl<ListBox>("FieldList");
						fieldList?.ScrollIntoView(target);

						// Drives the same TextChanged handler a real keystroke would, so a
						// screenshot can show a genuinely dirty, validated edit rather than a
						// static mock of one.
						var editValue = Environment.GetEnvironmentVariable("ASM_EDIT_VALUE");
						if (editValue != null && _propertiesPanel != null)
						{
							var box = _propertiesPanel.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
							if (box != null) box.Text = editValue;
							// TextChanged is dispatched, not synchronous with the property set above;
							// give it a turn of the UI loop before checking dirty state / saving.
							await Task.Delay(50);
						}

						if (Environment.GetEnvironmentVariable("ASM_SAVE") == "1")
						{
							var (ok, message) = Vm.ActiveDocument.Save();
							Console.WriteLine($"ASM_SAVE result: ok={ok} message=\"{message}\"");
						}
					}
				}

				var consoleFlag = Environment.GetEnvironmentVariable("ASM_CONSOLE");
				if (!string.IsNullOrEmpty(consoleFlag)) Vm.ShowConsole = consoleFlag != "0";

				var tagTreeFlag = Environment.GetEnvironmentVariable("ASM_TAGTREE");
				if (!string.IsNullOrEmpty(tagTreeFlag)) Vm.ShowTagTree = tagTreeFlag != "0";

				var sidebarFlag = Environment.GetEnvironmentVariable("ASM_SIDEBAR");
				if (!string.IsNullOrEmpty(sidebarFlag)) Vm.ShowValueSidebar = sidebarFlag != "0";
			}

			await Task.Delay(400);
			try
			{
				var px = new PixelSize((int)Bounds.Width, (int)Bounds.Height);
				using var rtb = new global::Avalonia.Media.Imaging.RenderTargetBitmap(px, new Vector(96, 96));
				UpdateLayout();
				rtb.Render(this);
				rtb.Save(path);
				Console.WriteLine($"wrote {path} ({px.Width}x{px.Height})");
			}
			catch (Exception ex)
			{
				Console.WriteLine("render failed: " + ex);
			}

			Close();
		}
	}
}
