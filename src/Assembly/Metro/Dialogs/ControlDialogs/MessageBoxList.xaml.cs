using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Assembly.Helpers;
using Assembly.Helpers.Native;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	/// Interaction logic for MessageBoxList.xaml
	/// </summary>
	public partial class MessageBoxList
	{
        public MessageBoxList(string title, string message, IEnumerable<object> items)
        {
            InitializeComponent();

            lblTitle.Text = title;
            lblSubInfo.Text = message;

            listbox.Items.Clear();
            foreach (var item in items)
                listbox.Items.Add(item);
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
