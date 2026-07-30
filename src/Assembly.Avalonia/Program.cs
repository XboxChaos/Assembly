using Avalonia;

namespace Assembly.Avalonia
{
	internal static class Program
	{
		[System.STAThread]
		public static int Main(string[] args)
		{
			if (args.Length > 0 && (args[0] == "--headless" || args[0] == "--edit-test" || args[0] == "--perf-test" || args[0] == "--ce-fields"))
				return HeadlessProbe.Run(args);

			return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
		}

		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.WithInterFont()
				.LogToTrace();
	}
}
