using System;
using System.Collections.Generic;
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
using Avalonia.Styling;
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

		// ---- display density (toolbar picker + View menu radio group) ----
		private ComboBox? _densityBox;
		private bool _syncingDensityUi; // re-entrancy guard: SyncDensityUi below sets DensityBox.SelectedItem itself, which would otherwise fire OnDensityChanged and write the same value back through AppSettings.Instance.Density a second time

		// ---- command palette / shortcuts overlay / global shortcuts (see CommandPalette.axaml.cs
		// and InitializeCommands() below for why this is one registry rather than a hardcoded list
		// scattered across menu/toolbar handlers) ----
		private readonly CommandRegistry _commands = new();
		private CommandPalette? _palette;
		private ShortcutsOverlay? _shortcuts;
		private TextBox? _fieldFilterBox;

		public MainWindow()
		{
			InitializeComponent();

			_bodyGrid = this.FindControl<Grid>("BodyGrid");
			_rootGrid = this.FindControl<Grid>("RootGrid");
			_propertiesPanel = this.FindControl<PropertiesPanel>("PropertiesPanelControl");
			_consoleList = this.FindControl<ListBox>("ConsoleList");
			_consoleFilterBox = this.FindControl<TextBox>("ConsoleFilterBox");
			_consoleLevelBox = this.FindControl<ComboBox>("ConsoleLevelBox");
			_fieldFilterBox = this.FindControl<TextBox>("FieldFilterBox");
			if (_consoleList != null) _consoleList.ItemsSource = _consoleFiltered;

			_palette = this.FindControl<CommandPalette>("Palette");
			_shortcuts = this.FindControl<ShortcutsOverlay>("Shortcuts");
			InitializeCommands();
			_palette?.Configure(_commands,
				tagsVersion: () => Vm?.TagsVersion ?? 0,
				tagEntries: () => Vm == null
					? Array.Empty<TagSearchEntry>()
					: Vm.AllTagsSnapshot.Select(t => new TagSearchEntry(t.Name, t.Group, t.SourceName, t)).ToList(),
				onSelectTag: token => { if (Vm != null && token is TagInfo info) Vm.SelectedTag = new TagNode(info); });

			// Bubble-phase (the default for a plain "+="), deliberately not Tunnel: a handler
			// closer to the focused element - the palette's own QueryBox Escape case, or the field
			// filter's existing OnFieldFilterKeyDown - runs first and can mark the key handled,
			// which is exactly the "closes the palette, clears a filter, cancels an edit, in that
			// priority order" precedence the brief asks for without this handler needing to know
			// which of those elements currently has focus - see OnGlobalKeyDown's own remarks.
			KeyDown += OnGlobalKeyDown;

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
		//
		// Every one of these is a thin trigger that calls into the CommandRegistry rather than
		// the underlying action directly - see InitializeCommands() below. That is what lets a
		// command executed from the toolbar or the native menu still count as "recently used" for
		// the palette's own recency ranking, and it means there is exactly one place (the
		// registry entry's Enabled predicate) that decides whether e.g. Save is currently
		// available, instead of that logic living once in a Button's IsEnabled binding and again
		// in whatever guards the menu/shortcut path.
		private void OnMenuOpenFile(object? sender, EventArgs e) => _commands.Invoke("file.openFile");
		private void OnOpenFileClick(object? sender, RoutedEventArgs e) => _commands.Invoke("file.openFile");

		private void OnMenuOpenFolder(object? sender, EventArgs e) => _commands.Invoke("file.openFolder");
		private void OnOpenFolderClick(object? sender, RoutedEventArgs e) => _commands.Invoke("file.openFolder");

		private void OnMenuOpenZip(object? sender, EventArgs e) => _commands.Invoke("file.openZip");
		private void OnOpenZipClick(object? sender, RoutedEventArgs e) => _commands.Invoke("file.openZip");

		private void OnMenuSave(object? sender, EventArgs e) => _commands.Invoke("file.save");
		private void OnSaveClick(object? sender, RoutedEventArgs e) => _commands.Invoke("file.save");

		private void SaveAndRefresh()
		{
			// Save() reseeds every dirty row's edit state from the freshly-written disk bytes and
			// calls MetaRowViewModel.NotifyEdited() for each one, so the properties sidebar (which
			// subscribes to each row it shows) picks up "unchanged" on its own - nothing further
			// to nudge here.
			Vm?.SaveActive();
		}

		private void OnMenuRevertAll(object? sender, EventArgs e) => _commands.Invoke("file.revertAll");
		private void OnRevertAllClick(object? sender, RoutedEventArgs e) => _commands.Invoke("file.revertAll");

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

		private void OnMenuCloseTab(object? sender, EventArgs e) => _commands.Invoke("file.closeTab");
		private void OnMenuCloseAll(object? sender, EventArgs e) => _commands.Invoke("file.closeAll");

		private void OnMenuFocusSearch(object? sender, EventArgs e) => _commands.Invoke("nav.focusTagFilter");
		private void OnMenuFocusFieldFilter(object? sender, EventArgs e) => _commands.Invoke("nav.focusFieldFilter");

		// ---- display density ----
		//
		// AppSettings.Instance.Density (Services/AppSettings.cs) is where a user's choice is
		// written and persisted; DisplayDensity.Current is what is actually live right now, which
		// is not always the same value - ASM_DENSITY (App.axaml.cs) deliberately calls
		// DisplayDensity.Apply directly, bypassing AppSettings, so a screenshot/QA override does
		// not overwrite a real saved preference. Reading AppSettings.Instance.Density here instead
		// of DisplayDensity.Current was tried first and was wrong for exactly that case: the
		// picker kept showing "Default" under ASM_DENSITY=Comfortable, because the persisted
		// setting genuinely hadn't changed even though the live density had - caught by actually
		// looking at this pass's own screenshots, not by inspection. Both the toolbar ComboBox and
		// the View menu's radio group only ever read DisplayDensity.Current (via SyncDensityUi,
		// driven by DisplayDensity.Changed so either surface picks up a change made through the
		// other - or, eventually, through a command-palette entry) and write to
		// AppSettings.Instance.Density (OnDensityChanged / OnDensityMenuClick below) - neither
		// owns UI state of its own.
		private void OnDensityBoxLoaded(object? sender, RoutedEventArgs e)
		{
			_densityBox = sender as ComboBox;
			DisplayDensity.Changed -= OnDisplayDensityChanged; // guard against double subscription if this control is ever reloaded
			DisplayDensity.Changed += OnDisplayDensityChanged;
			SyncDensityUi(DisplayDensity.Current);
		}

		private void OnDisplayDensityChanged(object? sender, EventArgs e) => SyncDensityUi(DisplayDensity.Current);

		private void OnDensityChanged(object? sender, SelectionChangedEventArgs e)
		{
			if (_syncingDensityUi) return;
			if ((sender as ComboBox)?.SelectedItem is ComboBoxItem { Tag: string tag } &&
			    Enum.TryParse<DensityLevel>(tag, out var level))
				AppSettings.Instance.Density = level;
		}

		/// <summary>
		///     Shared handler for all three "Density: ..." <c>NativeMenuItem</c>s (see
		///     MainWindow.axaml's View menu for why they cannot be looked up by name instead - a
		///     macOS-native menu item has no NameScope registration in this Avalonia build).
		///     Reads which level was clicked off the item's own <c>Header</c> text.
		/// </summary>
		private void OnDensityMenuClick(object? sender, EventArgs e)
		{
			if (sender is not NativeMenuItem item) return;
			var level = item.Header switch
			{
				"Density: Compact" => DensityLevel.Compact,
				"Density: Comfortable" => DensityLevel.Comfortable,
				_ => DensityLevel.Default
			};
			AppSettings.Instance.Density = level;
		}

		/// <summary>
		///     Pushes <paramref name="level" /> onto the toolbar ComboBox's selection and the View
		///     menu's three radio items' <c>IsChecked</c>, without writing back to
		///     <see cref="AppSettings" /> (that direction is already how <paramref name="level" />
		///     got here). The View menu items are found by walking <c>NativeMenu.GetMenu(this)</c>
		///     rather than a name lookup, for the same reason <see cref="OnDensityMenuClick" />
		///     reads <c>Header</c> off <c>sender</c> instead of a captured reference.
		/// </summary>
		private void SyncDensityUi(DensityLevel level)
		{
			_syncingDensityUi = true;
			try
			{
				if (_densityBox != null)
				{
					foreach (var entry in _densityBox.Items)
						if (entry is ComboBoxItem { Tag: string tag } item && string.Equals(tag, level.ToString(), StringComparison.Ordinal))
						{
							_densityBox.SelectedItem = item;
							break;
						}
				}

				var viewMenu = NativeMenu.GetMenu(this)?.Items.OfType<NativeMenuItem>()
					.FirstOrDefault(i => i.Header == "View")?.Menu;
				if (viewMenu != null)
				{
					foreach (var item in viewMenu.Items.OfType<NativeMenuItem>())
					{
						DensityLevel? itemLevel = item.Header switch
						{
							"Density: Compact" => DensityLevel.Compact,
							"Density: Default" => DensityLevel.Default,
							"Density: Comfortable" => DensityLevel.Comfortable,
							_ => null
						};
						if (itemLevel.HasValue) item.IsChecked = itemLevel.Value == level;
					}
				}
			}
			finally
			{
				_syncingDensityUi = false;
			}
		}


		private void OnMenuToggleTagTree(object? sender, EventArgs e) => _commands.Invoke("view.toggleTagTree");
		private void OnMenuToggleValueSidebar(object? sender, EventArgs e) => _commands.Invoke("view.toggleProperties");
		private void OnMenuToggleConsole(object? sender, EventArgs e) => _commands.Invoke("view.toggleConsole");
		private void OnMenuSwitchTheme(object? sender, EventArgs e) => _commands.Invoke("view.switchTheme");

		private void OnMenuGoBack(object? sender, EventArgs e) => _commands.Invoke("nav.back");
		private void OnMenuGoForward(object? sender, EventArgs e) => _commands.Invoke("nav.forward");
		private void OnMenuCommandPalette(object? sender, EventArgs e) => _commands.Invoke("nav.commandPalette");
		private void OnMenuGoToTag(object? sender, EventArgs e) => _commands.Invoke("nav.goToTag");
		private void OnMenuShowShortcuts(object? sender, EventArgs e) => _commands.Invoke("help.shortcuts");

		private void OnConsoleClearClick(object? sender, RoutedEventArgs e) => Vm?.Log.Clear();

		/// <summary>
		///     Every command this shell exposes - id, title, category, action, and (where one
		///     exists) an <see cref="Func{Boolean}" /> availability check and a bound
		///     <see cref="KeyGesture" />. Registered once, here, because this is the one place
		///     that already has direct access to everything a command could touch (the view model,
		///     every toolbar/menu action above, the palette and shortcuts overlay instances). Every
		///     trigger path - toolbar, native menu, the global key handler, and the palette itself
		///     - goes through <see cref="CommandRegistry.Invoke" />/<see cref="CommandRegistry.TryInvokeForGesture" />
		///     rather than duplicating any of this, so a command's behaviour, its shortcut and its
		///     palette entry can never independently drift the way three hand-maintained lists
		///     could. Declaration order doubles as the palette's empty-query browse order for
		///     commands nothing has made "recently used" yet, hence the grouping by category below.
		/// </summary>
		private void InitializeCommands()
		{
			_commands.Register(new CommandDefinition("file.openFile", "Open Cache File...", "File",
				() => _ = OpenFileViaPickerAsync(),
				gesture: PlatformKeys.Gesture(Key.O), shortcutLabel: PlatformKeys.Label(Key.O)));

			_commands.Register(new CommandDefinition("file.openFolder", "Open Folder...", "File",
				() => _ = OpenFolderViaPickerAsync(),
				gesture: PlatformKeys.Gesture(Key.O, KeyModifiers.Shift),
				shortcutLabel: PlatformKeys.Label(Key.O, KeyModifiers.Shift)));

			_commands.Register(new CommandDefinition("file.openZip", "Open Zip...", "File",
				() => _ = OpenZipViaPickerAsync()));

			_commands.Register(new CommandDefinition("file.save", "Save", "File",
				SaveAndRefresh,
				enabled: () => Vm?.ActiveDocument?.IsDirty == true,
				gesture: PlatformKeys.Gesture(Key.S), shortcutLabel: PlatformKeys.Label(Key.S)));

			_commands.Register(new CommandDefinition("file.revertAll", "Revert All Changes", "File",
				() => Vm?.ActiveDocument?.RevertAll(),
				enabled: () => Vm?.ActiveDocument?.IsDirty == true));

			_commands.Register(new CommandDefinition("file.closeTab", "Close Tab", "File",
				() => { if (Vm?.ActiveDocument != null) Vm.CloseDocument(Vm.ActiveDocument); },
				enabled: () => Vm?.ActiveDocument != null,
				gesture: PlatformKeys.Gesture(Key.W), shortcutLabel: PlatformKeys.Label(Key.W)));

			_commands.Register(new CommandDefinition("file.closeAll", "Close All Tabs", "File",
				() => Vm?.CloseAll(),
				enabled: () => Vm?.HasDocuments == true));

			_commands.Register(new CommandDefinition("view.toggleTagTree", "Toggle Tag Tree", "View",
				() => { if (Vm != null) Vm.ShowTagTree = !Vm.ShowTagTree; },
				gesture: PlatformKeys.Gesture(Key.OemBackslash), shortcutLabel: PlatformKeys.Label(Key.OemBackslash)));

			_commands.Register(new CommandDefinition("view.toggleProperties", "Toggle Properties Sidebar", "View",
				() => { if (Vm != null) Vm.ShowValueSidebar = !Vm.ShowValueSidebar; },
				gesture: PlatformKeys.Gesture(Key.OemBackslash, KeyModifiers.Shift),
				shortcutLabel: PlatformKeys.Label(Key.OemBackslash, KeyModifiers.Shift)));

			_commands.Register(new CommandDefinition("view.toggleConsole", "Toggle Console", "View",
				() => { if (Vm != null) Vm.ShowConsole = !Vm.ShowConsole; },
				gesture: PlatformKeys.Gesture(Key.OemBackslash, KeyModifiers.Alt),
				shortcutLabel: PlatformKeys.Label(Key.OemBackslash, KeyModifiers.Alt)));

			_commands.Register(new CommandDefinition("view.switchTheme", "Switch Theme (Light / Dark)", "View", SwitchTheme));

			// TODO(ui-density): the density pass (a separate agent this wave, per the brief's
			// file-ownership table) is exposing a public API for switching display density from
			// Theme/App.axaml - neither of which this pass may touch. Wire Execute/Enabled to
			// that API once it lands. Until then this is a real, visible-but-disabled palette
			// entry rather than a silently missing one, per the brief: "disabled commands should
			// be visible but clearly unavailable rather than absent".
			_commands.Register(new CommandDefinition("view.switchDensity", "Switch Display Density", "View",
				() => { }, enabled: () => false));

			_commands.Register(new CommandDefinition("nav.commandPalette", "Command Palette...", "Navigate",
				() => _palette?.Open(commandMode: true),
				gesture: PlatformKeys.Gesture(Key.K), shortcutLabel: PlatformKeys.Label(Key.K)));

			_commands.Register(new CommandDefinition("nav.goToTag", "Go to Tag...", "Navigate",
				() => _palette?.Open(commandMode: false),
				enabled: () => Vm?.HasAnySource == true,
				gesture: PlatformKeys.Gesture(Key.P), shortcutLabel: PlatformKeys.Label(Key.P)));

			_commands.Register(new CommandDefinition("nav.focusTagFilter", "Focus Tag Filter", "Navigate",
				() => this.FindControl<TextBox>("SearchBox")?.Focus(),
				gesture: PlatformKeys.Gesture(Key.F), shortcutLabel: PlatformKeys.Label(Key.F)));

			_commands.Register(new CommandDefinition("nav.focusFieldFilter", "Focus Field Filter", "Navigate",
				() => _fieldFilterBox?.Focus(),
				enabled: () => Vm?.ActiveDocument != null,
				gesture: PlatformKeys.Gesture(Key.F, KeyModifiers.Shift),
				shortcutLabel: PlatformKeys.Label(Key.F, KeyModifiers.Shift)));

			_commands.Register(new CommandDefinition("nav.back", "Go Back", "Navigate",
				() => Vm?.GoBack(),
				enabled: () => Vm?.CanGoBack == true,
				gesture: PlatformKeys.Gesture(Key.OemOpenBrackets), shortcutLabel: PlatformKeys.Label(Key.OemOpenBrackets)));

			_commands.Register(new CommandDefinition("nav.forward", "Go Forward", "Navigate",
				() => Vm?.GoForward(),
				enabled: () => Vm?.CanGoForward == true,
				gesture: PlatformKeys.Gesture(Key.OemCloseBrackets), shortcutLabel: PlatformKeys.Label(Key.OemCloseBrackets)));

			// One command per tab slot rather than a single hand-rolled digit switch in the global
			// key handler - TryInvokeForGesture already knows how to match a KeyGesture, so this
			// reuses that instead of adding a second, parallel way to bind a shortcut.
			for (int i = 1; i <= 9; i++)
			{
				int index = i - 1;
				var digitKey = DigitKey(i);
				_commands.Register(new CommandDefinition($"nav.selectTab{i}", $"Select Tab {i}", "Navigate",
					() => { if (Vm != null && index < Vm.Documents.Count) Vm.ActiveDocument = Vm.Documents[index]; },
					enabled: () => Vm != null && index < Vm.Documents.Count,
					gesture: PlatformKeys.Gesture(digitKey), shortcutLabel: PlatformKeys.Label(digitKey)));
			}

			_commands.Register(new CommandDefinition("help.shortcuts", "Keyboard Shortcuts", "Help",
				() => _shortcuts?.Open(_commands),
				gesture: PlatformKeys.Gesture(Key.OemQuestion), shortcutLabel: PlatformKeys.Label(Key.OemQuestion)));
		}

		private static Key DigitKey(int oneBased) => oneBased switch
		{
			1 => Key.D1, 2 => Key.D2, 3 => Key.D3, 4 => Key.D4, 5 => Key.D5,
			6 => Key.D6, 7 => Key.D7, 8 => Key.D8, 9 => Key.D9,
			_ => throw new ArgumentOutOfRangeException(nameof(oneBased))
		};

		private void SwitchTheme()
		{
			var app = Application.Current;
			if (app == null) return;
			// ActualThemeVariant (not RequestedThemeVariant) is read here so the first press
			// always visibly flips something even when nothing has ever pinned a variant yet -
			// App.axaml's own RequestedThemeVariant="Default" means "follow the OS" (see its
			// remarks), so RequestedThemeVariant itself can be "Default" indefinitely while
			// ActualThemeVariant is always resolved to a real Light/Dark.
			app.RequestedThemeVariant = app.ActualThemeVariant == ThemeVariant.Dark
				? ThemeVariant.Light
				: ThemeVariant.Dark;
		}

		/// <summary>
		///     Recognises every shortcut this shell defines regardless of which control currently
		///     has focus - the brief calls this out as a real, existing gap: Cmd+[ / Cmd+] back/
		///     forward used to only fire from inside <see cref="OnFieldListKeyDown" /> (the field
		///     table's own local handler), so it silently did nothing whenever focus was anywhere
		///     else - the tag tree, a filter box, the properties sidebar. Subscribed as a plain
		///     bubble-phase handler (see the constructor), so anything closer to the focused
		///     element that already marked the key handled - the palette's own Escape case in
		///     <c>CommandPalette.OnQueryBoxKeyDown</c>, <c>PropertiesPanel</c>'s Cmd+Z/Cmd+Shift+Z -
		///     is not overridden here; that is what makes "closes the palette, clears a filter,
		///     cancels an edit, in that priority order" fall out of ordinary event routing instead
		///     of this method needing to know where focus currently is.
		/// </summary>
		private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
		{
			if (e.Handled) return;

			if (e.Key == Key.Escape)
			{
				// Reached only when nothing closer to the focused element already handled Escape -
				// in particular this means the palette/shortcuts overlay, if open, get one more
				// chance here in case focus hasn't landed inside them yet (Open() posts its
				// Focus() call to the dispatcher queue rather than focusing synchronously - see
				// CommandPalette.Open's own remarks), so the overlay still closes on Escape even
				// during that brief window instead of the key falling through to "clear a filter".
				if (_palette?.IsOpen == true) { _palette.Close(); e.Handled = true; return; }
				if (_shortcuts?.IsOpen == true) { _shortcuts.Close(); e.Handled = true; return; }

				if (Vm?.ActiveDocument?.IsFiltering == true) { Vm.ActiveDocument.FilterQuery = ""; e.Handled = true; return; }
				if (!string.IsNullOrEmpty(Vm?.Search)) { Vm!.Search = ""; e.Handled = true; return; }

				// Nothing to close or clear - "cancel an in-flight edit" as the last resort: move
				// focus off whatever TextBox currently has it (a field editor's input - Views/
				// Editors/* is not this pass's to change, so backing out of it generically by
				// dropping focus is the one thing a global handler can safely do without knowing
				// that editor's own commit semantics) rather than leaving Escape with nothing left
				// to do while text is being edited.
				if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is TextBox) { this.Focus(); e.Handled = true; }
				return;
			}

			// While an overlay owns input, every other shortcut is suppressed rather than also
			// firing underneath it - Cmd+S saving silently while the user is mid-search in the
			// palette would be a surprise, not a convenience.
			if (_palette?.IsOpen == true || _shortcuts?.IsOpen == true) return;

			if (_commands.TryInvokeForGesture(e)) e.Handled = true;
		}

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
		// Cmd+[ / Cmd+] back/forward used to be handled only here (Meta+bracket, checked before
		// falling into the row-selection switch below) - that was the exact "wired only in the
		// field table's handler where it should be global" gap the brief calls out, since it did
		// nothing when focus was in the tag tree or a filter box. It is now a registered command
		// (InitializeCommands' "nav.back"/"nav.forward") matched by the window-level
		// OnGlobalKeyDown for every focus location, so there is nothing bracket-specific left here. ----
		private void OnFieldListKeyDown(object? sender, KeyEventArgs e)
		{
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

		/// <summary>
		///     Walks <see cref="MainViewModel.Nodes" /> (a mix of <c>GroupNode</c>/<c>FolderNode</c>/
		///     <c>TagNode</c> - see MainViewModel.cs and FolderNode.cs) looking for the <c>TagNode</c>
		///     that wraps <paramref name="info" />, for <see cref="CaptureAsync" />'s ASM_SELECT
		///     handling - see the call site's own remarks for why this is needed at all rather than
		///     just using whatever <c>MainViewModel.FindTag</c> already returned.
		/// </summary>
		private static TagNode? FindMatchingTagNode(System.Collections.IEnumerable nodes, TagInfo info)
		{
			foreach (var node in nodes)
			{
				switch (node)
				{
					case TagNode t when ReferenceEquals(t.Info, info):
						return t;
					case GroupNode g:
						var fromGroup = FindMatchingTagNode(g.Tags, info);
						if (fromGroup != null) return fromGroup;
						break;
					case FolderNode f:
						var fromFolder = FindMatchingTagNode(f.Children, info);
						if (fromFolder != null) return fromFolder;
						break;
				}
			}
			return null;
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

				// ASM_TAGSEARCH=<query> - sets the tag-tree filter directly (Vm.Search is the
				// same TwoWay-bound property SearchBox's Text drives), for screenshotting the
				// filtered tree or exercising OnGlobalKeyDown's Escape-clears-a-filter branch
				// without needing a real TextInput event (KeyDown alone, as ASM_KEYS above raises,
				// does not type characters into a TextBox - only a genuine text-input event does).
				var tagSearch = Environment.GetEnvironmentVariable("ASM_TAGSEARCH");
				if (!string.IsNullOrEmpty(tagSearch)) Vm.Search = tagSearch;

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
						if (tag != null)
						{
							Vm.SelectedTag = tag;
							// Also mark the tree's own SelectedItem, purely for screenshotting the
							// :selected row styling (MetroStyles.axaml's TreeViewItem
							// ThemeDictionaries) - Vm.SelectedTag alone never reaches the TreeView's
							// own selection state (one-way view -> view model only, see this
							// method's remarks further down), so without this every ASM_SHOT of a
							// populated tree showed no row selected at all. FindTag hands back a
							// freshly-constructed TagNode (MainViewModel.FindTag - ViewModels/, not
							// this pass's to change), a different reference than whatever TagNode
							// instance actually lives inside Vm.Nodes, so setting SelectedItem to
							// that instance directly matches nothing; FindMatchingTagNode below
							// walks the real tree data to find the one Avalonia's selection model
							// will actually recognise.
							var realNode = FindMatchingTagNode(Vm.Nodes, tag.Info);
							if (realNode != null) tree.SelectedItem = realNode;
						}
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

				// ASM_FOCUS=<control name> - moves real keyboard focus to a named control (e.g.
				// "TagTree" or "FieldList"), for screenshotting the :focus-visible styling
				// (MainWindow.axaml's Window.Styles) on a control this harness has no shortcut
				// that already lands on - Tab-cycling there itself is real OS-level focus-
				// navigation Avalonia intercepts ahead of ordinary routed KeyDown handlers, which
				// ASM_KEYS's synthetic RaiseEvent calls do not faithfully reproduce. Focusing a
				// ListBox/TreeView itself is not the same as a real Tab landing inside it - a real
				// Tab (or an arrow-key press once inside) moves focus to the *selected item's own
				// container*, which is what :focus-visible on ListBoxItem/TreeViewItem actually
				// keys off; ContainerFromItem finds that container directly.
				var focusTarget = Environment.GetEnvironmentVariable("ASM_FOCUS");
				if (!string.IsNullOrEmpty(focusTarget))
				{
					var control = this.FindControl<Control>(focusTarget);
					control?.Focus();
					UpdateLayout();

					InputElement? itemContainer = control switch
					{
						ListBox { SelectedItem: { } sel } lb => lb.ContainerFromItem(sel) as InputElement,
						TreeView { SelectedItem: { } sel } tv => tv.ContainerFromItem(sel) as InputElement,
						// No selection to key off (e.g. the tag tree - SelectedTag is one-way,
						// view -> view model only, so ASM_SELECT never populates TreeView's own
						// SelectedItem) - fall back to whatever container is already realized and
						// on screen, same source CaptureAsync's own ASM_SELECT handling already
						// reads from a few lines up.
						ListBox lb => lb.GetRealizedContainers().OfType<InputElement>().FirstOrDefault(),
						TreeView tv => tv.GetRealizedContainers().OfType<InputElement>().FirstOrDefault(),
						_ => null
					};
					// NavigationMethod.Tab, not the parameterless Focus() overload's default
					// (Unspecified) - :focus-visible specifically distinguishes keyboard-driven
					// focus from a plain programmatic/pointer one, so proving the style fires at
					// all means asking for focus the same way Avalonia's own Tab handling does.
					itemContainer?.Focus(NavigationMethod.Tab);

					await Task.Delay(50);
				}

				// ASM_KEYS="Cmd+K,Escape,..." - raises real, routed KeyDown events (RaiseEvent, not
				// a direct method call) sourced from whatever control currently has keyboard focus
				// - not this Window - so the event genuinely bubbles up through it (a palette
				// QueryBox's own OnQueryBoxKeyDown, PropertiesPanel's Cmd+Z override, ...) exactly
				// the way a real physical keypress would, before OnGlobalKeyDown ever sees it. That
				// is what makes this a faithful test of the Escape priority chain specifically -
				// sourcing the event from the Window itself would skip every nearer handler and
				// always exercise the same fallback branch regardless of what is actually focused.
				// One token per comma-separated KeyGesture.Parse-compatible entry; each is logged
				// with whether it ended up Handled, so a run's console output is itself the
				// evidence a shortcut was actually exercised, not just wired up. Digits need the
				// "D1".."D9" form (KeyGesture.Parse("Cmd+1") does not parse to Key.D1 - verified
				// directly against Avalonia 12.1.1; "Cmd+D1" does).
				var keysFlag = Environment.GetEnvironmentVariable("ASM_KEYS");
				if (!string.IsNullOrEmpty(keysFlag))
				{
					// ASM_SELECT above opens each name as a fire-and-forget OpenTagAsync (see
					// MainViewModel.SelectedTag's own remarks on why it has to be) - several such
					// calls fired back-to-back all set IsOpeningTag=true at entry, before any of
					// them actually wait on OpenTagAsync's own serializing gate, so an earlier call
					// finishing and clearing IsOpeningTag=false in its `finally` can make that flag
					// read "false" even while a later call is still queued on the gate behind it.
					// Waiting on the flag alone is therefore not reliable for "every ASM_SELECT
					// name has finished opening"; polling Documents.Count against how many names
					// were actually found is - a Campaign Evolved tag's parse can run ~400ms
					// (b30-scenario, per this project's own measurements), and without waiting for
					// all of them a shortcut fired too early could race a still-in-flight open for
					// a *different* tab, which then completes afterwards and reassigns
					// ActiveDocument out from under it - a harness timing artifact, not a real user
					// interleaving (nobody presses Cmd+1 mid-mount), so it is the harness's job to
					// wait it out rather than the product's.
					int expectedDocs = string.IsNullOrEmpty(select)
						? 0
						: select.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
					var deadline = DateTime.UtcNow.AddSeconds(5);
					while (Vm != null && Vm.Documents.Count < expectedDocs && DateTime.UtcNow < deadline) await Task.Delay(20);
					while (Vm?.IsOpeningTag == true && DateTime.UtcNow < deadline) await Task.Delay(20);

					foreach (var token in keysFlag.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
					{
						var gesture = KeyGesture.Parse(token);
						var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
						var args = new KeyEventArgs
						{
							RoutedEvent = InputElement.KeyDownEvent,
							Route = RoutingStrategies.Bubble,
							Key = gesture.Key,
							KeyModifiers = gesture.KeyModifiers,
							Source = focused ?? this
						};
						(focused as Control ?? this).RaiseEvent(args);
						Console.WriteLine($"ASM_KEYS: \"{token}\" (source={(focused as Control)?.Name ?? focused?.GetType().Name ?? "Window"}) -> Key={gesture.Key} Mods={gesture.KeyModifiers} handled={args.Handled}");
						await Task.Delay(150);
					}
				}

				// ASM_PALETTE=commands|tags[:<query>] - opens the command palette for a screenshot,
				// following the same "set up state, then let the final delay+render below capture
				// it" shape every other ASM_* flag above already uses. The query (if any) is typed
				// into QueryBox the same way ASM_EDIT_VALUE types into a field editor further up -
				// by reaching into the realized visual tree, not a testing-only public setter, to
				// exercise the exact TextChanged path a real keystroke drives.
				var paletteFlag = Environment.GetEnvironmentVariable("ASM_PALETTE");
				if (!string.IsNullOrEmpty(paletteFlag))
				{
					bool commandMode = paletteFlag.StartsWith("commands", StringComparison.OrdinalIgnoreCase);
					string? query = paletteFlag.Contains(':') ? paletteFlag[(paletteFlag.IndexOf(':') + 1)..] : null;
					_palette?.Open(commandMode);
					await Task.Delay(50);
					if (query != null && _palette != null)
					{
						var queryBox = _palette.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(t => t.Name == "QueryBox");
						if (queryBox != null)
						{
							queryBox.Text = (commandMode ? ">" : "") + query;
							queryBox.CaretIndex = queryBox.Text.Length;
						}
					}
					await Task.Delay(150);
				}

				if (Environment.GetEnvironmentVariable("ASM_SHORTCUTS") == "1")
				{
					_shortcuts?.Open(_commands);
					await Task.Delay(100);
				}
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
				rtb.Save(path, new global::Avalonia.Media.Imaging.PngBitmapEncoderOptions());
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
				rtb.Save(path, new global::Avalonia.Media.Imaging.PngBitmapEncoderOptions());
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
