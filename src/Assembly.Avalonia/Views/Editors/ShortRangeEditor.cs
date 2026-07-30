using System;
using System.Globalization;
using Assembly.Avalonia.Services;
using Avalonia.Controls;
using Avalonia.Input;

namespace Assembly.Avalonia.Views.Editors
{
	/// <summary>Editor for <see cref="MetaFieldKind.RangeInt16" />: a Min and a Max 16-bit signed integer.</summary>
	public sealed class ShortRangeEditor : FieldEditorBase
	{
		private static readonly string[] Labels = { "Min", "Max" };

		private TextBox[] _boxes = null!;
		private TextBlock[] _errors = null!;
		private FieldEditState?[] _before = null!;

		protected override void OnBind()
		{
			_boxes = new TextBox[2];
			_errors = new TextBlock[2];
			_before = new FieldEditState?[2];

			for (int i = 0; i < 2; i++)
			{
				int idx = i;
				var box = EditorVisuals.NumberBox(Component(Context.Row.Current!, idx).ToString(CultureInfo.InvariantCulture));
				var err = EditorVisuals.ErrorText();

				box.GotFocus += (_, _) => _before[idx] = Context.Snapshot();
				box.LostFocus += (_, _) => FinishGesture(idx);
				box.KeyDown += (_, e) =>
				{
					if (e.Key == Key.Enter) { FinishGesture(idx); e.Handled = true; }
					else if (e.Key == Key.Escape) { CancelGesture(idx); e.Handled = true; }
				};
				box.TextChanged += (_, _) => ApplyLiveText(idx);

				_boxes[i] = box;
				_errors[i] = err;
				Children.Add(EditorVisuals.LabeledRow(Labels[i], box, err));
			}
		}

		protected override void OnRowChanged(string? propertyName)
		{
			for (int i = 0; i < 2; i++)
				if (!_boxes[i].IsFocused) RefreshDisplay(i);
		}

		private void RefreshDisplay(int idx)
		{
			_boxes[idx].Text = Component(Context.Row.Current!, idx).ToString(CultureInfo.InvariantCulture);
			EditorVisuals.MarkValid(_boxes[idx], _errors[idx]);
		}

		private void ApplyLiveText(int idx)
		{
			var box = _boxes[idx];
			if (!short.TryParse(box.Text, NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v))
			{
				EditorVisuals.MarkInvalid(box, _errors[idx], "not a valid 16-bit integer (-32768 .. 32767)");
				return;
			}
			EditorVisuals.MarkValid(box, _errors[idx]);

			var edit = Context.Row.Current!;
			if (Component(edit, idx) == v) return;
			SetComponent(edit, idx, v);
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();
		}

		private void FinishGesture(int idx)
		{
			Context.Commit(_before[idx]);
			_before[idx] = null;
			RefreshDisplay(idx);
		}

		private void CancelGesture(int idx)
		{
			Context.Cancel(_before[idx]);
			_before[idx] = null;
			RefreshDisplay(idx);
		}

		public override string? GetClipboardValue()
		{
			var edit = Context.Row.Current;
			return edit?.Shorts == null ? null : $"{Component(edit, 0)}, {Component(edit, 1)}";
		}

		public override bool TryPasteValue(string text)
		{
			var edit = Context.Row.Current;
			if (edit?.Shorts == null) return false;

			var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length != 2) return false;
			if (!short.TryParse(parts[0], NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var lo)) return false;
			if (!short.TryParse(parts[1], NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var hi)) return false;

			var before = Context.Snapshot();
			edit.ShortLo = lo;
			edit.ShortHi = hi;
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();
			Context.Commit(before);
			RefreshDisplay(0);
			RefreshDisplay(1);
			return true;
		}

		private static short Component(FieldEditState edit, int idx) => idx == 0 ? edit.ShortLo : edit.ShortHi;

		private static void SetComponent(FieldEditState edit, int idx, short v)
		{
			if (idx == 0) edit.ShortLo = v; else edit.ShortHi = v;
		}
	}
}
