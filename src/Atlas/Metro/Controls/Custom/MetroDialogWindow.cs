using System;
using System.Windows;
using System.Windows.Controls.Primitives;

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

		#region Resizing

		public void ResizeCornerThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
		{
			var yAdjust = Height + e.VerticalChange;
			var xAdjust = Width + e.HorizontalChange;

			if (xAdjust > MinWidth)
				Width = xAdjust;
			if (yAdjust > MinHeight)
				Height = yAdjust;
		}

		public void ResizeRightThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
		{
			var xAdjust = Width + e.HorizontalChange;

			if (xAdjust > MinWidth)
				Width = xAdjust;
		}

		public void ResizeBottomThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
		{
			var yAdjust = Height + e.VerticalChange;

			if (yAdjust > MinHeight)
				Height = yAdjust;
		}

		#endregion
	}
}
