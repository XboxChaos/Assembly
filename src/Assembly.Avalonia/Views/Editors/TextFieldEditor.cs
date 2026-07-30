using System;
using Assembly.Avalonia.Services;
using Avalonia.Controls;
using Avalonia.Input;

namespace Assembly.Avalonia.Views.Editors
{
	/// <summary>
	///     Editor for <see cref="MetaFieldKind.Ascii" /> and <see cref="MetaFieldKind.Utf16" />: a
	///     fixed-width string field. The budget (the plugin's declared byte size, halved for UTF-16)
	///     is always shown, a live counter tracks how much of it is used, and text longer than the
	///     budget is never silently cut down to fit - it is left uncommitted with a visible warning
	///     until the user shortens it, exactly like an out-of-range number is rejected rather than
	///     wrapped.
	/// </summary>
	public sealed class TextFieldEditor : FieldEditorBase
	{
		private TextBox _box = null!;
		private TextBlock _budget = null!;
		private TextBlock _warn = null!;
		private FieldEditState? _before;
		private int _maxChars;

		protected override void OnBind()
		{
			var def = Context.Row.Def;
			var edit = Context.Row.Current!;
			bool ascii = def.Kind == MetaFieldKind.Ascii;
			_maxChars = ascii ? def.Size : def.Size / 2;

			_box = new TextBox { Text = edit.Text ?? "", Width = 260 };
			_budget = EditorVisuals.Hint("");
			_warn = EditorVisuals.WarnText();

			_box.GotFocus += (_, _) => _before = Context.Snapshot();
			_box.LostFocus += (_, _) => FinishGesture();
			_box.KeyDown += (_, e) =>
			{
				if (e.Key == Key.Enter) { FinishGesture(); e.Handled = true; }
				else if (e.Key == Key.Escape) { CancelGesture(); e.Handled = true; }
			};
			_box.TextChanged += (_, _) => ApplyLiveText();

			Children.Add(EditorVisuals.LabeledRow($"Text ({(ascii ? "ASCII" : "UTF-16")}, fixed-width budget: {_maxChars} chars)", _box, _warn));
			Children.Add(_budget);
			RefreshBudget();
		}

		protected override void OnRowChanged(string? propertyName)
		{
			if (_box.IsFocused) return;
			_box.Text = Context.Row.Current!.Text ?? "";
			RefreshBudget();
		}

		private void RefreshBudget()
		{
			int len = (_box.Text ?? "").Length;
			_budget.Text = $"{len} / {_maxChars} characters used";
		}

		private void ApplyLiveText()
		{
			var text = _box.Text ?? "";
			RefreshBudget();

			if (text.Length > _maxChars)
			{
				EditorVisuals.MarkInvalid(_box, _warn, $"too long by {text.Length - _maxChars} character(s) - this field is fixed-width and will not be saved until it fits.");
				return;
			}
			EditorVisuals.MarkValid(_box, _warn);

			var edit = Context.Row.Current!;
			if (edit.Text == text) return;
			edit.Text = text;
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();
		}

		private void FinishGesture()
		{
			Context.Commit(_before);
			_before = null;
			// A trailing over-budget attempt never made it into edit.Text (see ApplyLiveText); show
			// the last value that actually stuck rather than leaving the rejected text on screen.
			if ((_box.Text ?? "").Length > _maxChars)
			{
				_box.Text = Context.Row.Current!.Text ?? "";
				EditorVisuals.MarkValid(_box, _warn);
			}
			RefreshBudget();
		}

		private void CancelGesture()
		{
			Context.Cancel(_before);
			_before = null;
			_box.Text = Context.Row.Current!.Text ?? "";
			EditorVisuals.MarkValid(_box, _warn);
			RefreshBudget();
		}

		public override string? GetClipboardValue() => Context.Row.Current?.Text;

		public override bool TryPasteValue(string text)
		{
			if (text.Length > _maxChars) return false;

			var before = Context.Snapshot();
			var edit = Context.Row.Current!;
			edit.Text = text;
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();
			Context.Commit(before);
			_box.Text = text;
			RefreshBudget();
			return true;
		}
	}
}
