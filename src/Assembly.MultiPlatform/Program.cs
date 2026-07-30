using System;
using System.Collections.Generic;
using Avalonia;

namespace Assembly.MultiPlatform
{
	internal static class Program
	{
		/// <summary>
		///     The first arguments that mean "do not open a window".
		/// </summary>
		/// <remarks>
		///     A set rather than a chain of comparisons, because this list grows every time someone
		///     adds a headless mode. The chain form has already caused one silent bug - a mode was
		///     added to <see cref="HeadlessProbe" /> but not here, so it fell through and launched
		///     the real window instead of running - and two merge conflicts between agents adding
		///     modes side by side.
		/// </remarks>
		private static readonly HashSet<string> HeadlessModes = new(StringComparer.Ordinal)
		{
			"--headless",
			"--edit-test",
			"--perf-test",
			"--ce-fields",
			"--iostore-inspect",
			"--fifthgen-roundtrip",
			"--ce-unpack",
			"--ce-repack",
			"--ce-mutate-tag-file",
			"--palette-bench"
		};

		[STAThread]
		public static int Main(string[] args)
		{
			if (args.Length > 0 && HeadlessModes.Contains(args[0]))
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
