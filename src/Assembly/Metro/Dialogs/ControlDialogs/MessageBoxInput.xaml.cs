using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Assembly.Helpers;
using Assembly.Helpers.Native;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	/// Interaction logic for MessageBoxInput.xaml
	/// </summary>
	public partial class MessageBoxInput
	{
		private readonly string _regexMatch;

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			txtInput.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));

			base.OnMouseDown(e);
		}

		public MessageBoxInput(string title, string message, string placeholder, string regexMatch, string defaultText)
        {
            InitializeComponent();
            DwmDropShadow.DropShadowToWindow(this);

            lblTitle.Text = title;
            lblSubInfo.Text = message;
			lblPlaceholder.Text = placeholder;
			txtInput.Text = defaultText;
			_regexMatch = regexMatch;

			txtInput_LostFocus(txtInput, null);
        }

		private void txtInput_GotFocus(object sender, RoutedEventArgs e)
		{
			lblPlaceholder.Visibility = Visibility.Collapsed;
		}
		private void txtInput_LostFocus(object sender, RoutedEventArgs e)
		{
			if (!(sender is TextBox)) return;
			var textbox = sender as TextBox;
			lblPlaceholder.Visibility = string.IsNullOrEmpty(textbox.Text) ? 
				Visibility.Visible : Visibility.Collapsed;
		}
		private void lblPlaceholder_MouseDown(object sender, MouseButtonEventArgs e)
		{
			// TODO: hacky thing to give textbox focus on click
		}

		private void btnContinue_Click(object sender, RoutedEventArgs e)
		{
			ValidateInput();
		}
		private void txtInput_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
				ValidateInput();
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void ValidateInput()
		{
			if (_regexMatch != null)
			{
				var data = txtInput.Text;
				var match = Regex.Match(data, _regexMatch, RegexOptions.IgnoreCase);

				if (!match.Success)
				{
					// TODO: aaron, make this more user friendly, huehue
					MetroMessageBox.Show("Invalid Input", "The text you entered was not valid.");
					return;
				}
			}

			TempStorage.MessageBoxInputStorage = txtInput.Text;
			Close();
		}
    }
}
