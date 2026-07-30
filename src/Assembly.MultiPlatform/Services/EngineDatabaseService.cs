using System;
using System.IO;
using System.Linq;
using System.Text;
using Blamite.Serialization;
using Blamite.Serialization.Settings;

namespace Assembly.MultiPlatform.Services
{
	/// <summary>
	///     Loads and owns Blamite's engine database for the lifetime of the process.
	/// </summary>
	public static class EngineDatabaseService
	{
		private static bool _initialized;

		public static EngineDatabase? Database { get; private set; }
		public static string? Error { get; private set; }
		public static int EngineCount { get; private set; }
		public static string? FormatsRoot { get; private set; }
		public static string? PluginsRoot { get; private set; }

		public static void Initialize()
		{
			if (_initialized) return;
			_initialized = true;

			// Blamite's EndianReader.ReadWin1252 needs the Windows-1252 code page, which
			// .NET (non-Framework) does not register by default. Without this, string
			// reads throw on macOS and Linux.
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			try
			{
				var baseDir = AppContext.BaseDirectory;

				// Blamite resolves the nested database paths inside Engines.xml against the
				// process current directory, with no rebasing. The WPF app does the same.
				Directory.SetCurrentDirectory(baseDir);

				var formats = Path.Combine(baseDir, "Formats");
				var enginesXml = Path.Combine(formats, "Engines.xml");
				if (!File.Exists(enginesXml))
				{
					Error = $"Engines.xml not found at {enginesXml}";
					return;
				}

				FormatsRoot = formats;
				var plugins = Path.Combine(baseDir, "Plugins");
				PluginsRoot = Directory.Exists(plugins) ? plugins : null;

				Database = XMLEngineDatabaseLoader.LoadDatabase(enginesXml);
				EngineCount = Database.Count();
			}
			catch (Exception ex)
			{
				Error = $"{ex.GetType().Name}: {ex.Message}";
				Database = null;
			}
		}
	}
}
