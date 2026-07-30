using System;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Assembly.MultiPlatform.Views.Editors
{
	/// <summary>
	///     A small read-only hex dump with an ASCII gutter, sixteen bytes per row: the offset, the
	///     hex bytes, and their printable-ASCII rendering (a dot for anything outside 0x20..0x7E) -
	///     what a research tool needs for a field nobody has decoded a real shape for yet, instead
	///     of the field table's own truncated space-separated hex string. Bounded to a sane number
	///     of rows behind its own scroll region so one huge blob cannot take over the whole sidebar.
	/// </summary>
	public sealed class HexView : Border
	{
		private const int MaxBytesShown = 4096;

		private readonly SelectableTextBlock _text;

		public HexView()
		{
			BorderBrush = Res.Brush("SidebarHeaderSeperatorBrush", Brushes.Gray);
			BorderThickness = new Thickness(1);
			MaxHeight = 260;

			_text = new SelectableTextBlock { Classes = { "mono" }, FontSize = 10.5, Padding = new Thickness(6) };
			Child = new ScrollViewer { Content = _text, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
		}

		public void SetBytes(byte[] bytes, long baseOffset = 0)
		{
			if (bytes.Length == 0)
			{
				_text.Text = "(no bytes)";
				return;
			}

			int shown = Math.Min(bytes.Length, MaxBytesShown);
			var sb = new StringBuilder();

			for (int row = 0; row < shown; row += 16)
			{
				int n = Math.Min(16, shown - row);
				sb.Append((baseOffset + row).ToString("X6")).Append("  ");

				for (int i = 0; i < 16; i++)
				{
					if (i < n) sb.Append(bytes[row + i].ToString("X2"));
					else sb.Append("  ");
					sb.Append(i == 7 ? "  " : " ");
				}

				sb.Append(' ');
				for (int i = 0; i < n; i++)
				{
					byte b = bytes[row + i];
					sb.Append(b is >= 0x20 and <= 0x7E ? (char)b : '.');
				}

				sb.Append('\n');
			}

			if (bytes.Length > shown)
				sb.Append($"... {bytes.Length - shown} more byte(s) not shown\n");

			_text.Text = sb.ToString();
		}

		/// <summary>The same text the view renders, for the "copy raw bytes" action - full fidelity even past what <see cref="MaxBytesShown"/> displays.</summary>
		public static string ToHexLine(byte[] bytes) => string.Join(" ", Array.ConvertAll(bytes, b => b.ToString("X2")));
	}
}
