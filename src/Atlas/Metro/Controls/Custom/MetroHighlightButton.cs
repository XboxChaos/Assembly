using System.Windows;
using System.Windows.Controls;

namespace Atlas.Metro.Controls.Custom
{
	public class MetroHighlightButton : Button
	{
		public static readonly DependencyProperty IsHighlightedProperty = DependencyProperty.Register(
			"IsHighlighted", typeof(bool), typeof(MetroHighlightButton), new PropertyMetadata(default(bool)));

		public static readonly DependencyProperty IsFlippedProperty = DependencyProperty.Register(
			"IsFlipped", typeof(bool), typeof(MetroHighlightButton), new PropertyMetadata(default(bool)));

		public bool IsHighlighted
		{
			get { return (bool)GetValue(IsHighlightedProperty); }
			set { SetValue(IsHighlightedProperty, value); }
		}

		public bool IsFlipped
		{
			get { return (bool)GetValue(IsFlippedProperty); }
			set { SetValue(IsFlippedProperty, value); }
		}
	}
}
