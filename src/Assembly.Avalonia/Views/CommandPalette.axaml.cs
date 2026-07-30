using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Assembly.Avalonia.Services;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Assembly.Avalonia.Views
{
	/// <summary>One row of <see cref="CommandPalette" />'s result list - a command or a tag,
	/// rendered identically enough (chip, primary/secondary text, trailing shortcut) that one
	/// DataTemplate covers both. Presentation-ready (opacity, chip text) is computed once here
	/// rather than via converters, since neither <c>CommandDefinition</c> nor <c>TagSearchEntry</c>
	/// is itself a view model and this is the one place that turns either into a row.</summary>
	public sealed record PaletteResultItem
	{
		public required string PrimaryText { get; init; }
		public string SecondaryText { get; init; } = "";
		public bool HasSecondaryText => SecondaryText.Length > 0;
		public string ChipText { get; init; } = "";
		public bool HasChip => ChipText.Length > 0;
		public string? ShortcutLabel { get; init; }
		public bool HasShortcut => !string.IsNullOrEmpty(ShortcutLabel);
		public double RowOpacity { get; init; } = 1.0;
		public bool CanActivate { get; init; } = true;
		public required Action Activate { get; init; }

		public static PaletteResultItem ForCommand(CommandDefinition cmd, Action activate) => new()
		{
			PrimaryText = cmd.Title,
			ChipText = cmd.Category,
			ShortcutLabel = cmd.ShortcutLabel,
			RowOpacity = cmd.Enabled() ? 1.0 : 0.45,
			CanActivate = cmd.Enabled(),
			Activate = activate
		};

		public static PaletteResultItem ForTag(TagSearchEntry entry, Action activate) => new()
		{
			PrimaryText = entry.Name,
			SecondaryText = entry.SourceName,
			ChipText = entry.Group,
			Activate = activate
		};
	}

	/// <summary>
	///     The Cmd+K / Cmd+P overlay: one search box with two modes rather than either a single
	///     ranked list over everything or two separate widgets.
	///     <para>
	///         Commands and tags are never scored against each other. A retail mount carries
	///         ~12,000 tags against on the order of twenty commands - in one merged ranked list
	///         that imbalance means either every command needs an artificial score boost just to
	///         stay visible (fragile, and the boost itself becomes a second thing to tune whenever
	///         a tag name happens to prefix-match "save" or "open"), or a lucky tag match genuinely
	///         buries "Save" on page two - exactly the "palette that returns the right answer
	///         third" failure the brief warns against. VS Code hit the identical problem (files vs
	///         commands) and settled on the same fix: <b>plain text searches tags</b> (this is
	///         "quick open" - <see cref="Open" />(commandMode: false), bound to Cmd+P) and
	///         <b>a leading "&gt;" switches to commands</b> (<see cref="Open" />(commandMode: true),
	///         bound to Cmd+K, pre-fills the "&gt;" so a Cmd+K user who wants an action never has
	///         to type the prefix themselves - only someone who opened via Cmd+K and then wants a
	///         tag instead needs to backspace it out). One control, one shortcut family, one place
	///         to learn - just two unambiguous modes instead of one ambiguous ranking.
	///     </para>
	///     <para>
	///         Tag search never turns the mounted namespace into 12,000 view models: <see cref="TagSearchIndex" />
	///         holds lightweight structs rebuilt only when <c>MainViewModel.TagsVersion</c> changes
	///         (a mount/unmount), and <see cref="Refresh" /> below only ever materializes
	///         <see cref="PaletteResultItem" />s for the handful of rows actually shown.
	///     </para>
	/// </summary>
	public partial class CommandPalette : UserControl
	{
		private const int MaxResults = 40;

		private readonly ObservableCollection<PaletteResultItem> _results = new();
		private readonly TagSearchIndex _tagIndex = new();
		private readonly Dictionary<string, TagSearchEntry> _tagsByKey = new();
		private readonly RecencyTracker<string> _recentTags = new(capacity: 10);

		private readonly TextBox _queryBox;
		private readonly ListBox _resultsList;
		private readonly TextBlock _modeChipText;
		private readonly TextBlock _footerHint;
		private readonly TextBlock _emptyHint;
		private readonly Border _scrim;

		private CommandRegistry? _commands;
		private Func<int>? _tagsVersionProvider;
		private Func<IReadOnlyList<TagSearchEntry>>? _tagEntriesProvider;
		private Action<object?>? _onSelectTag;
		private int _lastIndexedTagsVersion = -1;
		private IInputElement? _restoreFocusTo;

		public bool IsOpen { get; private set; }

		public CommandPalette()
		{
			InitializeComponent();

			_queryBox = this.FindControl<TextBox>("QueryBox")!;
			_resultsList = this.FindControl<ListBox>("ResultsList")!;
			_modeChipText = this.FindControl<TextBlock>("ModeChipText")!;
			_footerHint = this.FindControl<TextBlock>("FooterHint")!;
			_emptyHint = this.FindControl<TextBlock>("EmptyHint")!;
			_scrim = this.FindControl<Border>("PaletteBox")!;

			_resultsList.ItemsSource = _results;
			_queryBox.TextChanged += (_, _) => Refresh();
			_queryBox.KeyDown += OnQueryBoxKeyDown;
			_resultsList.DoubleTapped += (_, _) => ActivateSelected();
		}

		private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

		/// <summary>
		///     Wires this control up to the app it's overlaying. <paramref name="tagsVersion" /> /
		///     <paramref name="tagEntries" /> are lazy (called only when the palette opens or a
		///     mount actually changed) rather than a live subscription, since the palette is closed
		///     far more often than the namespace changes - see <see cref="EnsureTagIndexFresh" />.
		/// </summary>
		public void Configure(CommandRegistry commands, Func<int> tagsVersion,
			Func<IReadOnlyList<TagSearchEntry>> tagEntries, Action<object?> onSelectTag)
		{
			_commands = commands;
			_tagsVersionProvider = tagsVersion;
			_tagEntriesProvider = tagEntries;
			_onSelectTag = onSelectTag;
		}

		/// <summary>Opens in command mode (query pre-filled with "&gt;", Cmd+K) or tag mode (empty
		/// query, Cmd+P) - see this class's own header comment for why these are the same control.</summary>
		public void Open(bool commandMode)
		{
			if (IsOpen) { _queryBox.Focus(); return; }

			EnsureTagIndexFresh();
			_restoreFocusTo = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();

			IsVisible = true;
			IsHitTestVisible = true;
			IsOpen = true;

			_queryBox.Text = commandMode ? ">" : "";
			_queryBox.CaretIndex = _queryBox.Text.Length;
			Refresh();

			// Focus() right after IsVisible=true can lose to layout not having run yet - the
			// TextBox isn't a valid focus target until it has a realized visual. Posting to the
			// dispatcher queue runs after this layout pass, same trick MainWindow's own screenshot
			// harness already relies on elsewhere for "wait a UI tick".
			Dispatcher.UIThread.Post(() => _queryBox.Focus());
		}

		public void Close()
		{
			if (!IsOpen) return;
			IsVisible = false;
			IsHitTestVisible = false;
			IsOpen = false;
			_restoreFocusTo?.Focus();
			_restoreFocusTo = null;
		}

		private void EnsureTagIndexFresh()
		{
			if (_tagsVersionProvider == null || _tagEntriesProvider == null) return;
			int version = _tagsVersionProvider();
			if (version == _lastIndexedTagsVersion) return;

			var entries = _tagEntriesProvider();
			_tagIndex.Rebuild(entries);

			_tagsByKey.Clear();
			foreach (var entry in entries)
				_tagsByKey[TagKey(entry)] = entry;

			_lastIndexedTagsVersion = version;
		}

		private static string TagKey(TagSearchEntry e) => $"{e.SourceName}::{e.Group}::{e.Name}";

		private void OnScrimPointerPressed(object? sender, PointerPressedEventArgs e)
		{
			// The palette box has its own handler below that marks the event handled before it
			// bubbles here, so reaching this handler at all means the click landed on the scrim
			// itself (outside the box) - "click outside to dismiss".
			Close();
		}

		private void OnPaletteBoxPointerPressed(object? sender, PointerPressedEventArgs e) => e.Handled = true;

		private void OnQueryBoxKeyDown(object? sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Down:
					MoveSelection(1);
					e.Handled = true;
					break;
				case Key.Up:
					MoveSelection(-1);
					e.Handled = true;
					break;
				case Key.Tab:
					// Trapped here rather than left to default focus traversal, so Tab cannot walk
					// focus out of the palette into the dimmed window behind the scrim - it steps
					// the result selection instead, same as Down/Shift+Down.
					MoveSelection(e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : 1);
					e.Handled = true;
					break;
				case Key.Enter:
					ActivateSelected();
					e.Handled = true;
					break;
				case Key.Escape:
					// Also handled by MainWindow's window-level Escape chain, but that only runs
					// once nothing closer has already claimed the key - handling it here directly
					// is what makes "palette open -> Escape closes it" take priority over "clear a
					// filter"/"cancel an edit" without MainWindow needing to know this control's
					// internals at all (see MainWindow.OnGlobalKeyDown's own remarks).
					Close();
					e.Handled = true;
					break;
			}
		}

		private void MoveSelection(int delta)
		{
			if (_results.Count == 0) return;
			int next = _resultsList.SelectedIndex < 0 ? 0 : _resultsList.SelectedIndex + delta;
			next = Math.Clamp(next, 0, _results.Count - 1);
			_resultsList.SelectedIndex = next;
			_resultsList.ScrollIntoView(next);
		}

		private void ActivateSelected()
		{
			if (_resultsList.SelectedItem is PaletteResultItem { CanActivate: true } item)
				item.Activate();
		}

		/// <summary>Rebuilds the result list from the current query. Runs on every keystroke, so
		/// everything here is bounded by <see cref="MaxResults" /> - see the type header for why
		/// this never costs "12,000 of anything" per call.</summary>
		private void Refresh()
		{
			string text = _queryBox.Text ?? "";
			bool commandMode = text.StartsWith(">", StringComparison.Ordinal);
			string query = commandMode ? text[1..].TrimStart() : text;

			_modeChipText.Text = commandMode ? "COMMANDS" : "TAGS";
			_results.Clear();

			if (commandMode) RefreshCommands(query);
			else RefreshTags(query);

			bool empty = _results.Count == 0;
			_resultsList.IsVisible = !empty;
			_emptyHint.IsVisible = empty;
			if (empty)
			{
				_emptyHint.Text = commandMode
					? $"No command matches \"{query}\"."
					: query.Trim().Length == 0
						? (_tagIndex.Count == 0
							? "Nothing mounted yet - open a cache file, folder or zip first."
							: $"{_tagIndex.Count:N0} tags indexed - type a name to search, or start with > for commands.")
						: $"No tag matches \"{query}\" in {_tagIndex.Count:N0} mounted tags.";
			}
			else
			{
				_resultsList.SelectedIndex = 0;
			}
		}

		private void RefreshCommands(string query)
		{
			if (_commands == null) return;
			foreach (var cmd in _commands.Search(query, MaxResults))
			{
				var item = PaletteResultItem.ForCommand(cmd, () =>
				{
					Close();
					_commands.Invoke(cmd.Id);
				});
				_results.Add(item);
			}

			_footerHint.Text = "↑↓ navigate    ↵ run    esc close    backspace → tag search";
		}

		private void RefreshTags(string query)
		{
			if (query.Trim().Length == 0)
			{
				// Nothing typed yet: surface recently-opened tags (most-recent-first) rather than
				// an arbitrary namespace slice - there is no meaningful "top of 12,000" with no
				// query to rank against.
				foreach (var key in _recentTags.Recent)
				{
					if (!_tagsByKey.TryGetValue(key, out var entry)) continue;
					_results.Add(PaletteResultItem.ForTag(entry, () => ActivateTag(entry)) with { SecondaryText = "recent · " + entry.SourceName });
				}
			}
			else
			{
				// Search a wider pool than is shown so the recency re-rank below can pull a recently
				// -used tag up from just outside the visible cut rather than only ever re-ordering
				// within whatever the raw fuzzy score alone already put in the top MaxResults.
				var hits = _tagIndex.Search(query, MaxResults * 3);
				var ranked = hits
					.Select(h => (h.Entry, Score: h.Score + RecencyBonus(h.Entry)))
					.OrderByDescending(h => h.Score)
					.Take(MaxResults);

				foreach (var (entry, _) in ranked)
					_results.Add(PaletteResultItem.ForTag(entry, () => ActivateTag(entry)));
			}

			_footerHint.Text = $"{_tagIndex.Count:N0} tags indexed    ↑↓ navigate    ↵ open    esc close";
		}

		private int RecencyBonus(TagSearchEntry entry)
		{
			int rank = _recentTags.RankOf(TagKey(entry));
			return rank < 0 ? 0 : 40 - rank * 2;
		}

		private void ActivateTag(TagSearchEntry entry)
		{
			_recentTags.Touch(TagKey(entry));
			Close();
			_onSelectTag?.Invoke(entry.Token);
		}
	}
}
