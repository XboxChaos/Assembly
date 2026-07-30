using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Assembly.Avalonia.ViewModels;
using Assembly.Avalonia.Views;

namespace Assembly.Avalonia
{
	public partial class App : Application
	{
		public override void Initialize() => AvaloniaXamlLoader.Load(this);

		public override void OnFrameworkInitializationCompleted()
		{
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
