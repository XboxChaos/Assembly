using System.Collections.Generic;
using System.Linq;
using Assembly.MultiPlatform.Services;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Assembly.MultiPlatform.Views
{
	public sealed record ShortcutRow(string Title, string Label);

	public sealed record ShortcutGroup(string Title, IReadOnlyList<ShortcutRow> Rows);

	/// <summary>
	///     The "?" keyboard-shortcuts cheatsheet - see this file's own remarks in
	///     ShortcutsOverlay.axaml. Purely a read-only reference: its content is derived from the
	///     same <see cref="CommandRegistry" /> the palette searches (every registered command with
	///     a bound shortcut, grouped by category) plus a short hand-written list of navigation keys
	///     that are not registry commands at all (Tab/arrow/Enter/Escape - there is no "action" to
	///     register for "move focus", only a key to document). Open/Close/Escape-to-dismiss is
	///     driven by <c>MainWindow</c>'s window-level key handler, the same as
	///     <see cref="CommandPalette" /> - see that handler's own remarks for why.
	/// </summary>
	public partial class ShortcutsOverlay : UserControl
	{
		private readonly ItemsControl _groupList;

		public bool IsOpen { get; private set; }

		private IInputElement? _restoreFocusTo;

		public ShortcutsOverlay()
		{
			InitializeComponent();
			_groupList = this.FindControl<ItemsControl>("GroupList")!;
		}

		private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

		public void Open(CommandRegistry commands)
		{
			if (IsOpen) return;
			_restoreFocusTo = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
			_groupList.ItemsSource = BuildGroups(commands);
			IsVisible = true;
			IsHitTestVisible = true;
			IsOpen = true;
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

		private void OnScrimPointerPressed(object? sender, PointerPressedEventArgs e) => Close();
		private void OnBoxPointerPressed(object? sender, PointerPressedEventArgs e) => e.Handled = true;

		private static List<ShortcutGroup> BuildGroups(CommandRegistry commands)
		{
			var navigation = new ShortcutGroup("Focus & navigation", new[]
			{
				new ShortcutRow("Move focus between tag tree, field table and properties", "Tab / ⇧Tab"),
				new ShortcutRow("Move selection up/down in a list, tree or the palette", "↑ / ↓"),
				new ShortcutRow("Expand a block, or step into a collapsed one's parent", "→ / ←"),
				new ShortcutRow("Open the selected tag reference, or expand/collapse a block", "↵"),
				new ShortcutRow("Close the palette, then clear a filter, then cancel an edit", "Esc")
			});

			var groups = new List<ShortcutGroup> { navigation };
			groups.AddRange(commands.Commands
				.Where(c => c.ShortcutLabel != null)
				.GroupBy(c => c.Category)
				.Select(g => new ShortcutGroup(g.Key,
					g.Select(c => new ShortcutRow(c.Title, c.ShortcutLabel!)).ToList())));

			return groups;
		}
	}
}
