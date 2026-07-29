using System;
using System.Globalization;
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

	/// <summary>Accent footer normally; red when a synthetic test fixture is loaded.</summary>
	public sealed class FooterBrushConverter : IValueConverter
	{
		private static readonly IBrush Normal = new SolidColorBrush(Color.Parse("#FF0079cb"));
		private static readonly IBrush Synthetic = new SolidColorBrush(Color.Parse("#FFE05252"));

		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is true ? Synthetic : Normal;

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}
}
