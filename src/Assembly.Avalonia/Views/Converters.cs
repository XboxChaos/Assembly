using System;
using System.Globalization;
using Assembly.Avalonia.ViewModels;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Assembly.Avalonia.Views
{
	/// <summary>Green when true, red when false. Used for the engine-database indicator.</summary>
	public sealed class OkBrushConverter : IValueConverter
	{
		private static readonly IBrush Ok = new SolidColorBrush(Color.Parse("#FF6FCF6F"));
		private static readonly IBrush Bad = new SolidColorBrush(Color.Parse("#FFE05252"));

		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is true ? Ok : Bad;

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>Accent footer normally; amber when the active document has unsaved edits.</summary>
	public sealed class FooterBrushConverter : IValueConverter
	{
		private static readonly IBrush Normal = new SolidColorBrush(Color.Parse("#FF0079cb"));
		private static readonly IBrush Dirty = new SolidColorBrush(Color.Parse("#FFB8860B"));

		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is true ? Dirty : Normal;

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>Indents a field-table row's name label by its tag-block nesting depth.</summary>
	public sealed class DepthIndentConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> new Thickness((value as int? ?? 0) * 16, 0, 0, 0);

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

	/// <summary>Transparent normally, accent when true. Drives the per-row dirty-state strip in the field table.</summary>
	public sealed class DirtyBrushConverter : IValueConverter
	{
		private static readonly IBrush Dirty = new SolidColorBrush(Color.Parse("#FF0079cb"));

		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is true ? Dirty : Brushes.Transparent;

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>Colours a console line by its log level.</summary>
	public sealed class LogLevelBrushConverter : IValueConverter
	{
		private static readonly IBrush InfoBrush = new SolidColorBrush(Color.Parse("#FF989898"));
		private static readonly IBrush WarnBrush = new SolidColorBrush(Color.Parse("#FFE0B020"));
		private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#FFE05252"));

		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value switch
			{
				LogLevel.Warn => WarnBrush,
				LogLevel.Error => ErrorBrush,
				_ => InfoBrush
			};

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}
}
