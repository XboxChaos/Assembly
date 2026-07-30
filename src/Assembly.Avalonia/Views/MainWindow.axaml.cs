using System;
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
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
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
		private StackPanel? _editorHost;

		public MainWindow()
		{
			InitializeComponent();

			_bodyGrid = this.FindControl<Grid>("BodyGrid");
			_rootGrid = this.FindControl<Grid>("RootGrid");
			_editorHost = this.FindControl<StackPanel>("EditorHost");

			DataContextChanged += (_, _) =>
			{
				if (Vm != null)
					Vm.PropertyChanged += OnVmPropertyChanged;
			};

			BuildEditor(null);

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
			Vm?.SaveActive();
			// Save() reseeds every row's edit state from the freshly-written disk bytes, which
			// clears dirty - but the sidebar was built for the pre-save row instance's state, so
			// rebuild it to show "unchanged" instead of a stale "* modified".
			BuildEditor(Vm?.ActiveDocument?.SelectedRow);
		}

		private void OnMenuCloseAll(object? sender, EventArgs e) => Vm?.CloseAll();

		private void OnMenuFocusSearch(object? sender, EventArgs e)
			=> this.FindControl<TextBox>("SearchBox")?.Focus();

		private void OnMenuToggleTagTree(object? sender, EventArgs e) { if (Vm != null) Vm.ShowTagTree = !Vm.ShowTagTree; }
		private void OnMenuToggleValueSidebar(object? sender, EventArgs e) { if (Vm != null) Vm.ShowValueSidebar = !Vm.ShowValueSidebar; }
		private void OnMenuToggleConsole(object? sender, EventArgs e) { if (Vm != null) Vm.ShowConsole = !Vm.ShowConsole; }

		private void OnRevertAllClick(object? sender, RoutedEventArgs e)
		{
			Vm?.ActiveDocument?.RevertAll();
			BuildEditor(Vm?.ActiveDocument?.SelectedRow);
		}

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
		private void OnRowSelectionChanged(object? sender, SelectionChangedEventArgs e)
		{
			var row = (sender as ListBox)?.SelectedItem as MetaRowViewModel;
			BuildEditor(row);
		}

		private void OnBlockExpandClick(object? sender, RoutedEventArgs e)
		{
			if ((sender as ToggleButton)?.Tag is MetaRowViewModel row && Vm?.ActiveDocument != null)
			{
				Vm.ActiveDocument.ToggleExpand(row);
				if (Vm.ActiveDocument.SelectedRow == row) BuildEditor(row);
			}
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

			if (e.Handled && doc.SelectedRow == row) BuildEditor(row);
		}

		// ================= value / properties sidebar =================
		//
		// Built imperatively rather than through XAML DataTemplates: the row's editable value
		// lives in a plain (non-bindable-by-field) FieldEditState, and each field kind needs its
		// own inline-validated widget set (flags -> a checkbox per bit computed from the schema,
		// enum -> a live dropdown, vectors -> N numeric boxes, etc). Building it in code keeps
		// that per-kind logic in one place and matches this codebase's existing convention of
		// plain code-behind event handlers rather than an ICommand/MVVM framework.

		private void BuildEditor(MetaRowViewModel? row)
		{
			if (_editorHost == null) return;
			_editorHost.Children.Clear();
			_refreshDirtyFooter = null;

			var doc = Vm?.ActiveDocument;
			if (row == null || doc == null)
			{
				_editorHost.Children.Add(Hint("Select a field in the table to edit its value here."));
				return;
			}

			_editorHost.Children.Add(BuildHeader(row));

			switch (row.Editor)
			{
				case EditorKind.Integer: AddIntegerEditor(row, doc); break;
				case EditorKind.Float: AddFloatComponents(row, doc, "Value"); break;
				case EditorKind.Vector2: AddFloatComponents(row, doc, "X", "Y"); break;
				case EditorKind.Vector3: AddFloatComponents(row, doc, "X", "Y", "Z"); break;
				case EditorKind.Vector4: AddFloatComponents(row, doc, "X", "Y", "Z", "W"); break;
				case EditorKind.RangeFloat: AddFloatComponents(row, doc, "Min", "Max"); break;
				case EditorKind.RangeInt16: AddShortRangeEditor(row, doc); break;
				case EditorKind.Enum: AddEnumEditor(row, doc); break;
				case EditorKind.Flags: AddFlagsEditor(row, doc); break;
				case EditorKind.Color: AddColorEditor(row, doc); break;
				case EditorKind.Ascii: AddTextEditor(row, doc, ascii: true); break;
				case EditorKind.Utf16: AddTextEditor(row, doc, ascii: false); break;
				case EditorKind.StringId: AddStringIdEditor(row, doc); break;
				case EditorKind.Block: AddBlockNavEditor(row, doc); break;
				default: AddReadOnlyNotice(row); break;
			}

			if (row.Def.IsEditable)
				_editorHost.Children.Add(BuildFooter(row, doc));
		}

		private static Control Hint(string text) => new TextBlock
		{
			Text = text, Classes = { "label" }, TextWrapping = TextWrapping.Wrap
		};

		private static readonly FontFamily SemiBoldFont = new("avares://AssemblyAvalonia/Assets/Fonts#Selawik Semibold");
		private static readonly IBrush SeparatorBrush = new SolidColorBrush(Color.Parse("#FF46464a"));

		private Control BuildHeader(MetaRowViewModel row)
		{
			var panel = new StackPanel { Spacing = 2, Margin = new Thickness(0, 0, 0, 4) };
			panel.Children.Add(new TextBlock { Text = row.Name, FontFamily = SemiBoldFont, FontSize = 14 });
			panel.Children.Add(new TextBlock { Text = row.KindLabel, Classes = { "label" }, FontSize = 11 });
			panel.Children.Add(new SelectableTextBlock { Text = row.OffsetLabel, Classes = { "mono", "dim" }, FontSize = 10.5 });
			if (!string.IsNullOrWhiteSpace(row.Def.Tooltip))
				panel.Children.Add(new TextBlock { Text = row.Def.Tooltip, Classes = { "label" }, FontSize = 10.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });
			panel.Children.Add(new Border { Height = 1, Background = SeparatorBrush, Margin = new Thickness(0, 6, 0, 0) });
			return panel;
		}

		// Set by BuildFooter each time the editor panel is (re)built for a row; Commit() calls
		// this instead of rebuilding the whole panel on every keystroke, so typing doesn't tear
		// down and recreate the very TextBox the user is typing into (which both steals focus
		// and, transitively through re-entrant control attach/detach, is a real hang risk).
		private Action? _refreshDirtyFooter;

		private Control BuildFooter(MetaRowViewModel row, TagDocumentViewModel doc)
		{
			var bar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 8, 0, 0) };
			var dirtyLabel = new TextBlock { Classes = { "label" }, VerticalAlignment = VerticalAlignment.Center, FontSize = 10.5 };
			var revert = new Button { Content = "Revert", IsEnabled = row.IsDirty };

			void RefreshDirty()
			{
				dirtyLabel.Text = row.IsDirty ? "* modified (not saved)" : "unchanged";
				revert.IsEnabled = row.IsDirty;
			}
			RefreshDirty();
			_refreshDirtyFooter = RefreshDirty;

			revert.Click += (_, _) => { row.Revert(); doc.RecomputeDirty(); RefreshDirty(); };

			bar.Children.Add(revert);
			bar.Children.Add(dirtyLabel);
			return bar;
		}

		private void Commit(MetaRowViewModel row, TagDocumentViewModel doc)
		{
			row.NotifyEdited();
			doc.RecomputeDirty();
			_refreshDirtyFooter?.Invoke();
		}

		private static TextBox NumberBox(string initial) => new()
		{
			Text = initial, Width = 140, HorizontalAlignment = HorizontalAlignment.Left
		};

		private static TextBlock ErrorText() => new()
		{
			Classes = { "label" }, Foreground = Brushes.OrangeRed, FontSize = 10.5, IsVisible = false, TextWrapping = TextWrapping.Wrap
		};

		private void AddLabeledRow(string label, Control editor, TextBlock? error = null)
		{
			var row = new StackPanel { Spacing = 3, Margin = new Thickness(0, 0, 0, 6) };
			row.Children.Add(new TextBlock { Text = label, Classes = { "label" }, FontSize = 10.5 });
			row.Children.Add(editor);
			if (error != null) row.Children.Add(error);
			_editorHost!.Children.Add(row);
		}

		private void AddIntegerEditor(MetaRowViewModel row, TagDocumentViewModel doc)
		{
			var edit = row.Current!;
			var box = NumberBox(edit.Int?.ToString(CultureInfo.InvariantCulture) ?? "0");
			var err = ErrorText();
			box.TextChanged += (_, _) =>
			{
				if (long.TryParse(box.Text, NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v)
				    || TryParseHex(box.Text, out v))
				{
					edit.Int = v;
					err.IsVisible = false;
					box.Classes.Remove("invalid");
					Commit(row, doc);
				}
				else
				{
					err.Text = "not a valid integer (decimal or 0x hex)";
					err.IsVisible = true;
					box.Classes.Add("invalid");
				}
			};
			AddLabeledRow("Value", box, err);
		}

		private static bool TryParseHex(string? text, out long value)
		{
			value = 0;
			if (text == null) return false;
			var t = text.Trim();
			if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				return long.TryParse(t[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
			return false;
		}

		private void AddFloatComponents(MetaRowViewModel row, TagDocumentViewModel doc, params string[] labels)
		{
			var edit = row.Current!;
			for (int i = 0; i < labels.Length; i++)
			{
				int idx = i;
				float initial = edit.Floats != null && idx < edit.Floats.Length ? edit.Floats[idx] : 0f;
				var box = NumberBox(initial.ToString("0.######", CultureInfo.InvariantCulture));
				var err = ErrorText();
				box.TextChanged += (_, _) =>
				{
					if (float.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && edit.Floats != null && idx < edit.Floats.Length)
					{
						edit.Floats[idx] = v;
						err.IsVisible = false;
						box.Classes.Remove("invalid");
						Commit(row, doc);
					}
					else
					{
						err.Text = "not a valid number";
						err.IsVisible = true;
						box.Classes.Add("invalid");
					}
				};
				AddLabeledRow(labels[idx], box, err);
			}
		}

		private void AddShortRangeEditor(MetaRowViewModel row, TagDocumentViewModel doc)
		{
			var edit = row.Current!;
			AddShortBox("Min", () => edit.ShortLo, v => edit.ShortLo = v, row, doc);
			AddShortBox("Max", () => edit.ShortHi, v => edit.ShortHi = v, row, doc);
		}

		private void AddShortBox(string label, Func<short> get, Action<short> set, MetaRowViewModel row, TagDocumentViewModel doc)
		{
			var box = NumberBox(get().ToString(CultureInfo.InvariantCulture));
			var err = ErrorText();
			box.TextChanged += (_, _) =>
			{
				if (short.TryParse(box.Text, NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v))
				{
					set(v);
					err.IsVisible = false;
					box.Classes.Remove("invalid");
					Commit(row, doc);
				}
				else
				{
					err.Text = "not a valid 16-bit integer";
					err.IsVisible = true;
					box.Classes.Add("invalid");
				}
			};
			AddLabeledRow(label, box, err);
		}

		private void AddEnumEditor(MetaRowViewModel row, TagDocumentViewModel doc)
		{
			var edit = row.Current!;
			var combo = new ComboBox { ItemsSource = row.Choices, HorizontalAlignment = HorizontalAlignment.Stretch };
			combo.SelectedItem = row.Choices.FirstOrDefault(c => c.Value == edit.Int);
			combo.SelectionChanged += (_, _) =>
			{
				if (combo.SelectedItem is ChoiceOption c)
				{
					edit.Int = c.Value;
					Commit(row, doc);
				}
			};
			AddLabeledRow("Selected option", combo);

			if (combo.SelectedItem == null && edit.Int != null)
				AddLabeledRow("Raw value (no matching option)", new TextBlock { Text = edit.Int.ToString(), Classes = { "mono" } });
		}

		private void AddFlagsEditor(MetaRowViewModel row, TagDocumentViewModel doc)
		{
			var edit = row.Current!;
			var list = new StackPanel { Spacing = 2 };
			foreach (var choice in row.Choices)
			{
				var cb = new CheckBox
				{
					Content = choice.Name,
					IsChecked = ((edit.Int ?? 0) & choice.Value) != 0,
					FontSize = 11.5
				};
				cb.PropertyChanged += (_, ev) =>
				{
					if (ev.Property != ToggleButton.IsCheckedProperty) return;
					if (cb.IsChecked == true) edit.Int = (edit.Int ?? 0) | choice.Value;
					else edit.Int = (edit.Int ?? 0) & ~choice.Value;
					Commit(row, doc);
				};
				list.Children.Add(cb);
			}
			if (row.Choices.Count == 0)
				list.Children.Add(Hint("This bitfield has no named bits in the plugin."));
			_editorHost!.Children.Add(list);
		}

		private void AddColorEditor(MetaRowViewModel row, TagDocumentViewModel doc)
		{
			var edit = row.Current!;
			uint raw = (uint)(edit.Int ?? 0);
			bool hasAlpha = row.Def.Note == "argb";

			var swatch = new Border { Width = 32, Height = 22, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
			void Repaint(uint v) => swatch.Background = new SolidColorBrush(new Color(
				hasAlpha ? (byte)(v >> 24) : (byte)255, (byte)(v >> 16), (byte)(v >> 8), (byte)v));
			Repaint(raw);

			var hexBox = NumberBox($"{raw:X8}");
			var err = ErrorText();
			var row1 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
			row1.Children.Add(swatch);
			row1.Children.Add(hexBox);

			hexBox.TextChanged += (_, _) =>
			{
				var t = hexBox.Text?.TrimStart('#') ?? "";
				if (uint.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
				{
					edit.Int = v;
					Repaint(v);
					err.IsVisible = false;
					hexBox.Classes.Remove("invalid");
					Commit(row, doc);
				}
				else
				{
					err.Text = "not a valid hex colour (e.g. FF8800)";
					err.IsVisible = true;
					hexBox.Classes.Add("invalid");
				}
			};

			_editorHost!.Children.Add(new TextBlock { Text = hasAlpha ? "ARGB (hex)" : "RGB (hex)", Classes = { "label" }, FontSize = 10.5, Margin = new Thickness(0, 0, 0, 3) });
			_editorHost.Children.Add(row1);
			_editorHost.Children.Add(err);
		}

		private void AddTextEditor(MetaRowViewModel row, TagDocumentViewModel doc, bool ascii)
		{
			var edit = row.Current!;
			int maxChars = ascii ? row.Def.Size : row.Def.Size / 2;
			var box = new TextBox { Text = edit.Text ?? "", Width = 240 };
			var err = ErrorText();
			box.TextChanged += (_, _) =>
			{
				var text = box.Text ?? "";
				if (text.Length > maxChars)
				{
					err.Text = $"too long: {text.Length}/{maxChars} characters (fixed-size field)";
					err.IsVisible = true;
					box.Classes.Add("invalid");
					return;
				}
				edit.Text = text;
				err.IsVisible = false;
				box.Classes.Remove("invalid");
				Commit(row, doc);
			};
			AddLabeledRow($"Text (max {maxChars} chars)", box, err);
		}

		private void AddStringIdEditor(MetaRowViewModel row, TagDocumentViewModel doc)
		{
			var edit = row.Current!;
			_editorHost!.Children.Add(new TextBlock { Text = "Current: " + row.DisplayValue, Classes = { "mono" }, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) });

			var search = new TextBox { Watermark = "Search existing string IDs...", Width = 240 };
			var results = new ListBox { Height = 120, Width = 240 };
			_editorHost.Children.Add(search);
			_editorHost.Children.Add(results);

			search.TextChanged += (_, _) =>
			{
				results.ItemsSource = doc.SearchStringIds(search.Text ?? "", 30);
			};
			results.SelectionChanged += (_, _) =>
			{
				if (results.SelectedItem is string s)
				{
					var sid = doc.FindStringId(s);
					if (sid != null)
					{
						edit.Int = sid.Value;
						Commit(row, doc);
					}
				}
			};

			_editorHost.Children.Add(Hint("Picking from existing string IDs writes a real value. Adding brand-new strings isn't supported yet (it needs the cache's string table to grow)."));
		}

		private void AddBlockNavEditor(MetaRowViewModel row, TagDocumentViewModel doc)
		{
			_editorHost!.Children.Add(new TextBlock { Text = row.ElementSummary, Classes = { "mono" }, FontSize = 12 });

			var nav = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 8, 0, 8) };
			var prev = new Button { Content = "< Prev" };
			var next = new Button { Content = "Next >" };
			var indexBox = new TextBox { Text = row.ElementIndex.ToString(), Width = 60 };
			prev.Click += (_, _) => { doc.SetElementIndex(row, row.ElementIndex - 1); RefreshBlockNav(row, doc); };
			next.Click += (_, _) => { doc.SetElementIndex(row, row.ElementIndex + 1); RefreshBlockNav(row, doc); };
			indexBox.KeyDown += (_, ke) =>
			{
				if (ke.Key == Key.Enter && int.TryParse(indexBox.Text, out var i))
				{
					doc.SetElementIndex(row, i);
					RefreshBlockNav(row, doc);
				}
			};
			nav.Children.Add(prev);
			nav.Children.Add(indexBox);
			nav.Children.Add(next);
			_editorHost.Children.Add(nav);

			var expand = new ToggleButton { Content = row.IsExpanded ? "Collapse in table" : "Expand in table", IsChecked = row.IsExpanded };
			expand.Click += (_, _) => { doc.ToggleExpand(row); BuildEditor(row); };
			_editorHost.Children.Add(expand);

			_editorHost.Children.Add(Hint(doc.IsFifthGen
				? "This tag is self-describing: its block/array/struct elements were already parsed from the payload's own 'bdat' chunk when the tag was opened, not resolved through a cache pointer."
				: "Element navigation reads the block's live count/pointer from the cache and resolves the pointer through the cache's own meta-area converter - not a canned list."));
		}

		private void RefreshBlockNav(MetaRowViewModel row, TagDocumentViewModel doc) => BuildEditor(row);

		private void AddReadOnlyNotice(MetaRowViewModel row)
		{
			_editorHost!.Children.Add(new TextBlock
			{
				Text = row.Def.Kind switch
				{
					MetaFieldKind.TagReference => "Tag references are shown resolved, but retargeting them isn't wired up yet.",
					MetaFieldKind.DataReference => "Raw data references (variable-length blobs) aren't editable yet - they need the allocator.",
					MetaFieldKind.RawData or MetaFieldKind.HexString => "Raw/hex byte blobs aren't editable yet.",
					MetaFieldKind.Datum => "Datum indices are identifiers, not editable values.",
					MetaFieldKind.Comment => "This is an informational note, not a field.",
					MetaFieldKind.FifthGenValue =>
						"Campaign Evolved tag fields are read-only in this build: the tag carries its own schema " +
						"instead of a plugin, and Blamite's FifthGenCacheFile.SaveChanges does not support writing them back.",
					_ => $"{row.Def.Kind} fields are read-only in this pass."
				},
				Classes = { "label" },
				TextWrapping = TextWrapping.Wrap
			});
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
						BuildEditor(blockRow);

						var elementIndexStr = Environment.GetEnvironmentVariable("ASM_ELEMENT");
						if (!string.IsNullOrEmpty(elementIndexStr) && int.TryParse(elementIndexStr, out var elIdx))
						{
							Vm.ActiveDocument.SetElementIndex(blockRow, elIdx);
							BuildEditor(blockRow);
						}
					}
				}

				var editField = Environment.GetEnvironmentVariable("ASM_EDIT");
				if (!string.IsNullOrEmpty(editField) && Vm.ActiveDocument != null)
				{
					var target = Vm.ActiveDocument.Rows.FirstOrDefault(r => r.Name.Equals(editField, StringComparison.OrdinalIgnoreCase));
					if (target != null)
					{
						Vm.ActiveDocument.SelectedRow = target;
						BuildEditor(target);
						var fieldList = this.FindControl<ListBox>("FieldList");
						fieldList?.ScrollIntoView(target);

						// Drives the same TextChanged handler a real keystroke would, so a
						// screenshot can show a genuinely dirty, validated edit rather than a
						// static mock of one.
						var editValue = Environment.GetEnvironmentVariable("ASM_EDIT_VALUE");
						if (editValue != null && _editorHost != null)
						{
							var box = _editorHost.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
							if (box != null) box.Text = editValue;
							// TextChanged is dispatched, not synchronous with the property set above;
							// give it a turn of the UI loop before checking dirty state / saving.
							await Task.Delay(50);
						}

						if (Environment.GetEnvironmentVariable("ASM_SAVE") == "1")
						{
							var (ok, message) = Vm.ActiveDocument.Save();
							Console.WriteLine($"ASM_SAVE result: ok={ok} message=\"{message}\"");
							BuildEditor(Vm.ActiveDocument.SelectedRow);
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
