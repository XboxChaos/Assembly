using Avalonia;
using Avalonia.Media;

namespace Assembly.Avalonia.Views.Editors
{
	/// <summary>
	///     Small theme-resource lookups shared by the field editors. Looks resources up through
	///     <see cref="Application.Current" /> rather than a control's own <c>TryFindResource</c>,
	///     because these editors are frequently constructed and populated before they are attached
	///     to the visual tree (a control's resource lookup only walks ancestors it already has),
	///     and every brush/font this reaches lives in <c>Theme/MetroDark.axaml</c>, which is merged
	///     into the application's own resources regardless of where in the tree something asks.
	/// </summary>
	internal static class Res
	{
		public static IBrush Brush(string key, IBrush fallback)
		{
			var app = Application.Current;
			if (app != null && app.TryGetResource(key, app.ActualThemeVariant, out var value) && value is IBrush b)
				return b;
			return fallback;
		}

		public static FontFamily? Font(string key)
		{
			var app = Application.Current;
			if (app != null && app.TryGetResource(key, app.ActualThemeVariant, out var value) && value is FontFamily f)
				return f;
			return null;
		}
	}
}
