using System;
using System.Windows;

namespace Atlas.Metro.Controls.Custom
{
	public class MetroDialogWindow : Window
	{
		public static readonly DependencyProperty WindowTitleProperty = DependencyProperty.Register("WindowTitle",
			typeof (String), typeof (MetroDialogWindow));

		

		public String WindowTitle
		{
			get { return (String)GetValue(WindowTitleProperty); }
			set { SetValue(WindowTitleProperty, value); }
		}
	}
}
