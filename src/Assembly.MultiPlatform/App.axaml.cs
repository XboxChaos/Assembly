using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Assembly.MultiPlatform.Services;
using Assembly.MultiPlatform.ViewModels;
using Assembly.MultiPlatform.Views;

namespace Assembly.MultiPlatform
{
	public partial class App : Application
	{
		public override void Initialize() => AvaloniaXamlLoader.Load(this);

		public override void OnFrameworkInitializationCompleted()
		{
			// Touching AppSettings.Instance runs its static Load() (Services/AppSettings.cs),
			// which reads settings.json (if any) and immediately calls DisplayDensity.Apply for
			// the saved (or Default, on a first run) density - before MainWindow exists, so the
			// very first frame already reflects it instead of flashing Default and then jumping.
			_ = AppSettings.Instance;

			// Dev/QA override, same idea as ASM_THEME below: forces a density for one run without
			// touching the user's saved preference (DisplayDensity.Apply, not
			// AppSettings.Instance.Density - the latter would persist the override to disk).
			// Lets a screenshot harness produce all three tiers without a settings round-trip.
			var densityOverride = Environment.GetEnvironmentVariable("ASM_DENSITY");
			if (!string.IsNullOrEmpty(densityOverride) &&
			    Enum.TryParse<DensityLevel>(densityOverride, ignoreCase: true, out var densityLevel))
				DisplayDensity.Apply(densityLevel);

			// Dev/QA override: RequestedThemeVariant is "Default" (App.axaml) so the app follows
			// the OS light/dark setting, which is the correct default but makes the *other*
			// variant awkward to actually look at without changing macOS System Settings and
			// restarting. ASM_THEME=Light|Dark forces one, for screenshotting both without that
			// round-trip; anything else (including unset) leaves the system-follow default alone.
			var themeOverride = Environment.GetEnvironmentVariable("ASM_THEME");
			RequestedThemeVariant = themeOverride switch
			{
				"Light" => ThemeVariant.Light,
				"Dark" => ThemeVariant.Dark,
				_ => RequestedThemeVariant
			};

			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				var vm = new MainViewModel();
				var window = new MainWindow { DataContext = vm };
				desktop.MainWindow = window;

				// Open a file passed on the command line, if any.
				var args = desktop.Args;
				if (args is { Length: > 0 } && System.IO.File.Exists(args[0]))
					window.Opened += async (_, _) => await vm.OpenFileAsync(args[0]);
			}

			base.OnFrameworkInitializationCompleted();
		}
	}
}
