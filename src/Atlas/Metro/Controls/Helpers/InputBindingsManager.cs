using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Atlas.Metro.Controls.Helpers
{
	// http://stackoverflow.com/questions/563195/bind-textbox-on-enter-key-press
	public static class InputBindingsManager
	{
		public static readonly DependencyProperty UpdatePropertySourceWhenEnterPressedProperty = DependencyProperty
			.RegisterAttached(
				"UpdatePropertySourceWhenEnterPressed", typeof (DependencyProperty), typeof (InputBindingsManager),
				new PropertyMetadata(null, OnUpdatePropertySourceWhenEnterPressedPropertyChanged));

		public static void SetUpdatePropertySourceWhenEnterPressed(DependencyObject dp, DependencyProperty value)
		{
			dp.SetValue(UpdatePropertySourceWhenEnterPressedProperty, value);
		}

		public static DependencyProperty GetUpdatePropertySourceWhenEnterPressed(DependencyObject dp)
		{
			return (DependencyProperty) dp.GetValue(UpdatePropertySourceWhenEnterPressedProperty);
		}

		private static void OnUpdatePropertySourceWhenEnterPressedPropertyChanged(DependencyObject dp,
			DependencyPropertyChangedEventArgs e)
		{
			var element = dp as UIElement;
			if (element == null)
				return;

			if (e.OldValue != null)
				element.PreviewKeyDown -= HandlePreviewKeyDown;

			if (e.NewValue != null)
				element.PreviewKeyDown += HandlePreviewKeyDown;
		}

		private static void HandlePreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				DoUpdateSource(e.Source);
		}

		private static void DoUpdateSource(object source)
		{
			DependencyProperty property = GetUpdatePropertySourceWhenEnterPressed(source as DependencyObject);
			if (property == null)
				return;

			var elt = source as UIElement;
			if (elt == null)
				return;

			BindingExpression binding = BindingOperations.GetBindingExpression(elt, property);
			if (binding != null)
				binding.UpdateSource();
		}
	}
}