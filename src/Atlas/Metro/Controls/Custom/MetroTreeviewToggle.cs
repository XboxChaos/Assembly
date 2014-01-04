using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Atlas.Metro.Controls.Custom
{
	public class MetroTreeViewToggle : ToggleButton
	{
		public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
			"IsActive", typeof(Boolean), typeof(MetroTreeViewToggle), new PropertyMetadata(default(Boolean)));

		public Boolean IsActive
		{
			get { return (Boolean)GetValue(IsActiveProperty); }
			set { SetValue(IsActiveProperty, value); }
		}
	}
}
