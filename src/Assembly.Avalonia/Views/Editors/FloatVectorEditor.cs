using System;
using System.Globalization;
using System.Linq;
using Assembly.Avalonia.Services;
using Avalonia.Controls;
using Avalonia.Input;

namespace Assembly.Avalonia.Views.Editors
{
	/// <summary>
	///     Editor for every float-vector-shaped kind: a single float or angle, a 2/3/4-wide vector,
	///     or a float range. One component gets one text box, labelled with what the component
	///     actually means for this field's declared type (<see cref="MetaFieldDef.ComponentLabels" />
	///     - i/j/k for a direction, x/y/z for a position, yaw/pitch/roll for an orientation, min/max
	///     for a range) rather than a generic index, matching this codebase's own WPF meta editor's
	///     convention for the same distinction.
	/// </summary>
	public sealed class FloatVectorEditor : FieldEditorBase
	{
		private TextBox[] _boxes = null!;
		private TextBlock[] _errors = null!;
		private FieldEditState?[] _before = null!;

		protected override void OnBind()
		{
			var def = Context.Row.Def;
			var edit = Context.Row.Current!;
			var labels = def.ComponentLabels;
			int n = edit.Floats?.Length ?? labels.Length;

			_boxes = new TextBox[n];
			_errors = new TextBlock[n];
			_before = new FieldEditState?[n];

			for (int i = 0; i < n; i++)
			{
				int idx = i; // captured per-component
				var box = EditorVisuals.NumberBox(FormatComponent(edit, idx));
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
				string label = idx < labels.Length ? labels[idx] : idx.ToString();
				Children.Add(EditorVisuals.LabeledRow(label, box, err));
			}
		}

		protected override void OnRowChanged(string? propertyName)
		{
			for (int i = 0; i < _boxes.Length; i++)
			{
				if (_boxes[i].IsFocused) continue;
				RefreshDisplay(i);
			}
		}

		private void RefreshDisplay(int idx)
		{
			_boxes[idx].Text = FormatComponent(Context.Row.Current!, idx);
			EditorVisuals.MarkValid(_boxes[idx], _errors[idx]);
		}

		private void ApplyLiveText(int idx)
		{
			var box = _boxes[idx];
			if (!float.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
			{
				EditorVisuals.MarkInvalid(box, _errors[idx], "not a valid number");
				return;
			}
			EditorVisuals.MarkValid(box, _errors[idx]);

			var edit = Context.Row.Current!;
			if (edit.Floats == null || idx >= edit.Floats.Length || edit.Floats[idx] == v) return;
			edit.Floats[idx] = v;
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
			return edit?.Floats == null ? null : string.Join(", ", edit.Floats.Select(F));
		}

		public override bool TryPasteValue(string text)
		{
			var edit = Context.Row.Current;
			if (edit?.Floats == null) return false;

			var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length != edit.Floats.Length) return false;

			var parsed = new float[parts.Length];
			for (int i = 0; i < parts.Length; i++)
				if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out parsed[i]))
					return false;

			var before = Context.Snapshot();
			Array.Copy(parsed, edit.Floats, parsed.Length);
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();
			Context.Commit(before);
			for (int i = 0; i < _boxes.Length; i++) RefreshDisplay(i);
			return true;
		}

		private static string FormatComponent(FieldEditState edit, int idx) =>
			edit.Floats != null && idx < edit.Floats.Length ? F(edit.Floats[idx]) : "0";

		private static string F(float f) => f.ToString("0.######", CultureInfo.InvariantCulture);
	}
}
