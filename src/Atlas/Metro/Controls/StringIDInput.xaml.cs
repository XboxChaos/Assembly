using System.Windows;
using System.Windows.Controls;
using Blamite.Util;

namespace Assembly.Metro.Controls
{
	/// <summary>
	///     Interaction logic for StringIDInput.xaml
	/// </summary>
	public partial class StringIDInput : UserControl
	{
		/// <summary>
		///     The <see cref="Trie" /> to use to search for stringIDs.
		/// </summary>
		public static readonly DependencyProperty SearchTrieProperty = DependencyProperty.Register("SearchTrie", typeof (Trie),
			typeof (StringIDInput));

		/// <summary>
		///     The text in the textbox.
		/// </summary>
		public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof (string),
			typeof (StringIDInput), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		/// <summary>
		///     Initializes a new instance of the <see cref="StringIDInput" /> class.
		/// </summary>
		public StringIDInput()
		{
			InitializeComponent();
		}

		/// <summary>
		///     Gets or sets the <see cref="Trie" /> to use to search for stringIDs.
		/// </summary>
		public Trie SearchTrie
		{
			get { return (Trie) GetValue(SearchTrieProperty); }
			set { SetValue(SearchTrieProperty, value); }
		}

		/// <summary>
		///     Gets or sets the text in the textbox.
		/// </summary>
		public string Text
		{
			get { return (string) GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		private void textbox_Populating(object sender, PopulatingEventArgs e)
		{
			e.Cancel = true;
			if (SearchTrie != null)
			{
				textbox.ItemsSource = SearchTrie.FindPrefix(e.Parameter);
				textbox.PopulateComplete();
			}
		}

		private void textbox_GotFocus(object sender, RoutedEventArgs e)
		{
			if (!textbox.IsDropDownOpen)
			{
				// This is a hack that makes focus work properly for AutoCompleteBox controls
				var internalTextBox = textbox.Template.FindName("Text", textbox) as UIElement;
				if (internalTextBox != null)
					internalTextBox.Focus();
			}
		}
	}
}