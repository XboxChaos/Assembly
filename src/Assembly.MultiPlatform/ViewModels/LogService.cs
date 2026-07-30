using System;
using System.Collections.ObjectModel;

namespace Assembly.MultiPlatform.ViewModels
{
	public enum LogLevel { Info, Warn, Error }

	/// <summary>One line in the console/output pane.</summary>
	public sealed class LogEntry
	{
		public DateTime Time { get; init; } = DateTime.Now;
		public LogLevel Level { get; init; }
		public string Message { get; init; } = "";
		public string TimeLabel => Time.ToString("HH:mm:ss");
		public string LevelLabel => Level switch
		{
			LogLevel.Warn => "WARN",
			LogLevel.Error => "ERROR",
			_ => "INFO"
		};
	}

	/// <summary>
	///     Backs the console/output pane at the bottom of the shell. Every mount, open, save
	///     and error in the app funnels through here — it is a real log of what the app did,
	///     not placeholder text.
	/// </summary>
	public sealed class LogService
	{
		public ObservableCollection<LogEntry> Entries { get; } = new();

		public void Info(string message) => Add(LogLevel.Info, message);
		public void Warn(string message) => Add(LogLevel.Warn, message);
		public void Error(string message) => Add(LogLevel.Error, message);

		private void Add(LogLevel level, string message)
		{
			Entries.Add(new LogEntry { Level = level, Message = message });
			// Keep the pane from growing unbounded across a long session.
			while (Entries.Count > 2000) Entries.RemoveAt(0);
		}

		public void Clear() => Entries.Clear();
	}
}
