using System.Globalization;
using System.Linq;
using Assembly.Avalonia.ViewModels;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Assembly.Avalonia.Views.Editors
{
	/// <summary>
	///     Editor for <see cref="Assembly.Avalonia.Services.MetaFieldKind.Enum" />: a searchable,
	///     type-to-filter dropdown rather than a plain <see cref="ComboBox" />. Some Halo enums
	///     (weapon/object type lists in particular) run to hundreds of options, which is exactly
	///     what makes scanning a long unfiltered list impractical - <see cref="AutoCompleteBox" />
	///     gives free text filtering over the same <see cref="MetaRowViewModel.Choices" /> list the
	///     old plain combo used.
	/// </summary>
	public sealed class EnumEditor : FieldEditorBase
	{
		private AutoCompleteBox _box = null!;
		private TextBlock _unmatched = null!;

		protected override void OnBind()
		{
			var edit = Context.Row.Current!;
			var choices = Context.Row.Choices;

			_box = new AutoCompleteBox
			{
				ItemsSource = choices,
				FilterMode = AutoCompleteFilterMode.Contains,
				MinimumPrefixLength = 0,
				PlaceholderText = $"Search {choices.Count} option(s)...",
				HorizontalAlignment = HorizontalAlignment.Stretch
			};

			var current = choices.FirstOrDefault(c => c.Value == edit.Int);
			_box.SelectedItem = current;
			_box.Text = current?.Name ?? "";

			_box.GotFocus += (_, _) => _box.IsDropDownOpen = true;
			_box.SelectionChanged += (_, _) =>
			{
				if (_box.SelectedItem is not ChoiceOption c) return;
				if (edit.Int == c.Value) return;
				var before = Context.Snapshot();
				edit.Int = c.Value;
				Context.Row.NotifyEdited();
				Context.Doc.RecomputeDirty();
				Context.Commit(before);
			};

			_unmatched = EditorVisuals.Hint("");
			_unmatched.IsVisible = false;

			Children.Add(EditorVisuals.LabeledRow("Selected option", _box));
			Children.Add(_unmatched);
			RefreshUnmatchedNotice();
		}

		protected override void OnRowChanged(string? propertyName)
		{
			if (_box.IsFocused) return;
			var edit = Context.Row.Current!;
			var choices = Context.Row.Choices;
			var current = choices.FirstOrDefault(c => c.Value == edit.Int);
			_box.SelectedItem = current;
			_box.Text = current?.Name ?? "";
			RefreshUnmatchedNotice();
		}

		private void RefreshUnmatchedNotice()
		{
			var edit = Context.Row.Current!;
			var choices = Context.Row.Choices;
			bool matched = choices.Any(c => c.Value == edit.Int);
			_unmatched.IsVisible = !matched && edit.Int != null;
			if (!matched && edit.Int != null)
				_unmatched.Text = $"Raw value {edit.Int} does not match any named option in the plugin.";
		}

		public override string? GetClipboardValue()
		{
			var edit = Context.Row.Current;
			return edit?.Int?.ToString(CultureInfo.InvariantCulture);
		}

		public override bool TryPasteValue(string text)
		{
			var t = text.Trim();
			var choices = Context.Row.Choices;

			ChoiceOption? match = choices.FirstOrDefault(c => c.Name.Equals(t, System.StringComparison.OrdinalIgnoreCase));
			long? value = match?.Value;
			if (value == null && long.TryParse(t, out var raw))
				value = raw;
			if (value == null) return false;

			var edit = Context.Row.Current!;
			if (edit.Int == value) return true;

			var before = Context.Snapshot();
			edit.Int = value;
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();
			Context.Commit(before);

			var current = choices.FirstOrDefault(c => c.Value == value);
			_box.SelectedItem = current;
			_box.Text = current?.Name ?? "";
			RefreshUnmatchedNotice();
			return true;
		}
	}
}
