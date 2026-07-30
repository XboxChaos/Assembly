using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Assembly.Avalonia.Services;
using Assembly.Avalonia.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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

		// ---- console filter/copy/auto-scroll state (see the console block's comment in
		// MainWindow.axaml for why this is a code-behind-managed list rather than a direct
		// binding to Log.Entries) ----
		private readonly ObservableCollection<LogEntry> _consoleFiltered = new();
		private ListBox? _consoleList;
		private TextBox? _consoleFilterBox;
		private ComboBox? _consoleLevelBox;

		public MainWindow()
		{
			InitializeComponent();

			_bodyGrid = this.FindControl<Grid>("BodyGrid");
			_rootGrid = this.FindControl<Grid>("RootGrid");
			_propertiesPanel = this.FindControl<PropertiesPanel>("PropertiesPanelControl");
			_consoleList = this.FindControl<ListBox>("ConsoleList");
			_consoleFilterBox = this.FindControl<TextBox>("ConsoleFilterBox");
			_consoleLevelBox = this.FindControl<ComboBox>("ConsoleLevelBox");
			if (_consoleList != null) _consoleList.ItemsSource = _consoleFiltered;

			DataContextChanged += (_, _) =>
			{
				if (Vm != null)
				{
					Vm.PropertyChanged += OnVmPropertyChanged;
					Vm.Log.Entries.CollectionChanged += OnConsoleEntriesChanged;
					RefreshConsoleFilter(pinToTail: true);
				}
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

		// ---- Campaign Evolved unpack / repack ----
		// Public and parameterless-at-the-call-site by design (see the remarks on each) so the
		// command-palette pass can register them as commands without this window needing to know
		// anything about how a command is invoked.
		private void OnMenuUnpack(object? sender, EventArgs e) => ShowCEUnpackDialog();
		private void OnUnpackClick(object? sender, RoutedEventArgs e) => ShowCEUnpackDialog();
		private void OnMenuRepack(object? sender, EventArgs e) => ShowCERepackDialog();
		private void OnRepackClick(object? sender, RoutedEventArgs e) => ShowCERepackDialog();

		/// <summary>
		///     Opens the Campaign Evolved unpack dialog (<see cref="CEPackagingDialog" />, <see cref="CEPackagingMode.Unpack" />).
		/// </summary>
		/// <remarks>
		///     Does not require a Campaign Evolved source to already be mounted in the tag tree - the dialog has its
		///     own folder picker - but pre-fills it with the mount directory of the first fifth-generation source
		///     already open, if there is one, as a convenience.
		/// </remarks>
		public void ShowCEUnpackDialog() => ShowCEPackagingDialog(CEPackagingMode.Unpack);

		/// <summary>Opens the Campaign Evolved repack dialog (<see cref="CEPackagingDialog" />, <see cref="CEPackagingMode.Repack" />).</summary>
		public void ShowCERepackDialog() => ShowCEPackagingDialog(CEPackagingMode.Repack);

		private void ShowCEPackagingDialog(CEPackagingMode mode)
		{
			var db = EngineDatabaseService.Database;
			if (db == null)
			{
				Vm?.Log.Error("Cannot open the Campaign Evolved packaging dialog: the engine database failed to load.");
				return;
			}

			string? initialSource = Vm?.Sources
				.SelectMany(s => s.Sessions)
				.FirstOrDefault(s => s.Cache is Blamite.Blam.FifthGen.FifthGenCacheFile)
				?.Cache is Blamite.Blam.FifthGen.FifthGenCacheFile fifthGen
				? fifthGen.MountDirectory
				: null;

			var dialog = new CEPackagingDialog(mode, db, initialSource);
			dialog.ShowDialog(this);
		}

		private void OnMenuFocusSearch(object? sender, EventArgs e)
			=> this.FindControl<TextBox>("SearchBox")?.Focus();

		private void OnMenuToggleTagTree(object? sender, EventArgs e) { if (Vm != null) Vm.ShowTagTree = !Vm.ShowTagTree; }
		private void OnMenuToggleValueSidebar(object? sender, EventArgs e) { if (Vm != null) Vm.ShowValueSidebar = !Vm.ShowValueSidebar; }
		private void OnMenuToggleConsole(object? sender, EventArgs e) { if (Vm != null) Vm.ShowConsole = !Vm.ShowConsole; }

		private void OnRevertAllClick(object? sender, RoutedEventArgs e) => Vm?.ActiveDocument?.RevertAll();

		private void OnConsoleClearClick(object? sender, RoutedEventArgs e) => Vm?.Log.Clear();

		// ---- console filter / copy / auto-scroll ----

		private void OnConsoleEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			// LogService.Add (ViewModels/) is reached from background work (e.g.
			// TagNamespace.MountFolder runs off the UI thread), so this event can fire from any
			// thread; RefreshConsoleFilter touches DataContext and Avalonia controls, both of
			// which assert UI-thread affinity, so it has to be marshalled rather than called
			// directly.
			if (Dispatcher.UIThread.CheckAccess()) RefreshConsoleFilter();
			else Dispatcher.UIThread.Post(() => RefreshConsoleFilter());
		}

		private void OnConsoleFilterChanged(object? sender, TextChangedEventArgs e) => RefreshConsoleFilter();
		private void OnConsoleFilterChanged(object? sender, SelectionChangedEventArgs e) => RefreshConsoleFilter();

		/// <summary>
		///     Rebuilds <see cref="_consoleFiltered" /> from <c>Log.Entries</c> against the current
		///     text/level filters, then decides whether to scroll to the new last line. "Sensibly"
		///     auto-scrolling (per the brief) means not yanking the view away from a line a reader
		///     is deliberately looking at further up in the history - so this only follows the tail
		///     when the viewport was already within a couple of lines of the bottom before the
		///     refresh, or when <paramref name="pinToTail" /> forces it (initial load).
		/// </summary>
		private void RefreshConsoleFilter(bool pinToTail = false)
		{
			if (_consoleList == null || Vm == null) return;

			var minLevel = _consoleLevelBox?.SelectedIndex switch
			{
				1 => LogLevel.Warn,
				2 => LogLevel.Error,
				_ => LogLevel.Info
			};
			var needle = _consoleFilterBox?.Text;

			bool wasNearBottom = pinToTail || IsConsoleScrolledNearBottom();

			_consoleFiltered.Clear();
			// Snapshot rather than enumerate Log.Entries directly: LogService.Add (ViewModels/,
			// not owned by this pass) is reached from background work with no synchronization,
			// so a concurrent Add during this foreach is a real possibility, not a hypothetical.
			foreach (var entry in Vm.Log.Entries.ToList())
			{
				if (entry.Level < minLevel) continue;
				if (!string.IsNullOrWhiteSpace(needle) &&
				    entry.Message.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0) continue;
				_consoleFiltered.Add(entry);
			}

			if (wasNearBottom && _consoleFiltered.Count > 0)
				_consoleList.ScrollIntoView(_consoleFiltered[^1]);
		}

		private bool IsConsoleScrolledNearBottom()
		{
			var sv = _consoleList?.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
			// No realized ScrollViewer yet (first layout, or the console is hidden) - default to
			// "yes", so the very first entries land visible rather than requiring a manual scroll.
			if (sv == null) return true;
			const double bottomSlack = 24; // px - "close enough to the bottom" to still count as pinned
			return sv.Offset.Y + sv.Viewport.Height >= sv.Extent.Height - bottomSlack;
		}

		private async void OnConsoleCopyClick(object? sender, RoutedEventArgs e)
		{
			var text = string.Join(Environment.NewLine,
				_consoleFiltered.Select(entry => $"[{entry.TimeLabel}] {entry.LevelLabel,-5} {entry.Message}"));
			var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
			if (clipboard != null && text.Length > 0)
				await clipboard.SetTextAsync(text);
		}

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

		/// <summary>Middle-click-to-close: <see cref="Button.Click" /> only ever fires for the
		/// primary button, so a tab needs its own pointer handler to notice the middle button.</summary>
		private void OnTabPointerPressed(object? sender, PointerPressedEventArgs e)
		{
			if (Vm == null || sender is not Button { Tag: TagDocumentViewModel doc } button) return;
			if (!e.GetCurrentPoint(button).Properties.IsMiddleButtonPressed) return;
			e.Handled = true;
			Vm.CloseDocument(doc);
		}

		/// <summary>Redirects vertical wheel/trackpad delta to horizontal scroll on the tab strip,
		/// so "many tags open" overflows to something a plain mouse wheel can actually reach - a
		/// horizontal-only ScrollViewer otherwise ignores vertical wheel input entirely.</summary>
		private void OnTabStripWheel(object? sender, PointerWheelEventArgs e)
		{
			if (sender is not ScrollViewer sv) return;
			double delta = Math.Abs(e.Delta.Y) >= Math.Abs(e.Delta.X) ? e.Delta.Y : e.Delta.X;
			if (delta == 0) return;
			sv.Offset = new Vector(Math.Max(0, sv.Offset.X - delta * 48), sv.Offset.Y);
			e.Handled = true;
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

		// ---- in-tag field filter ----
		private void OnFieldFilterKeyDown(object? sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape && Vm?.ActiveDocument != null)
			{
				Vm.ActiveDocument.FilterQuery = "";
				e.Handled = true;
			}
		}

		// ---- tag reference navigation ----
		private void OnFollowReferenceClick(object? sender, RoutedEventArgs e)
		{
			if ((sender as Button)?.Tag is MetaRowViewModel row)
				FollowReference(row);
		}

		private void OnFieldListDoubleTapped(object? sender, TappedEventArgs e)
		{
			if ((sender as ListBox)?.SelectedItem is MetaRowViewModel row)
				FollowReference(row);
		}

		private void FollowReference(MetaRowViewModel row)
		{
			if (Vm == null || row.RefTarget == null) return;
			Vm.NavigateToTagReference(row.RefTarget);
		}

		// ---- field table keyboard navigation: arrows move selection (Avalonia's ListBox already
		// does that), Left/Right collapse/expand or step to the parent row, Enter follows a
		// reference or toggles a block, matching the ToggleButton/"open" glyph the mouse already has.
		// Cmd+[ / Cmd+] also drive tag-navigation back/forward here - macOS's own convention for
		// history navigation (Safari, Xcode) and this shell is native-chrome, macOS-first - since
		// there is nowhere in this file's owned elements to put a visible back/forward button (the
		// header band and toolbar are a different agent's region). ----
		private void OnFieldListKeyDown(object? sender, KeyEventArgs e)
		{
			if (Vm != null && e.KeyModifiers.HasFlag(KeyModifiers.Meta))
			{
				if (e.Key == Key.OemOpenBrackets) { Vm.GoBack(); e.Handled = true; return; }
				if (e.Key == Key.OemCloseBrackets) { Vm.GoForward(); e.Handled = true; return; }
			}

			if (sender is not ListBox listBox || Vm?.ActiveDocument is not { } doc) return;
			if (listBox.SelectedItem is not MetaRowViewModel row) return;

			switch (e.Key)
			{
				case Key.Right when row.IsBlock && !row.IsExpanded:
					doc.ToggleExpand(row);
					e.Handled = true;
					break;

				case Key.Left when row.IsBlock && row.IsExpanded:
					doc.ToggleExpand(row);
					e.Handled = true;
					break;

				case Key.Left when row.Depth > 0:
				{
					// Nothing to collapse on a leaf/collapsed row - step selection up to the
					// nearest visible ancestor instead, the way a file tree's Left arrow does once
					// a node is already collapsed.
					int idx = doc.Rows.IndexOf(row);
					for (int i = idx - 1; i >= 0; i--)
					{
						if (doc.Rows[i].Depth < row.Depth)
						{
							doc.SelectedRow = doc.Rows[i];
							listBox.ScrollIntoView(doc.Rows[i]);
							break;
						}
					}
					e.Handled = true;
					break;
				}

				case Key.Enter when row.RefTarget != null:
					FollowReference(row);
					e.Handled = true;
					return; // navigation may switch ActiveDocument - the sidebar refresh below is only for this document

				case Key.Enter when row.IsBlock:
					doc.ToggleExpand(row);
					e.Handled = true;
					break;
			}
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

			// ASM_CE_DIALOG=unpack|repack screenshots CEPackagingDialog instead of this window - a
			// dialog is a separate native top-level, so it needs its own RenderTargetBitmap.Render
			// call rather than being reachable through this window's own visual tree. ASM_CE_TAGS /
			// ASM_CE_OUTPUT prefill the repack-only / shared fields; ASM_CE_PREVIEW=1 runs the same
			// preview OnPreviewClick would, so the screenshot shows real preview output rather than
			// an empty form.
			var ceDialogMode = Environment.GetEnvironmentVariable("ASM_CE_DIALOG");
			if (!string.IsNullOrEmpty(ceDialogMode))
			{
				await CaptureCEPackagingDialogAsync(ceDialogMode, path);
				return;
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

		private async Task CaptureCEPackagingDialogAsync(string modeArg, string path)
		{
			var db = EngineDatabaseService.Database;
			if (db == null)
			{
				Console.WriteLine("ASM_CE_DIALOG: engine database failed to load, cannot open the dialog.");
				Close();
				return;
			}

			var mode = string.Equals(modeArg, "repack", StringComparison.OrdinalIgnoreCase) ? CEPackagingMode.Repack : CEPackagingMode.Unpack;
			var initialSource = Environment.GetEnvironmentVariable("ASM_FOLDER") ?? Environment.GetEnvironmentVariable("ASM_CE_SOURCE");

			var dialog = new CEPackagingDialog(mode, db, initialSource);
			dialog.Show(this);
			await Task.Delay(200);

			var tagsDir = Environment.GetEnvironmentVariable("ASM_CE_TAGS");
			if (!string.IsNullOrEmpty(tagsDir)) dialog.Vm.TagsDirectory = tagsDir;
			var outputDir = Environment.GetEnvironmentVariable("ASM_CE_OUTPUT");
			if (!string.IsNullOrEmpty(outputDir)) dialog.Vm.OutputDirectory = outputDir;

			if (Environment.GetEnvironmentVariable("ASM_CE_PREVIEW") == "1")
			{
				await dialog.TriggerPreviewForScreenshotAsync();
				await Task.Delay(200);
			}

			if (Environment.GetEnvironmentVariable("ASM_CE_RUN") == "1")
			{
				await dialog.TriggerRunForScreenshotAsync();
				await Task.Delay(200);
			}

			try
			{
				var px = new PixelSize((int)dialog.Bounds.Width, (int)dialog.Bounds.Height);
				using var rtb = new global::Avalonia.Media.Imaging.RenderTargetBitmap(px, new Vector(96, 96));
				dialog.UpdateLayout();
				rtb.Render(dialog);
				rtb.Save(path);
				Console.WriteLine($"wrote {path} ({px.Width}x{px.Height}) [CEPackagingDialog, mode={mode}]");
			}
			catch (Exception ex)
			{
				Console.WriteLine("render failed: " + ex);
			}

			dialog.Close();
			Close();
		}
	}
}
