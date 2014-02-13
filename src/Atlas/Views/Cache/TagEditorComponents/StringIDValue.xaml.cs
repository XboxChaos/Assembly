using System.Windows;
using System.Windows.Controls;
using Blamite.Util;

namespace Atlas.Views.Cache.TagEditorComponents
{
	/// <summary>
	///     Interaction logic for StringIDValue.xaml
	/// </summary>
	public partial class StringIDValue : UserControl
	{
		public static readonly DependencyProperty SearchTrieProperty = DependencyProperty.Register("SearchTrie", typeof (Trie),
			typeof (StringIDValue));

		public StringIDValue()
		{
			InitializeComponent();
		}

		public Trie SearchTrie
		{
			get { return (Trie) GetValue(SearchTrieProperty); }
			set { SetValue(SearchTrieProperty, value); }
		}
	}
}