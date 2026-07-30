using System;
using System.Globalization;
using Assembly.MultiPlatform.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Assembly.MultiPlatform.Views.Editors
{
	/// <summary>
	///     Editor for <see cref="Assembly.MultiPlatform.Services.MetaFieldKind.ColorInt" />: a swatch, a
	///     hex box, and a real HSV picker (a saturation/value square plus a hue strip, each
	///     draggable) instead of four bare numeric spinners. <see cref="MetaFieldDef.Note" /> being
	///     "argb" (set by <c>PluginSchemaVisitor.VisitColorInt</c>) is what distinguishes a colour
	///     that carries an alpha channel from one that does not; a channel-less colour gets no alpha
	///     slider and is stored/shown with alpha forced to 0xFF.
	/// </summary>
	public sealed class ColorEditor : FieldEditorBase
	{
		private const double SquareSize = 150;
		private const double HueBarSize = 150;

		private bool _hasAlpha;
		private double _h, _s, _v; // hue 0..360, saturation/value 0..1
		private byte _a = 255;

		private Border _swatch = null!;
		private TextBox _hexBox = null!;
		private TextBlock _hexErr = null!;
		private TextBlock _alphaLabel = null!;
		private Slider? _alphaSlider;
		private Canvas _svSquare = null!;
		private Border _svHueLayer = null!;
		private Ellipse _svThumb = null!;
		private Border _hueBar = null!;
		private Border _hueThumb = null!;
		private Control _hueBarPanel = null!;

		private FieldEditState? _dragBefore;
		private bool _dragging;

		protected override void OnBind()
		{
			var edit = Context.Row.Current!;
			_hasAlpha = Context.Row.Def.Note == "argb";
			uint raw = unchecked((uint)(edit.Int ?? 0));
			_a = _hasAlpha ? (byte)(raw >> 24) : (byte)255;
			RgbToHsv((byte)(raw >> 16), (byte)(raw >> 8), (byte)raw, out _h, out _s, out _v);

			_swatch = new Border { Width = 48, Height = 32, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };

			_hexBox = EditorVisuals.NumberBox(HexText(raw), 130);
			_hexErr = EditorVisuals.ErrorText();
			_hexBox.GotFocus += (_, _) => _dragBefore = Context.Snapshot();
			_hexBox.LostFocus += (_, _) => FinishHexGesture();
			_hexBox.KeyDown += (_, e) =>
			{
				if (e.Key == Key.Enter) { FinishHexGesture(); e.Handled = true; }
				else if (e.Key == Key.Escape) { Context.Cancel(_dragBefore); _dragBefore = null; RefreshAll(); e.Handled = true; }
			};
			_hexBox.TextChanged += (_, _) => ApplyHexLiveText();

			var swatchRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
			swatchRow.Children.Add(_swatch);
			swatchRow.Children.Add(EditorVisuals.LabeledRow(_hasAlpha ? "ARGB (hex)" : "RGB (hex)", _hexBox, _hexErr));

			BuildSvSquare();
			BuildHueBar();

			Children.Add(swatchRow);
			Children.Add(_svSquare);
			Children.Add(_hueBarPanel);

			if (_hasAlpha)
			{
				_alphaSlider = new Slider { Minimum = 0, Maximum = 255, Value = _a, Width = SquareSize };
				_alphaLabel = new TextBlock { Classes = { "label" }, FontSize = 10.5 };
				_alphaSlider.AddHandler(PointerPressedEvent, (_, _) => _dragBefore = Context.Snapshot(), RoutingStrategies.Tunnel);
				_alphaSlider.AddHandler(PointerReleasedEvent, (_, _) => { Context.Commit(_dragBefore); _dragBefore = null; }, RoutingStrategies.Tunnel);
				_alphaSlider.PropertyChanged += (_, e) =>
				{
					if (e.Property != RangeBase.ValueProperty) return;
					_a = (byte)_alphaSlider.Value;
					ApplyFromHsva(pushHistory: false);
				};
				Children.Add(EditorVisuals.LabeledRow("Alpha", _alphaSlider, _alphaLabel));
			}

			RefreshAll();
		}

		private void BuildSvSquare()
		{
			_svSquare = new Canvas { Width = SquareSize, Height = SquareSize, ClipToBounds = true };
			_svHueLayer = new Border { Width = SquareSize, Height = SquareSize, Background = Brushes.Red };
			var whiteFade = new Border
			{
				Width = SquareSize,
				Height = SquareSize,
				Background = new LinearGradientBrush
				{
					StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
					EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
					GradientStops =
					{
						new GradientStop(Colors.White, 0),
						new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
					}
				}
			};
			var blackFade = new Border
			{
				Width = SquareSize,
				Height = SquareSize,
				Background = new LinearGradientBrush
				{
					StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
					EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
					GradientStops =
					{
						new GradientStop(Color.FromArgb(0, 0, 0, 0), 0),
						new GradientStop(Colors.Black, 1)
					}
				}
			};
			_svThumb = new Ellipse { Width = 10, Height = 10, Stroke = Brushes.White, StrokeThickness = 2, IsHitTestVisible = false };

			_svSquare.Children.Add(_svHueLayer);
			_svSquare.Children.Add(whiteFade);
			_svSquare.Children.Add(blackFade);
			_svSquare.Children.Add(_svThumb);

			_svSquare.PointerPressed += (_, e) => { _dragging = true; _dragBefore = Context.Snapshot(); ApplySvFromPointer(e.GetPosition(_svSquare)); };
			_svSquare.PointerMoved += (_, e) => { if (_dragging) ApplySvFromPointer(e.GetPosition(_svSquare)); };
			_svSquare.PointerReleased += (_, _) => { _dragging = false; Context.Commit(_dragBefore); _dragBefore = null; };
		}

		private void BuildHueBar()
		{
			var rainbow = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative) };
			(double Offset, Color Color)[] stops =
			{
				(0, Colors.Red), (1.0 / 6, Colors.Yellow), (2.0 / 6, Colors.Lime), (3.0 / 6, Colors.Cyan),
				(4.0 / 6, Colors.Blue), (5.0 / 6, Colors.Magenta), (1, Colors.Red)
			};
			foreach (var stop in stops) rainbow.GradientStops.Add(new GradientStop(stop.Color, stop.Offset));

			_hueBar = new Border { Width = HueBarSize, Height = 18, Background = rainbow };
			_hueThumb = new Border { Width = 3, Height = 18, Background = Brushes.White, HorizontalAlignment = HorizontalAlignment.Left };
			var overlay = new Canvas { Width = HueBarSize, Height = 18 };
			overlay.Children.Add(_hueThumb);
			var host = new Panel { Width = HueBarSize, Height = 18 };
			host.Children.Add(_hueBar);
			host.Children.Add(overlay);

			host.PointerPressed += (_, e) => { _dragging = true; _dragBefore = Context.Snapshot(); ApplyHueFromPointer(e.GetPosition(_hueBar)); };
			host.PointerMoved += (_, e) => { if (_dragging) ApplyHueFromPointer(e.GetPosition(_hueBar)); };
			host.PointerReleased += (_, _) => { _dragging = false; Context.Commit(_dragBefore); _dragBefore = null; };

			var wrapped = new StackPanel { Spacing = 3 };
			wrapped.Children.Add(new TextBlock { Text = "Hue / saturation / value (drag)", Classes = { "label" }, FontSize = 10.5 });
			wrapped.Children.Add(host);
			_hueBarPanel = wrapped;
		}

		private void ApplySvFromPointer(Point p)
		{
			_s = Math.Clamp(p.X / SquareSize, 0, 1);
			_v = Math.Clamp(1 - p.Y / SquareSize, 0, 1);
			ApplyFromHsva(pushHistory: false);
		}

		private void ApplyHueFromPointer(Point p)
		{
			_h = Math.Clamp(p.X / HueBarSize, 0, 1) * 360;
			ApplyFromHsva(pushHistory: false);
		}

		/// <summary>Recomputes RGB from the current h/s/v/a, writes it live into the row (so the
		/// dirty strip and any Save mid-drag see it), and refreshes every visual. History is only
		/// ever pushed at the end of a drag/keystroke gesture, never per pointer-move sample.</summary>
		private void ApplyFromHsva(bool pushHistory)
		{
			HsvToRgb(_h, _s, _v, out var r, out var g, out var b);
			uint packed = _hasAlpha
				? ((uint)_a << 24) | ((uint)r << 16) | ((uint)g << 8) | b
				: 0xFFu << 24 | ((uint)r << 16) | ((uint)g << 8) | b;

			var edit = Context.Row.Current!;
			long stored = unchecked((long)packed);
			if (edit.Int != stored)
			{
				edit.Int = stored;
				Context.Row.NotifyEdited();
				Context.Doc.RecomputeDirty();
			}
			RefreshAll();
		}

		private void RefreshAll()
		{
			var edit = Context.Row.Current!;
			uint raw = unchecked((uint)(edit.Int ?? 0));
			byte r = (byte)(raw >> 16), g = (byte)(raw >> 8), b = (byte)raw;

			_swatch.Background = new SolidColorBrush(Color.FromArgb(255, r, g, b));
			if (!_hexBox.IsFocused) { _hexBox.Text = HexText(raw); EditorVisuals.MarkValid(_hexBox, _hexErr); }

			_svHueLayer.Background = new SolidColorBrush(HsvToColor(_h, 1, 1));
			Canvas.SetLeft(_svThumb, _s * SquareSize - _svThumb.Width / 2);
			Canvas.SetTop(_svThumb, (1 - _v) * SquareSize - _svThumb.Height / 2);
			_hueThumb.Margin = new Thickness(Math.Clamp(_h / 360 * HueBarSize - _hueThumb.Width / 2, 0, HueBarSize - _hueThumb.Width), 0, 0, 0);

			if (_alphaSlider != null && !_alphaSlider.IsPointerOver)
			{
				_alphaSlider.Value = _a;
				_alphaLabel.Text = _a.ToString(CultureInfo.InvariantCulture);
			}
		}

		protected override void OnRowChanged(string? propertyName)
		{
			if (_dragging || _hexBox.IsFocused) return;
			var edit = Context.Row.Current!;
			uint raw = unchecked((uint)(edit.Int ?? 0));
			_a = _hasAlpha ? (byte)(raw >> 24) : (byte)255;
			RgbToHsv((byte)(raw >> 16), (byte)(raw >> 8), (byte)raw, out _h, out _s, out _v);
			RefreshAll();
		}

		private void ApplyHexLiveText()
		{
			var t = (_hexBox.Text ?? "").Trim().TrimStart('#');
			if (!uint.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v) || t.Length is not (6 or 8))
			{
				EditorVisuals.MarkInvalid(_hexBox, _hexErr, _hasAlpha ? "not a valid hex colour (AARRGGBB or RRGGBB)" : "not a valid hex colour (RRGGBB)");
				return;
			}
			EditorVisuals.MarkValid(_hexBox, _hexErr);

			uint packed = t.Length == 6 ? 0xFFu << 24 | v : v;
			if (!_hasAlpha) packed = 0xFFu << 24 | (packed & 0xFFFFFF);

			var edit = Context.Row.Current!;
			long stored = unchecked((long)packed);
			if (edit.Int == stored) return;
			edit.Int = stored;
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();

			_a = _hasAlpha ? (byte)(packed >> 24) : (byte)255;
			RgbToHsv((byte)(packed >> 16), (byte)(packed >> 8), (byte)packed, out _h, out _s, out _v);
			_swatch.Background = new SolidColorBrush(Color.FromUInt32(0xFF000000 | (packed & 0xFFFFFF)));
			_svHueLayer.Background = new SolidColorBrush(HsvToColor(_h, 1, 1));
			Canvas.SetLeft(_svThumb, _s * SquareSize - _svThumb.Width / 2);
			Canvas.SetTop(_svThumb, (1 - _v) * SquareSize - _svThumb.Height / 2);
			_hueThumb.Margin = new Thickness(Math.Clamp(_h / 360 * HueBarSize - _hueThumb.Width / 2, 0, HueBarSize - _hueThumb.Width), 0, 0, 0);
		}

		private void FinishHexGesture()
		{
			Context.Commit(_dragBefore);
			_dragBefore = null;
			RefreshAll();
		}

		private string HexText(uint raw) => _hasAlpha ? raw.ToString("X8", CultureInfo.InvariantCulture) : (raw & 0xFFFFFF).ToString("X6", CultureInfo.InvariantCulture);

		public override string? GetClipboardValue()
		{
			var edit = Context.Row.Current;
			return edit?.Int == null ? null : "#" + HexText(unchecked((uint)edit.Int.Value));
		}

		public override bool TryPasteValue(string text)
		{
			var t = text.Trim().TrimStart('#');
			if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) t = t[2..];
			if (!uint.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v) || t.Length is not (6 or 8)) return false;

			uint packed = t.Length == 6 ? 0xFFu << 24 | v : v;
			if (!_hasAlpha) packed = 0xFFu << 24 | (packed & 0xFFFFFF);

			var before = Context.Snapshot();
			var edit = Context.Row.Current!;
			edit.Int = unchecked((long)packed);
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();
			Context.Commit(before);

			_a = _hasAlpha ? (byte)(packed >> 24) : (byte)255;
			RgbToHsv((byte)(packed >> 16), (byte)(packed >> 8), (byte)packed, out _h, out _s, out _v);
			RefreshAll();
			return true;
		}

		private static Color HsvToColor(double h, double s, double v)
		{
			HsvToRgb(h, s, v, out var r, out var g, out var b);
			return Color.FromRgb(r, g, b);
		}

		private static void HsvToRgb(double h, double s, double v, out byte r, out byte g, out byte b)
		{
			h = ((h % 360) + 360) % 360;
			double c = v * s;
			double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
			double m = v - c;
			(double rp, double gp, double bp) = h switch
			{
				< 60 => (c, x, 0.0),
				< 120 => (x, c, 0.0),
				< 180 => (0.0, c, x),
				< 240 => (0.0, x, c),
				< 300 => (x, 0.0, c),
				_ => (c, 0.0, x)
			};
			r = (byte)Math.Round((rp + m) * 255);
			g = (byte)Math.Round((gp + m) * 255);
			b = (byte)Math.Round((bp + m) * 255);
		}

		private static void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
		{
			double rf = r / 255.0, gf = g / 255.0, bf = b / 255.0;
			double max = Math.Max(rf, Math.Max(gf, bf));
			double min = Math.Min(rf, Math.Min(gf, bf));
			double delta = max - min;

			h = 0;
			if (delta > 0.00001)
			{
				if (max == rf) h = 60 * (((gf - bf) / delta) % 6);
				else if (max == gf) h = 60 * ((bf - rf) / delta + 2);
				else h = 60 * ((rf - gf) / delta + 4);
			}
			if (h < 0) h += 360;

			s = max <= 0 ? 0 : delta / max;
			v = max;
		}
	}
}
