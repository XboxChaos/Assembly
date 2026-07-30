using System;
using System.Globalization;
using Assembly.MultiPlatform.Services;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace Assembly.MultiPlatform.Views.Editors
{
	/// <summary>
	///     Editor for the fixed-width integer kinds (UInt8 through Int64). A decimal/hex toggle
	///     controls both how the current value is displayed and how new text is interpreted; either
	///     way, a value that does not fit the field's declared bit width is rejected with a visible
	///     error and never written - not clamped, not wrapped, not silently truncated to fit.
	///
	///     UInt64 gets special handling throughout: <see cref="FieldEditState.Int" /> stores every
	///     integer kind in a signed 64-bit slot (see its own remarks), so a UInt64 value above
	///     <see cref="long.MaxValue" /> is held as a negative <c>long</c> whose bit pattern happens
	///     to be the right unsigned value - correct for the round trip through
	///     <see cref="MetaValueWriter" />, but it must never be *shown* to a user as a negative
	///     number, so every format/parse path here reinterprets it through <see cref="ulong" />.
	/// </summary>
	public sealed class IntegerEditor : FieldEditorBase
	{
		private TextBox _box = null!;
		private TextBlock _err = null!;
		private RadioButton _hexBtn = null!;
		private FieldEditState? _before;

		private bool HexMode => _hexBtn.IsChecked == true;

		protected override void OnBind()
		{
			var def = Context.Row.Def;
			var edit = Context.Row.Current!;
			var groupName = "IntMode" + GetHashCode();

			var decBtn = new RadioButton { Content = "Decimal", GroupName = groupName, IsChecked = true };
			_hexBtn = new RadioButton { Content = "Hex", GroupName = groupName };
			var modeRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
			modeRow.Children.Add(decBtn);
			modeRow.Children.Add(_hexBtn);
			decBtn.Click += (_, _) => RefreshDisplay();
			_hexBtn.Click += (_, _) => RefreshDisplay();

			_box = EditorVisuals.NumberBox(Format(edit.Int ?? 0, def, false));
			_err = EditorVisuals.ErrorText();

			_box.GotFocus += (_, _) => _before = Context.Snapshot();
			_box.LostFocus += (_, _) => FinishGesture();
			_box.KeyDown += (_, e) =>
			{
				if (e.Key == Key.Enter) { FinishGesture(); e.Handled = true; }
				else if (e.Key == Key.Escape) { CancelGesture(); e.Handled = true; }
			};
			_box.TextChanged += (_, _) => ApplyLiveText();

			Children.Add(modeRow);
			Children.Add(EditorVisuals.LabeledRow("Value", _box, _err));
			Children.Add(new TextBlock { Text = RangeLabel(def), Classes = { "label", "dim" }, FontSize = 10 });
		}

		protected override void OnRowChanged(string? propertyName)
		{
			if (_box.IsFocused) return; // the user is typing here right now; do not fight their cursor
			RefreshDisplay();
		}

		private void RefreshDisplay()
		{
			var edit = Context.Row.Current!;
			_box.Text = Format(edit.Int ?? 0, Context.Row.Def, HexMode);
			EditorVisuals.MarkValid(_box, _err);
		}

		private void ApplyLiveText()
		{
			if (!TryParseInteger(_box.Text ?? "", HexMode, Context.Row.Def, out var stored, out var error))
			{
				EditorVisuals.MarkInvalid(_box, _err, error!);
				return;
			}
			EditorVisuals.MarkValid(_box, _err);

			var edit = Context.Row.Current!;
			if (edit.Int == stored) return;
			edit.Int = stored;
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();
		}

		/// <summary>Ends the edit gesture: commits whatever last parsed successfully as one undo
		/// step, then reformats the box - which silently discards a trailing invalid keystroke
		/// (e.g. a bare "-" typed and then abandoned) back to the last good value rather than
		/// leaving the box showing text that was never actually stored.</summary>
		private void FinishGesture()
		{
			Context.Commit(_before);
			_before = null;
			RefreshDisplay();
		}

		private void CancelGesture()
		{
			Context.Cancel(_before);
			_before = null;
			RefreshDisplay();
		}

		public override string? GetClipboardValue()
		{
			var edit = Context.Row.Current;
			return edit?.Int == null ? null : Format(edit.Int.Value, Context.Row.Def, HexMode);
		}

		public override bool TryPasteValue(string text)
		{
			if (!TryParseInteger(text, HexMode, Context.Row.Def, out var stored, out _))
				return false;

			var before = Context.Snapshot();
			var edit = Context.Row.Current!;
			edit.Int = stored;
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();
			Context.Commit(before);
			RefreshDisplay();
			return true;
		}

		private static string RangeLabel(MetaFieldDef def)
		{
			if (def.Kind == MetaFieldKind.UInt64)
				return $"range: 0 .. {ulong.MaxValue} ({def.KindLabel})";
			var (min, max) = IntegerBounds(def.IntegerBits, def.IsSignedInteger);
			return $"range: {min} .. {max} ({def.KindLabel})";
		}

		private static (long Min, long Max) IntegerBounds(int bits, bool signed) => (signed, bits) switch
		{
			(false, 8) => (0, 255),
			(false, 16) => (0, 65535),
			(false, 32) => (0, 4294967295L),
			(true, 8) => (sbyte.MinValue, sbyte.MaxValue),
			(true, 16) => (short.MinValue, short.MaxValue),
			(true, 32) => (int.MinValue, int.MaxValue),
			(true, 64) => (long.MinValue, long.MaxValue),
			_ => (long.MinValue, long.MaxValue)
		};

		private static string Format(long stored, MetaFieldDef def, bool hex)
		{
			if (def.Kind == MetaFieldKind.UInt64)
			{
				ulong uv = unchecked((ulong)stored);
				return hex ? "0x" + uv.ToString("X", CultureInfo.InvariantCulture) : uv.ToString(CultureInfo.InvariantCulture);
			}

			if (!hex)
				return stored.ToString(CultureInfo.InvariantCulture);

			int bits = def.IntegerBits <= 0 ? 32 : def.IntegerBits;
			ulong pattern = bits >= 64 ? unchecked((ulong)stored) : unchecked((ulong)stored) & ((1UL << bits) - 1);
			int digits = Math.Max(1, bits / 4);
			return "0x" + pattern.ToString("X" + digits, CultureInfo.InvariantCulture);
		}

		/// <summary>Parses one integer field's typed or pasted text. Hex digits (with or without a
		/// leading "0x") are read as the field's raw unsigned bit pattern and then sign-extended if
		/// the field is signed, matching how a hex editor reads bytes rather than requiring a user
		/// to type a minus sign in front of a hex value. Decimal text is parsed as a normal signed
		/// (or, for UInt64, unsigned) number and range-checked against the field's declared width.</summary>
		private static bool TryParseInteger(string text, bool hexMode, MetaFieldDef def, out long stored, out string? error)
		{
			stored = 0;
			var t = (text ?? "").Trim();
			if (t.Length == 0) { error = "value required"; return false; }

			bool explicitHex = t.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
			string digits = explicitHex ? t[2..] : t;
			bool asHex = hexMode || explicitHex;
			int bits = def.IntegerBits <= 0 ? 32 : def.IntegerBits;

			if (asHex)
			{
				if (digits.Length == 0 || !ulong.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var uv))
				{
					error = "not a valid hex value";
					return false;
				}
				if (bits < 64 && uv > (1UL << bits) - 1)
				{
					error = $"too large for a {bits}-bit field (max 0x{(1UL << bits) - 1:X})";
					return false;
				}
				stored = def.IsSignedInteger ? SignExtend(unchecked((long)uv), bits) : unchecked((long)uv);
				error = null;
				return true;
			}

			if (def.Kind == MetaFieldKind.UInt64)
			{
				if (!ulong.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var uv))
				{
					error = "not a valid unsigned integer";
					return false;
				}
				stored = unchecked((long)uv);
				error = null;
				return true;
			}

			if (!long.TryParse(digits, NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed))
			{
				error = "not a valid integer";
				return false;
			}

			var (min, max) = IntegerBounds(bits, def.IsSignedInteger);
			if (parsed < min || parsed > max)
			{
				error = $"out of range for {def.KindLabel} ({min} .. {max})";
				return false;
			}

			stored = parsed;
			error = null;
			return true;
		}

		private static long SignExtend(long value, int bits)
		{
			if (bits >= 64) return value;
			long mask = (1L << bits) - 1;
			value &= mask;
			long signBit = 1L << (bits - 1);
			return (value ^ signBit) - signBit;
		}
	}
}
