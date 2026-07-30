using System;
using System.Globalization;
using Assembly.Avalonia.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Assembly.Avalonia.Views
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

	/// <summary>Accent footer normally; the shared dirty colour when the active document has unsaved edits.</summary>
	public sealed class FooterBrushConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is true ? ThemeBrush.Resolve("DirtyStateBrush", "#FFCE8B3C") : ThemeBrush.Resolve("ExtryzeAccentBrush", "#FF0079cb");

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>Indents a field-table row's name label by its tag-block nesting depth.</summary>
	public sealed class DepthIndentConverter : IValueConverter
	{
		/// <summary>Pixels per nesting level. Shared with <see cref="IndentGuideBrushConverter"/>, which
		/// draws its guide lines at the same 16px cadence so the lines land under the text they indent.</summary>
		public const double UnitWidth = 16;

		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> new Thickness((value as int? ?? 0) * UnitWidth, 0, 0, 0);

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

	/// <summary>Foreground for the field-table TYPE column badge; see <see cref="TypeBadgeCategory"/>.
	/// Transparent (inherits the ambient secondary text colour) for uncategorised kinds, so numeric
	/// fields keep reading as plain text rather than getting a not-quite-badge tint.</summary>
	public sealed class TypeBadgeForegroundConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> TypeBadgeCategory.Categorize(value as string) switch
			{
				"Container" => ThemeBrush.Resolve("BadgeContainerForeground", "#FFDDD6FE"),
				"Enum" => ThemeBrush.Resolve("BadgeEnumForeground", "#FFA5F3FC"),
				"Flags" => ThemeBrush.Resolve("BadgeFlagsForeground", "#FFF5D0FE"),
				"String" => ThemeBrush.Resolve("BadgeStringForeground", "#FFA7F3D0"),
				_ => ThemeBrush.Resolve("TextBrushSecondary", "#FF989898")
			};

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>Background chip for the field-table TYPE column badge; see <see cref="TypeBadgeCategory"/>.</summary>
	public sealed class TypeBadgeBackgroundConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> TypeBadgeCategory.Categorize(value as string) switch
			{
				"Container" => ThemeBrush.Resolve("BadgeContainerBackground", "#33A78BFA"),
				"Enum" => ThemeBrush.Resolve("BadgeEnumBackground", "#3322D3EE"),
				"Flags" => ThemeBrush.Resolve("BadgeFlagsBackground", "#33E879F9"),
				"String" => ThemeBrush.Resolve("BadgeStringBackground", "#3334D399"),
				_ => Brushes.Transparent
			};

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	///     Draws the field table's nesting indent guides as the row's own background: thin
	///     vertical hairlines at each 16px indent level (see <see cref="DepthIndentConverter"/>),
	///     starting past the row's fixed-width offset/type columns. This is a background brush
	///     rather than extra visual elements deliberately - the field-table row template lives in
	///     the row layout owned by the virtualization pass (see Theme/MetroStyles.axaml's header),
	///     so a container-level Background (set via a ListBoxItem Style selector, not the
	///     DataTemplate) is the only lever available here that doesn't touch that template. It is
	///     drawn tall enough (200px) to cover any realistic row height and clipped to the actual
	///     row bounds by the ListBoxItem's own render bounds, and it is overridden outright by the
	///     hover/selected background setters in MetroStyles.axaml, so guides only show in the
	///     resting state - exactly where they're useful (selection/hover already communicate depth
	///     well enough via their own highlight).
	///
	///     <see cref="ColumnStartX"/> hardcodes the field-table's current fixed column widths
	///     (dirty strip 4 + expander 26 + offset 90 + kind 120 = 240px before the name column
	///     begins). If the virtualization pass changes those widths this will drift out of
	///     alignment with the indent margin - it is cosmetic drift, not a functional break, but
	///     worth a look if the guide lines stop lining up under the indented text.
	/// </summary>
	public sealed class IndentGuideBrushConverter : IValueConverter
	{
		private const double ColumnStartX = 240;
		private const double UnitWidth = DepthIndentConverter.UnitWidth;
		private const double GuideHeight = 200;

		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			int depth = value as int? ?? 0;
			if (depth <= 0) return Brushes.Transparent;

			var pen = ThemeBrush.Resolve("SidebarHeaderSeperatorBrush", "#FF46464a");
			var group = new GeometryGroup { FillRule = FillRule.NonZero };
			for (int i = 0; i < depth; i++)
			{
				double x = ColumnStartX + UnitWidth * i + UnitWidth / 2.0;
				group.Children.Add(new RectangleGeometry(new Rect(x, 0, 1, GuideHeight)));
			}

			// DestinationRect is left at its default (relative 0,0,1,1 = "cover the whole
			// element"); with Stretch=None the drawing still renders at its own natural
			// (absolute-pixel) size anchored top-left within that region, rather than being
			// scaled to fill it - which is what makes the line x-coordinates above behave as
			// real pixel offsets instead of fractions of the row's width.
			return new DrawingBrush
			{
				Drawing = new GeometryDrawing { Brush = pen, Geometry = group },
				Stretch = Stretch.None,
				TileMode = TileMode.None,
				AlignmentX = AlignmentX.Left,
				AlignmentY = AlignmentY.Top
			};
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
}
