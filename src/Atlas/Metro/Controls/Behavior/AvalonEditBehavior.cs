using System;
using System.Windows;
using ICSharpCode.AvalonEdit;
using System.Windows.Interactivity;

namespace Atlas.Metro.Controls.Behavior
{
	public sealed class AvalonEditBehavior : Behavior<TextEditor>
	{
		public static readonly DependencyProperty GiveMeTheTextProperty =
			DependencyProperty.Register("GiveMeTheText", typeof (string), typeof (AvalonEditBehavior),
				new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
					PropertyChangedCallback));

		public static TextEditor DatTextEditor { get; private set; }

		public string GiveMeTheText
		{
			get { return (string) GetValue(GiveMeTheTextProperty); }
			set { SetValue(GiveMeTheTextProperty, value); }
		}

		protected override void OnAttached()
		{
			base.OnAttached();
			if (AssociatedObject != null)
			{
				DatTextEditor = AssociatedObject;
				AssociatedObject.TextChanged += AssociatedObjectOnTextChanged;
			}
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();
			if (AssociatedObject != null)
			{
				DatTextEditor = null;
				AssociatedObject.TextChanged -= AssociatedObjectOnTextChanged;
			}
		}

		private void AssociatedObjectOnTextChanged(object sender, EventArgs eventArgs)
		{
			var textEditor = sender as TextEditor;
			if (textEditor == null) return;

			if (textEditor.Document != null)
				GiveMeTheText = textEditor.Document.Text;
		}

		private static void PropertyChangedCallback(
			DependencyObject dependencyObject,
			DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
		{
			if (DatTextEditor == null) return;

			if (DatTextEditor.Document != null)
				DatTextEditor.Document.Text = dependencyPropertyChangedEventArgs.NewValue.ToString();
		}
	}
}
