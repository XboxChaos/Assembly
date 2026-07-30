using System.Text;
using Avalonia;
using Avalonia.Input;

namespace Assembly.Avalonia.Services
{
	/// <summary>
	///     Builds <see cref="KeyGesture" />s and their display labels off the platform's own
	///     "primary" modifier (<c>IPlatformSettings.HotkeyConfiguration.CommandModifiers</c> - Meta
	///     on macOS, Control everywhere else) instead of a hardcoded <c>KeyModifiers.Meta</c>, so a
	///     Linux/Windows build of this shell gets Ctrl-based shortcuts for free rather than dead
	///     Cmd-only bindings. Every new global shortcut this pass adds goes through here; existing
	///     Cmd-literal gestures elsewhere in this codebase (e.g. <c>PropertiesPanel</c>'s
	///     <c>KeyGesture.Parse("Cmd+Z")</c>) predate this and are out of this pass's file scope to
	///     change.
	/// </summary>
	public static class PlatformKeys
	{
		/// <summary>The platform's primary command modifier - Meta (⌘) on macOS, Control elsewhere.</summary>
		public static KeyModifiers CommandModifiers =>
			Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control;

		private static bool IsMac => CommandModifiers == KeyModifiers.Meta;

		/// <summary><paramref name="extra" /> is combined with the platform's own primary modifier.</summary>
		public static KeyGesture Gesture(Key key, KeyModifiers extra = KeyModifiers.None) =>
			new(key, CommandModifiers | extra);

		/// <summary>
		///     A short human label for <paramref name="key" />+<paramref name="extra" /> (plus the
		///     implicit primary modifier) - mac glyphs (⌘⇧⌥⌃) on macOS, matching the labels already
		///     hand-written elsewhere in this shell (MainWindow.axaml's empty-state CTAs), or plain
		///     "Ctrl+"/"Shift+"/"Alt+" text on other platforms.
		/// </summary>
		public static string Label(Key key, KeyModifiers extra = KeyModifiers.None)
		{
			var sb = new StringBuilder();
			if (IsMac)
			{
				if ((extra & KeyModifiers.Control) != 0) sb.Append('⌃');
				if ((extra & KeyModifiers.Alt) != 0) sb.Append('⌥');
				if ((extra & KeyModifiers.Shift) != 0) sb.Append('⇧');
				sb.Append('⌘');
			}
			else
			{
				if ((extra & KeyModifiers.Control) != 0) sb.Append("Ctrl+");
				if ((extra & KeyModifiers.Alt) != 0) sb.Append("Alt+");
				if ((extra & KeyModifiers.Shift) != 0) sb.Append("Shift+");
				sb.Append("Ctrl+");
			}

			sb.Append(KeyLabel(key));
			return sb.ToString();
		}

		private static string KeyLabel(Key key) => key switch
		{
			Key.OemOpenBrackets => "[",
			Key.OemCloseBrackets => "]",
			Key.OemBackslash => "\\",
			Key.OemComma => ",",
			Key.OemPeriod => ".",
			Key.OemQuestion => "/",
			Key.OemMinus => "-",
			Key.OemPlus => "=",
			Key.Escape => "Esc",
			Key.Enter => "↵",
			Key.D0 => "0", Key.D1 => "1", Key.D2 => "2", Key.D3 => "3", Key.D4 => "4",
			Key.D5 => "5", Key.D6 => "6", Key.D7 => "7", Key.D8 => "8", Key.D9 => "9",
			_ => key.ToString()
		};
	}
}
