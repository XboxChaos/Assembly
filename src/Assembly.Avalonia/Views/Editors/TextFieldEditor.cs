using System;
using System.Text;
using Assembly.Avalonia.Services;
using Avalonia.Controls;
using Avalonia.Input;

namespace Assembly.Avalonia.Views.Editors
{
	/// <summary>
	///     Editor for <see cref="MetaFieldKind.Ascii" /> and <see cref="MetaFieldKind.Utf16" />: a
	///     fixed-width string field. The budget is always shown, a live counter tracks how much of
	///     it is used, and text longer than the budget is never silently cut down to fit - it is
	///     left uncommitted with a visible warning until the user shortens it, exactly like an
	///     out-of-range number is rejected rather than wrapped.
	/// </summary>
	/// <remarks>
	///     A classic plugin's <c>ascii</c>/<c>utf16</c> field and a Campaign Evolved "string"/"long
	///     string" field (mapped onto <see cref="MetaFieldKind.Ascii" /> by
	///     <see cref="Assembly.Avalonia.ViewModels.TagDocumentViewModel.GetFifthGenScalarDef" />)
	///     share this editor's shape - a fixed-width text box with a live budget - but not its
	///     byte-counting rule: <see cref="MetaFieldDef.Utf8Budget" /> switches the budget from
	///     one-byte-per-character (classic <c>ascii</c>'s Latin-1 write path, or the character count
	///     classic <c>utf16</c> writes two bytes each for) to a UTF-8 byte count with one byte
	///     reserved for the terminator <c>FifthGenStringValue.SetValue</c> requires - the same
	///     encoding and the same reservation that class's own write path enforces, so a value this
	///     editor accepts is always one that write path will too.
	/// </remarks>
	public sealed class TextFieldEditor : FieldEditorBase
	{
		private TextBox _box = null!;
		private TextBlock _budget = null!;
		private TextBlock _warn = null!;
		private FieldEditState? _before;
		private int _maxUnits;
		private bool _utf8;

		protected override void OnBind()
		{
			var def = Context.Row.Def;
			var edit = Context.Row.Current!;
			bool ascii = def.Kind == MetaFieldKind.Ascii;
			_utf8 = def.Utf8Budget;
			_maxUnits = _utf8 ? Math.Max(0, def.Size - 1) : ascii ? def.Size : def.Size / 2;

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

			string label = _utf8
				? $"Text (UTF-8, byte budget: {_maxUnits} - one more reserved for the terminator)"
				: $"Text ({(ascii ? "ASCII" : "UTF-16")}, fixed-width budget: {_maxUnits} chars)";
			Children.Add(EditorVisuals.LabeledRow(label, _box, _warn));
			Children.Add(_budget);
			RefreshBudget();
		}

		protected override void OnRowChanged(string? propertyName)
		{
			if (_box.IsFocused) return;
			_box.Text = Context.Row.Current!.Text ?? "";
			RefreshBudget();
		}

		private int Measure(string text) => _utf8 ? Encoding.UTF8.GetByteCount(text) : text.Length;
		private string Unit(int n) => _utf8 ? (n == 1 ? "byte" : "bytes") : (n == 1 ? "character" : "characters");

		private void RefreshBudget()
		{
			int len = Measure(_box.Text ?? "");
			_budget.Text = _utf8 ? $"{len} / {_maxUnits} bytes used" : $"{len} / {_maxUnits} characters used";
		}

		private void ApplyLiveText()
		{
			var text = _box.Text ?? "";
			RefreshBudget();

			int len = Measure(text);
			if (len > _maxUnits)
			{
				EditorVisuals.MarkInvalid(_box, _warn, $"too long by {len - _maxUnits} {Unit(len - _maxUnits)} - this field is fixed-width and will not be saved until it fits.");
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
			if (Measure(_box.Text ?? "") > _maxUnits)
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
			if (Measure(text) > _maxUnits) return false;

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
