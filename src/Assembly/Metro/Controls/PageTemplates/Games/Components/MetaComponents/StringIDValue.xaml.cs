using Assembly.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Blamite.Util;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
    /// <summary>
    /// Interaction logic for StringIDValue.xaml
    /// </summary>
    public partial class StringIDValue : UserControl
    {
        public StringIDValue()
        {
            InitializeComponent();
        }

        public readonly static DependencyProperty SearchTrieProperty = DependencyProperty.Register("SearchTrie", typeof(Trie), typeof(StringIDValue));

        public Trie SearchTrie
        {
            get { return (Trie)GetValue(SearchTrieProperty); }
            set { SetValue(SearchTrieProperty, value); }
        }
    }
}
