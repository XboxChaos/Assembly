using System.Globalization;
using System.Linq;
using Assembly.Avalonia.Services;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Assembly.Avalonia.Views.Editors
{
	/// <summary>
	///     Editor for <see cref="MetaFieldKind.Flags" />: one checkbox per named bit, plus the raw
	///     word itself shown as editable hex - kept in sync both ways, so a bit the plugin has not
	///     named (not uncommon; plugins do not always account for every bit) is still visible and
	///     settable through the raw box even though it has no checkbox of its own.
	/// </summary>
	public sealed class FlagsEditor : FieldEditorBase
	{
		private CheckBox[] _boxes = null!;
		private TextBox _rawBox = null!;
		private TextBlock _rawErr = null!;
		private FieldEditState? _rawBefore;
		private bool _suppressCheckboxEvents;

		protected override void OnBind()
		{
			var edit = Context.Row.Current!;
			var choices = Context.Row.Choices;
			int bits = System.Math.Max(1, Context.Row.Def.Size * 8);

			var list = new StackPanel { Spacing = 2 };
			_boxes = new CheckBox[choices.Count];
			for (int i = 0; i < choices.Count; i++)
			{
				var choice = choices[i];
				var cb = new CheckBox
				{
					Content = $"{choice.Name}  (0x{choice.Value:X})",
					IsChecked = ((edit.Int ?? 0) & choice.Value) != 0,
					FontSize = 11.5
				};
				cb.PropertyChanged += (_, ev) =>
				{
					if (_suppressCheckboxEvents || ev.Property != ToggleButton.IsCheckedProperty) return;
					var before = Context.Snapshot();
					long v = edit.Int ?? 0;
					edit.Int = cb.IsChecked == true ? v | choice.Value : v & ~choice.Value;
					Context.Row.NotifyEdited();
					Context.Doc.RecomputeDirty();
					Context.Commit(before);
					RefreshRawBox();
				};
				_boxes[i] = cb;
				list.Children.Add(cb);
			}

			if (choices.Count == 0)
				list.Children.Add(EditorVisuals.Hint("This bitfield has no named bits in the plugin - use the raw word below."));

			_rawBox = EditorVisuals.NumberBox(FormatRaw(edit.Int ?? 0, bits));
			_rawErr = EditorVisuals.ErrorText();
			_rawBox.GotFocus += (_, _) => _rawBefore = Context.Snapshot();
			_rawBox.LostFocus += (_, _) => FinishRawGesture();
			_rawBox.KeyDown += (_, e) =>
			{
				if (e.Key == Key.Enter) { FinishRawGesture(); e.Handled = true; }
				else if (e.Key == Key.Escape) { CancelRawGesture(); e.Handled = true; }
			};
			_rawBox.TextChanged += (_, _) => ApplyRawLiveText(bits);

			Children.Add(list);
			Children.Add(EditorVisuals.Separator());
			Children.Add(EditorVisuals.LabeledRow($"Raw word (0x, {bits} bits)", _rawBox, _rawErr));
		}

		protected override void OnRowChanged(string? propertyName)
		{
			RefreshCheckboxes();
			if (!_rawBox.IsFocused) RefreshRawBox();
		}

		private void RefreshCheckboxes()
		{
			var edit = Context.Row.Current!;
			var choices = Context.Row.Choices;
			_suppressCheckboxEvents = true;
			for (int i = 0; i < _boxes.Length; i++)
				_boxes[i].IsChecked = ((edit.Int ?? 0) & choices[i].Value) != 0;
			_suppressCheckboxEvents = false;
		}

		private void RefreshRawBox()
		{
			int bits = System.Math.Max(1, Context.Row.Def.Size * 8);
			_rawBox.Text = FormatRaw(Context.Row.Current!.Int ?? 0, bits);
			EditorVisuals.MarkValid(_rawBox, _rawErr);
		}

		private void ApplyRawLiveText(int bits)
		{
			var text = (_rawBox.Text ?? "").Trim().TrimStart('#');
			if (text.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase)) text = text[2..];

			if (!ulong.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v) ||
			    (bits < 64 && v > (1UL << bits) - 1))
			{
				EditorVisuals.MarkInvalid(_rawBox, _rawErr, $"not a valid {bits}-bit hex word");
				return;
			}
			EditorVisuals.MarkValid(_rawBox, _rawErr);

			var edit = Context.Row.Current!;
			long stored = unchecked((long)v);
			if (edit.Int == stored) return;
			edit.Int = stored;
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();
			RefreshCheckboxes();
		}

		private void FinishRawGesture()
		{
			Context.Commit(_rawBefore);
			_rawBefore = null;
			RefreshRawBox();
		}

		private void CancelRawGesture()
		{
			Context.Cancel(_rawBefore);
			_rawBefore = null;
			RefreshRawBox();
			RefreshCheckboxes();
		}

		private static string FormatRaw(long stored, int bits)
		{
			ulong pattern = bits >= 64 ? unchecked((ulong)stored) : unchecked((ulong)stored) & ((1UL << bits) - 1);
			int digits = System.Math.Max(1, bits / 4);
			return "0x" + pattern.ToString("X" + digits, CultureInfo.InvariantCulture);
		}

		public override string? GetClipboardValue() => _rawBox.Text;

		public override bool TryPasteValue(string text)
		{
			int bits = System.Math.Max(1, Context.Row.Def.Size * 8);
			var t = text.Trim().TrimStart('#');
			if (t.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase)) t = t[2..];
			if (!ulong.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v)) return false;
			if (bits < 64 && v > (1UL << bits) - 1) return false;

			var before = Context.Snapshot();
			var edit = Context.Row.Current!;
			edit.Int = unchecked((long)v);
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();
			Context.Commit(before);
			RefreshCheckboxes();
			RefreshRawBox();
			return true;
		}
	}
}
