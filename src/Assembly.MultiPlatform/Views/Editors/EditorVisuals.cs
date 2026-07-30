using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Assembly.MultiPlatform.Views.Editors
{
	/// <summary>Small control-building helpers shared by the field editors, factored out of the
	/// old single BuildEditor method so every editor renders the same basic vocabulary (a labelled
	/// row, a validated text box, an inline error line) instead of each reinventing it.</summary>
	internal static class EditorVisuals
	{
		public static TextBox NumberBox(string initial, double width = 150) => new()
		{
			Text = initial,
			Width = width,
			HorizontalAlignment = HorizontalAlignment.Left,
			Classes = { "mono" }
		};

		public static TextBlock ErrorText() => new()
		{
			Classes = { "label" },
			FontSize = 10.5,
			TextWrapping = TextWrapping.Wrap,
			IsVisible = false,
			Foreground = Res.Brush("ErrorBrush", Brushes.OrangeRed)
		};

		public static TextBlock WarnText() => new()
		{
			Classes = { "label" },
			FontSize = 10.5,
			TextWrapping = TextWrapping.Wrap,
			IsVisible = false,
			Foreground = Res.Brush("WarnBrush", Brushes.Goldenrod)
		};

		/// <summary>A small caption line above a control, e.g. "X", "Min", "Text (max 32 chars)".</summary>
		public static StackPanel LabeledRow(string label, Control editor, Control? note = null)
		{
			var panel = new StackPanel { Spacing = 3 };
			panel.Children.Add(new TextBlock { Text = label, Classes = { "label" }, FontSize = 10.5 });
			panel.Children.Add(editor);
			if (note != null) panel.Children.Add(note);
			return panel;
		}

		public static Border Separator() => new()
		{
			Height = 1,
			Background = Res.Brush("SidebarHeaderSeperatorBrush", Brushes.Gray),
			Margin = new Thickness(0, 2, 0, 2)
		};

		public static TextBlock Hint(string text) => new()
		{
			Text = text,
			Classes = { "label" },
			FontSize = 10.5,
			TextWrapping = TextWrapping.Wrap
		};

		public static void MarkInvalid(TextBox box, TextBlock err, string message)
		{
			err.Text = message;
			err.IsVisible = true;
			box.Classes.Add("invalid");
		}

		public static void MarkValid(TextBox box, TextBlock err)
		{
			err.IsVisible = false;
			box.Classes.Remove("invalid");
		}
	}
}
