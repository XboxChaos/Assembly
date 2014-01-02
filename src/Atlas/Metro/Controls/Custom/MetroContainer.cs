using System;
using System.Windows;
using System.Windows.Controls;

namespace Atlas.Metro.Controls.Custom
{
	public class MetroContainer : ContentControl
	{
		public static DependencyProperty TitleProperty;

		static MetroContainer()
		{
			TitleProperty = DependencyProperty.Register("Title", typeof (String), typeof (MetroContainer));
		}

		public String Title
		{
			get { return (String)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}
	}
}
