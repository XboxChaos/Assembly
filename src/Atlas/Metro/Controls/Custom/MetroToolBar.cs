using System.Windows;
using System.Windows.Controls;

namespace Atlas.Metro.Controls.Custom
{
	public class MetroToolBar : ToolBar
	{
		public static readonly DependencyProperty DropDownVisibilityProperty = DependencyProperty.Register(
			"DropDownVisibility", typeof(Visibility), typeof(MetroToolBar));

		public Visibility DropDownVisibility
		{
			get { return (Visibility)GetValue(DropDownVisibilityProperty); }
			set { SetValue(DropDownVisibilityProperty, value); }
		}
	}
}
