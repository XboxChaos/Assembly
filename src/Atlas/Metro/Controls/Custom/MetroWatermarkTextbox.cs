using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Atlas.Metro.Controls.Custom
{
	public class MetroWatermarkTextbox : TextBox
	{
		public static DependencyProperty WatermarkProperty;
		public static DependencyProperty WatermarkForegroundProperty;
		public static DependencyProperty SearchIconVisibilityProperty;

		/// <summary>
		/// 
		/// </summary>
		static MetroWatermarkTextbox()
		{
			WatermarkProperty = DependencyProperty.Register("Watermark", typeof(String), typeof(MetroWatermarkTextbox));
			WatermarkForegroundProperty = DependencyProperty.Register("WatermarkForeground", typeof(SolidColorBrush), typeof(MetroWatermarkTextbox));
			SearchIconVisibilityProperty = DependencyProperty.Register("SearchIconVisibility", typeof(Visibility), typeof(MetroWatermarkTextbox));
		}

		/// <summary>
		/// 
		/// </summary>
		public MetroWatermarkTextbox()
		{
			DefaultStyleKey = typeof(MetroWatermarkTextbox);
			TextChanged += TextBoxTextChanged;
			GotFocus += OnGotFocus;
			LostFocus += OnLostFocus;
		}

		#region Region

		/// <summary>
		/// 
		/// </summary>
		public String Watermark
		{
			get { return (String)GetValue(WatermarkProperty); }
			set { SetValue(WatermarkProperty, value); }
		}

		/// <summary>
		/// 
		/// </summary>
		public SolidColorBrush WatermarkForeground
		{
			get { return (SolidColorBrush)GetValue(WatermarkForegroundProperty); }
			set { SetValue(WatermarkForegroundProperty, value); }
		}

		/// <summary>
		/// 
		/// </summary>
		public Visibility SearchIconVisibility
		{
			get { return (Visibility)GetValue(SearchIconVisibilityProperty); }
			set { SetValue(SearchIconVisibilityProperty, value); }
		}

		#endregion

		#region Base Class Overrides

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			//we need to set the initial state of the watermark
			GoToWatermarkVisualState();
		}

		#endregion

		#region Event Handlers

		private void TextBoxTextChanged(object sender, TextChangedEventArgs e)
		{
			GoToWatermarkVisualState();
		}

		private void OnGotFocus(object sender, RoutedEventArgs routedEventArgs)
		{
			GoToWatermarkVisualState();
		}

		private void OnLostFocus(object sender, RoutedEventArgs routedEventArgs)
		{
			GoToWatermarkVisualState();
		}

		#endregion

		#region Methods

		private void GoToWatermarkVisualState()
		{
			var hasFocus = IsFocused;
			//if our text is empty and our control doesn't have focus then show the watermark
			//otherwise the control eirther has text or has focus which in either case we need to hide the watermark
			if (String.IsNullOrEmpty(Text) && !hasFocus)
				GoToVisualState("WatermarkVisible");
			else
				GoToVisualState("WatermarkCollapsed");
		}

		private void GoToVisualState(string stateName, bool useTransitions = true)
		{
			VisualStateManager.GoToState(this, stateName, useTransitions);
		}

		#endregion
	}
}
