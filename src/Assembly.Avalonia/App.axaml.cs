using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Assembly.Avalonia.ViewModels;
using Assembly.Avalonia.Views;

namespace Assembly.Avalonia
{
	public partial class App : Application
	{
		public override void Initialize() => AvaloniaXamlLoader.Load(this);

		public override void OnFrameworkInitializationCompleted()
		{
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				var vm = new MainViewModel();
				var window = new MainWindow { DataContext = vm };
				desktop.MainWindow = window;

				// Open a file passed on the command line, if any.
				var args = desktop.Args;
				if (args is { Length: > 0 } && System.IO.File.Exists(args[0]))
					window.Opened += async (_, _) => await vm.OpenAsync(args[0]);
			}

			base.OnFrameworkInitializationCompleted();
		}
	}
}
