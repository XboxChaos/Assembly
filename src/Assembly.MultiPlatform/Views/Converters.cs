using System;
using System.Globalization;
using System.Linq;
using Assembly.MultiPlatform.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Assembly.MultiPlatform.Views
{
	/// <summary>
	///     Shared helper for converters that colour a value by looking up one of
	///     <c>MetroDark.axaml</c>'s theme-dictionary keys, so the colour tracks whichever of
	///     "Dark"/"Light" is active instead of a colour frozen in a <c>static readonly</c>
	///     field at converter-construction time. Falls back to the given literal only if the
	///     application (or the resource) genuinely cannot be found - e.g. design-time preview -
	///     so a converter never throws for a missing brush.
	/// </summary>
	internal static class ThemeBrush
	{
		public static IBrush Resolve(string key, string fallbackHex)
		{
			var app = Application.Current;
			if (app != null && app.TryGetResource(key, app.ActualThemeVariant, out var value) && value is IBrush brush)
				return brush;
			return new SolidColorBrush(Color.Parse(fallbackHex));
		}
	}

	/// <summary>Green when true, red when false. Used for the engine-database indicator.</summary>
	public sealed class OkBrushConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is true ? ThemeBrush.Resolve("SuccessBrush", "#FF6FCF6F") : ThemeBrush.Resolve("ErrorBrush", "#FFE05252");

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	///     The status bar's own background: quiet chrome (the same neutral surface every other
	///     panel header in this shell uses) normally, the shared dirty colour only when the active
	///     document actually has something to report. An earlier version filled this bar with the
	///     solid accent blue at all times - "a coloured strip bolted on" rather than chrome the
	///     accent could ever stand out against, since by the time a reader reaches the bottom of
	///     the window they have already seen that exact blue on the header's top edge, the active
	///     tab underline and every accent button. Reserving it for the one state that is actually
	///     worth a signal (unsaved changes) is the same "one vivid accent, used sparingly" principle
	///     the rest of this pass follows - see <see cref="FooterForegroundBrushConverter"/> for the
	///     matching text-colour swap this needs (white reads on the dirty gold in both themes; the
	///     neutral surface needs the ordinary primary text colour instead, not a fixed white).
	/// </summary>
	public sealed class FooterBrushConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is true ? ThemeBrush.Resolve("DirtyStateBrush", "#FFCE8B3C") : ThemeBrush.Resolve("SidebarHeaderBrush", "#FF2d2d30");

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>Text colour to match <see cref="FooterBrushConverter"/>'s background: white on the
	/// dirty-gold fill (dark enough in both themes for white to clear contrast), the ordinary
	/// primary text colour on the neutral surface (where a hardcoded white would fail outright in
	/// Light).</summary>
	public sealed class FooterForegroundBrushConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is true ? Brushes.White : ThemeBrush.Resolve("TextBrushPrimary", "#FFFFFFFF");

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>Right-pointing triangle collapsed, down-pointing triangle expanded - same shape as
	/// the tag tree's expander (see MetroStyles.axaml), reused here for tag block rows.</summary>
	public sealed class ExpanderGlyphConverter : IValueConverter
	{
		private static readonly Geometry Collapsed = Geometry.Parse("M0,0 L6,3 L0,6 Z");
		private static readonly Geometry Expanded = Geometry.Parse("M0,0 L6,0 L3,6 Z");

		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is true ? Expanded : Collapsed;

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>Transparent normally, the shared dirty colour when true. Drives the per-row dirty-state
	/// strip in the field table (and, via the same colour, the tab dirty dot and footer tint - see
	/// MetroDark.axaml's header for why "dirty" got its own colour instead of reusing the accent).</summary>
	public sealed class DirtyBrushConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is true ? ThemeBrush.Resolve("DirtyStateBrush", "#FFCE8B3C") : Brushes.Transparent;

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>Colours a console line's level text by its log level.</summary>
	public sealed class LogLevelBrushConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value switch
			{
				LogLevel.Warn => ThemeBrush.Resolve("WarnBrush", "#FFE0B020"),
				LogLevel.Error => ThemeBrush.Resolve("ErrorBrush", "#FFE05252"),
				_ => ThemeBrush.Resolve("TextBrushSecondary", "#FF989898")
			};

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>Tinted chip background behind the console level badge - the muted counterpart to
	/// <see cref="LogLevelBrushConverter"/>'s foreground, same idea as the field-table type badges.</summary>
	public sealed class LogLevelBackgroundBrushConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value switch
			{
				LogLevel.Warn => ThemeBrush.Resolve("WarnBrushMuted", "#33E0B020"),
				LogLevel.Error => ThemeBrush.Resolve("ErrorBrushMuted", "#33E05252"),
				_ => Brushes.Transparent
			};

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	///     Categorises a field-table/tag-tree "kind" label (e.g. "block", "array[3]", "struct",
	///     "enum8", "Enum", "TagBlock[40]", "Flags", "StringId") into one of four badge
	///     categories by substring match, since classic plugin-XML kind labels (the
	///     <c>MetaFieldKind</c> enum's own <c>ToString()</c>) and Campaign Evolved's
	///     schema-declared type names (see <c>FifthGenFieldDefinition.TypeName</c>) are two
	///     unrelated vocabularies with no shared enum to switch on - substring matching is the
	///     only thing that works for both without this file depending on either schema's types.
	///     Deliberately only badges the kinds worth visually flagging while scanning a few
	///     hundred rows (containers, choice types, text); plain numeric/vector kinds are left
	///     unbadged; see MetroStyles.axaml.
	/// </summary>
	internal static class TypeBadgeCategory
	{
		public static string? Categorize(string? kindLabel)
		{
			if (string.IsNullOrEmpty(kindLabel)) return null;
			var k = kindLabel;
			if (Contains(k, "block") || Contains(k, "array") || Contains(k, "struct")) return "Container";
			if (Contains(k, "enum")) return "Enum";
			if (Contains(k, "flag")) return "Flags";
			if (Contains(k, "string") || Contains(k, "ascii") || Contains(k, "utf16") || Contains(k, "char")) return "String";
			return null;
		}

		private static bool Contains(string haystack, string needle)
			=> haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	/// <summary>Foreground for the field-table TYPE column label; see <see cref="TypeBadgeCategory"/>.
	/// Transparent (inherits the ambient secondary text colour) for uncategorised kinds, so numeric
	/// fields keep reading as plain text rather than getting a not-quite-badge tint.
	///
	/// Deliberately text-only, with no background chip: an earlier version painted a tinted
	/// rectangle behind this text too (see git history), which against a few hundred rows read as
	/// a wall of candy-coloured pills rather than a scannable type column - exactly what a reader
	/// does not want from a "structurally significant kinds only" signal. Colour alone, at a
	/// slightly deeper/less pastel step than the original chip-era palette (MetroDark.axaml's
	/// Badge*Foreground keys), keeps the categories distinguishable while reading as quiet syntax-
	/// style colouring instead of a chip.</summary>
	public sealed class TypeBadgeForegroundConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> TypeBadgeCategory.Categorize(value as string) switch
			{
				"Container" => ThemeBrush.Resolve("BadgeContainerForeground", "#FFA78BFA"),
				"Enum" => ThemeBrush.Resolve("BadgeEnumForeground", "#FF22D3EE"),
				"Flags" => ThemeBrush.Resolve("BadgeFlagsForeground", "#FFE879F9"),
				"String" => ThemeBrush.Resolve("BadgeStringForeground", "#FF34D399"),
				_ => ThemeBrush.Resolve("TextBrushSecondary", "#FF989898")
			};

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	///     Turns a field-table row's nesting depth into <c>depth</c> placeholder items, so the
	///     row's DataTemplate can lay out one real indent-guide element per ancestor level with an
	///     <c>ItemsControl</c> (see MainWindow.axaml's FieldList row template) instead of painting
	///     lines into the row's own Background.
	///
	///     This replaces an earlier approach (paint the guides as a <c>DrawingBrush</c> Background
	///     with hand-computed pixel offsets) that turned out not to render at all in practice: a
	///     ListBoxItem's Background is sized and clipped to that one row's own bounds, so even a
	///     geometry that draws correctly in isolation only ever contributes a few pixels per row,
	///     with nothing to guarantee neighbouring rows' copies land on the same physical pixel
	///     column once anti-aliasing and per-row subpixel snapping are in play - confirmed by
	///     rendering a real expanded block and finding no visible line at any zoom level, not even
	///     a misaligned or flickering one. Real elements in the template, each simply as tall as
	///     its own row, do not have this problem: there is nothing to keep aligned across rows
	///     because each guide is drawn by the same fixed-width column in every row.
	/// </summary>
	public sealed class DepthRangeConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			int depth = value as int? ?? 0;
			return depth <= 0 ? Array.Empty<int>() : Enumerable.Range(0, depth).ToArray();
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	///     Strips a leading "* " dirty marker from a tab title. <c>TagDocumentViewModel.TabTitle</c>
	///     (owned by ViewModels/, not this pass) prepends "* " so any consumer gets a
	///     dirty-aware title even without extra UI; the tab strip now shows a dedicated dirty dot
	///     instead (see MainWindow.axaml), so showing both would be redundant.
	/// </summary>
	public sealed class StripDirtyMarkerConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			var s = value as string ?? "";
			return s.StartsWith("* ", StringComparison.Ordinal) ? s[2..] : s;
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	///     Trims the trailing " - Assembly" that <c>MainViewModel.WindowTitle</c> (ViewModels/, not
	///     this pass) appends, for the header band's small session-summary line underneath the big
	///     "ASSEMBLY" wordmark (MainWindow.axaml). WindowTitle also drives the native window's own
	///     title bar, where "N sources, N tags - Assembly" repeating the app name at the end is
	///     normal and expected (every macOS window title does this); reusing that exact string a
	///     second time 40px below a wordmark that already says "ASSEMBLY" is what the brief calls
	///     out by name - two readings of the same word in the same glance. When WindowTitle is bare
	///     "Assembly" (nothing mounted yet, see MainViewModel.CloseAll), there is no summary left
	///     to show once the suffix is gone, so this returns "" rather than leaving the un-trimmed
	///     word behind - a blank second line costs nothing next to a wordmark this large.
	/// </summary>
	public sealed class HeaderSubtitleConverter : IValueConverter
	{
		private const string Suffix = " - Assembly";

		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			var s = value as string ?? "";
			return s.EndsWith(Suffix, StringComparison.Ordinal) ? s[..^Suffix.Length] : "";
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}
}
