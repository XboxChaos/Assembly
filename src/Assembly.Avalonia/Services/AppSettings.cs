using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Assembly.Avalonia.Services
{
	/// <summary>
	///     The whole of this app's persisted, user-facing preferences - today just
	///     <see cref="Density" />. Nothing else in Assembly.Avalonia persists anything (no prior
	///     settings file anywhere in the project, no framework dependency for one), so this is
	///     deliberately the smallest honest mechanism for it: one hand-written JSON file, in the
	///     platform's real app-data location, read once at startup and written on every change -
	///     not a settings framework, since one property does not justify one.
	/// </summary>
	public sealed class AppSettings : INotifyPropertyChanged
	{
		private const string FileName = "settings.json";

		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			WriteIndented = true,
			Converters = { new JsonStringEnumConverter() }
		};

		public static AppSettings Instance { get; } = Load();

		private DensityLevel _density;

		private AppSettings(DensityLevel density)
		{
			_density = density;
		}

		/// <summary>
		///     The persisted density choice. Setting this both applies it live
		///     (<see cref="DisplayDensity.Apply" />, which reflows every open window immediately -
		///     see that method's remarks for exactly how far "every" reaches) and writes it to
		///     disk. This is the one public entry point density switching should go through - a
		///     future command-palette entry (Services/CommandRegistry.cs, owned by a different
		///     pass this wave) should call this setter directly, e.g. one command per
		///     <see cref="DisplayDensity.Levels" /> entry, or a single "cycle density" command
		///     calling <see cref="CycleDensity" />.
		/// </summary>
		public DensityLevel Density
		{
			get => _density;
			set
			{
				if (_density == value) return;
				_density = value;
				DisplayDensity.Apply(value);
				Save();
				OnPropertyChanged();
			}
		}

		/// <summary>Steps to the next density level, wrapping Comfortable back to Compact - the
		/// single action a "cycle density" command needs.</summary>
		public void CycleDensity() => Density = DisplayDensity.Next(Density);

		public event PropertyChangedEventHandler? PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string? name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		private static AppSettings Load()
		{
			var density = DensityLevel.Default;
			try
			{
				var path = SettingsFilePath();
				if (File.Exists(path))
				{
					var json = File.ReadAllText(path);
					var data = JsonSerializer.Deserialize<SettingsData>(json, JsonOptions);
					if (data != null) density = data.Density;
				}
			}
			catch
			{
				// A missing, corrupt or unreadable settings file is not worth failing startup
				// over - fall back to the Default tier, exactly as a genuine first run would.
			}

			var settings = new AppSettings(density);
			// Seed the live resources before any window is created, so the very first frame
			// already matches the saved choice instead of flashing Default and then jumping.
			DisplayDensity.Apply(density);
			return settings;
		}

		private void Save()
		{
			try
			{
				var path = SettingsFilePath();
				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				var json = JsonSerializer.Serialize(new SettingsData { Density = _density }, JsonOptions);
				File.WriteAllText(path, json);
			}
			catch
			{
				// Best-effort: a failed write (read-only app-data volume, sandbox denial) should
				// not crash the app over a preferences file - the in-memory choice still applies
				// for the rest of this run via the DisplayDensity.Apply call already made above.
			}
		}

		/// <summary>
		///     The platform's real app-data directory - not .NET's cross-platform
		///     <see cref="Environment.SpecialFolder.ApplicationData" /> default, which on macOS
		///     resolves to <c>~/.config</c> (a .NET/XDG convention Finder and every other Mac app
		///     ignores), not <c>~/Library/Application Support</c>. This app is macOS-first (see
		///     BRIEF.md), so this asks the OS directly rather than trusting that default.
		/// </summary>
		private static string SettingsFilePath()
		{
			string dir;
			if (OperatingSystem.IsMacOS())
			{
				var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				dir = Path.Combine(home, "Library", "Application Support", "Assembly");
			}
			else
			{
				dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Assembly");
			}

			return Path.Combine(dir, FileName);
		}

		/// <summary>The on-disk shape of settings.json. Internal: callers go through <see cref="AppSettings" />, never this DTO directly.</summary>
		private sealed class SettingsData
		{
			public DensityLevel Density { get; set; } = DensityLevel.Default;
		}
	}
}
