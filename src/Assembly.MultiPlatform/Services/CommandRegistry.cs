using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;

namespace Assembly.MultiPlatform.Services
{
	/// <summary>
	///     One entry in the command palette's ">" (command) mode - a small, self-contained record
	///     rather than a hardcoded switch in the view, per the command-palette brief: the palette
	///     is meant to be a genuine map of what the shell can do, not a shadow of whatever handlers
	///     happen to already exist. <see cref="Enabled" /> is re-evaluated every time the palette
	///     is opened or its query changes, so a command whose availability depends on live state
	///     (Save with nothing open, Go Back with empty history) tracks that state rather than being
	///     computed once at registration time.
	/// </summary>
	public sealed class CommandDefinition
	{
		public CommandDefinition(string id, string title, string category, Action execute,
			Func<bool>? enabled = null, KeyGesture? gesture = null, string? shortcutLabel = null)
		{
			Id = id;
			Title = title;
			Category = category;
			Execute = execute;
			Enabled = enabled ?? (() => true);
			Gesture = gesture;
			ShortcutLabel = shortcutLabel;
		}

		public string Id { get; }
		public string Title { get; }
		public string Category { get; }
		public Action Execute { get; }
		public Func<bool> Enabled { get; }

		/// <summary>Null for a command that is only reachable through the palette (no bound key).</summary>
		public KeyGesture? Gesture { get; }

		/// <summary>Display label for the shortcut column - "the palette teaches the shortcuts
		/// rather than replacing them" only works if every bound command shows one.</summary>
		public string? ShortcutLabel { get; }
	}

	/// <summary>
	///     Holds every command the shell exposes and tracks which ones were recently invoked, for
	///     the palette's "recently-used entries float" ranking rule. Populated once, in
	///     <c>MainWindow</c>'s constructor (the one place that already owns every action a command
	///     could invoke - open/save/close/toggle/navigate), and consulted from both the command
	///     palette and the global key-down handler, so a shortcut and its palette entry can never
	///     drift apart the way two independently hand-maintained lists could.
	/// </summary>
	public sealed class CommandRegistry
	{
		private readonly List<CommandDefinition> _commands = new();
		private readonly Dictionary<string, string> _lowerTitleCache = new();
		private readonly RecencyTracker<string> _recent = new(capacity: 12);

		public IReadOnlyList<CommandDefinition> Commands => _commands;

		public void Register(CommandDefinition command)
		{
			_commands.Add(command);
			_lowerTitleCache[command.Id] = (command.Category + " " + command.Title).ToLowerInvariant();
		}

		/// <summary>
		///     Runs a command by id and records it as recently used - the one path every trigger
		///     (palette selection, global shortcut, toolbar/menu click) is routed through, so "was
		///     this used recently" reflects real usage regardless of how it was invoked.
		/// </summary>
		public bool Invoke(string id)
		{
			var cmd = _commands.FirstOrDefault(c => c.Id == id);
			if (cmd == null || !cmd.Enabled()) return false;
			cmd.Execute();
			_recent.Touch(id);
			return true;
		}

		/// <summary>Finds and matches <paramref name="e" /> against every command with a bound
		/// gesture, for the window-level global key handler. Disabled commands are skipped rather
		/// than swallowing the keystroke - a disabled Save shortcut should do nothing, not eat the
		/// key event some other handler might otherwise want.</summary>
		public bool TryInvokeForGesture(KeyEventArgs e)
		{
			foreach (var cmd in _commands)
			{
				if (cmd.Gesture == null || !cmd.Gesture.Matches(e)) continue;
				if (!cmd.Enabled()) return false;
				cmd.Execute();
				_recent.Touch(cmd.Id);
				return true;
			}
			return false;
		}

		/// <summary>
		///     Ranks every command against <paramref name="query" /> (empty = every command,
		///     recency-ordered first) and returns the top <paramref name="max" />. Disabled
		///     commands are included - "visible but clearly unavailable rather than absent" - the
		///     view decides how to render <see cref="CommandDefinition.Enabled" /> == false.
		/// </summary>
		public List<CommandDefinition> Search(string query, int max)
		{
			query = query.Trim();

			if (query.Length == 0)
			{
				// Empty query: recently-used first (most-recent first), then declaration order -
				// declaration order already roughly groups by category (see MainWindow's
				// InitializeCommands), which is a reasonable default browse order for "what can I do".
				return _commands
					.OrderByDescending(c => _recent.RankOf(c.Id) is var r && r >= 0 ? 1000 - r : -1)
					.Take(max)
					.ToList();
			}

			var lowerQuery = query.ToLowerInvariant();
			var scored = new List<(CommandDefinition Cmd, int Score)>();
			foreach (var cmd in _commands)
			{
				var score = FuzzyMatch.Score(_lowerTitleCache[cmd.Id], lowerQuery);
				if (score == null) continue;

				int recencyRank = _recent.RankOf(cmd.Id);
				if (recencyRank >= 0) score += 40 - recencyRank * 2;
				if (!cmd.Enabled()) score -= 500; // still shown, but never crowds out a usable command

				scored.Add((cmd, score.Value));
			}

			scored.Sort((a, b) => b.Score.CompareTo(a.Score));
			return scored.Take(max).Select(s => s.Cmd).ToList();
		}
	}
}
