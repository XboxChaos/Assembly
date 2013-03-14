using System;
using System.Windows;
using ICSharpCode.AvalonEdit;

namespace Assembly.Metro.Controls.BindingTextEditor
{
	public class MvvmTextEditor : TextEditor
	{
		public static DependencyProperty DocumentTextProperty =
			DependencyProperty.Register("DocumentText",
										typeof(string), typeof(MvvmTextEditor),
			new PropertyMetadata((obj, args) =>
			{
				var target = (MvvmTextEditor)obj;
				target.Document.UndoStack.EndUndoGroup();
				target.DocumentText = (string)args.NewValue;
			})
		);

		public string DocumentText
		{
			get
			{
				return Text;
			}
			set
			{
				Text = value;
			}
		}

		protected override void OnTextChanged(EventArgs e)
		{
			SetCurrentValue(DocumentTextProperty, Text);
			base.OnTextChanged(e);
		}
	}
}
