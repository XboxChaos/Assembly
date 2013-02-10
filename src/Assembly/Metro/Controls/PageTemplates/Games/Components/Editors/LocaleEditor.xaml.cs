using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Assembly.Metro.Dialogs;
using Assembly.Windows;
using ExtryzeDLL.Blam;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.IO;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.Editors
{
    /// <summary>
    /// Interaction logic for LocaleEditor.xaml
    /// </summary>
    public partial class LocaleEditor : UserControl
    {
        private ICacheFile _cache;
        private IStreamManager _streamManager;
        private int _languageIndex;
        private ILanguage _currentLanguage;
        private LocaleTable _currentLocaleTable;
        private List<LocaleEntry> _locales;
        private ICollectionView _localeView;
        private ObservableCollection<LocaleGroup> _groups = new ObservableCollection<LocaleGroup>();
        private LocaleRange _currentRange;
        private string _filter;

        public LocaleEditor(ICacheFile cache, IStreamManager streamManager, int index)
        {
            InitializeComponent();

            _cache = cache;
            _streamManager = streamManager;
            _languageIndex = index;
            _currentLanguage = cache.Languages[index];

            Thread thrd = new Thread(new ThreadStart(LoadLanguage));
            thrd.SetApartmentState(ApartmentState.STA);
            thrd.Start();
        }

        private void lvLocales_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           //LocaleEntry localeEntry = (LocaleEntry)localeEntries.SelectedItem;
           //if (localeEntry != null)
           //     _haloMap.txtLocaleSelectedContent.Text = localeEntry.Locale;
        }

        /// <summary>
        /// Load a language into the listview
        /// </summary>
        private void LoadLanguage()
        {
            using (EndianReader reader = new EndianReader(_streamManager.OpenRead(), _streamManager.SuggestedEndian))
            {
                _currentLocaleTable = _currentLanguage.LoadStrings(reader);
            }

            _locales = new List<LocaleEntry>();
            _localeView = CollectionViewSource.GetDefaultView(_locales);
            _localeView.Filter = LocaleFilter;

            for (int i = 0; i < _currentLocaleTable.Strings.Count; i++)
            {
                Locale locale = _currentLocaleTable.Strings[i];
                string stringId = _cache.StringIDs.GetString(locale.ID);
                if (stringId == null)
                    stringId = locale.ID.ToString();

                _locales.Add(new LocaleEntry(i, stringId, locale.Value));
            }

            LoadGroups();

            Dispatcher.Invoke(new Action( delegate { lvLocales.DataContext = _localeView; }));
        }

        /// <summary>
        /// Loads the list of locale groups.
        /// </summary>
        private void LoadGroups()
        {
            // Make a default group that shows everything
            LocaleRange allLocales = new LocaleRange(0, _locales.Count);
            _currentRange = allLocales;
            _groups.Add(new LocaleGroup() { Name = "(show all)", Range = allLocales });

            // Load the groups stored in the cache file
            foreach (ILocaleGroup group in _cache.LocaleGroups)
            {
                string name = _cache.FileNames.FindTagName(group.TagIndex);
                LocaleRange range = group.Ranges[_languageIndex];

                _groups.Add(new LocaleGroup() { Name = name, Range = range });
            }

            Dispatcher.Invoke(new Action(delegate
                {
                    cbLocaleGroups.ItemsSource = _groups;
                    cbLocaleGroups.SelectedIndex = 0;
                }));
        }

        private bool LocaleFilter(object item)
        {
            LocaleEntry locale = (LocaleEntry)item;
            if (_currentRange != null)
            {
                // Only show locales in the current range
                if (locale.Index < _currentRange.StartIndex || locale.Index >= _currentRange.StartIndex + _currentRange.Size)
                    return false;
            }
            if (!string.IsNullOrEmpty(_filter))
            {
                // Only show locales that match the filter
                if (!locale.Locale.ToLower().Contains(_filter) && !locale.StringID.ToLower().Contains(_filter))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Filter the currently selected language
        /// </summary>
        /// <param name="filter">The filter string</param>
        private void FilterLanguage(string filter)
        {
            if (filter != null)
                _filter = filter.ToLower();
            else
                _filter = null;

            btnReset.IsEnabled = !string.IsNullOrEmpty(filter);
            _localeView.Refresh();
        }

        private void txtFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
                FilterLanguage(txtFilter.Text);
        }

        private void btnFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterLanguage(txtFilter.Text);
        }

        private void btnSaveAll_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < _locales.Count; i++)
                _currentLocaleTable.Strings[i].Value = _locales[i].Locale;

            using (EndianStream stream = new EndianStream(_streamManager.OpenReadWrite(), _streamManager.SuggestedEndian))
            {
                _currentLanguage.SaveStrings(stream, _currentLocaleTable);
                _cache.SaveChanges(stream);
            }

            MetroMessageBox.Show("Locales Saved", "Locales saved successfully!");
        }

        private void btnReset_Click_1(object sender, RoutedEventArgs e)
        {
            txtFilter.Text = "";
            FilterLanguage(null);
        }

        private void cbLocaleGroups_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            LocaleGroup group = cbLocaleGroups.SelectedItem as LocaleGroup;
            if (group != null)
            {
                _currentRange = group.Range;
                _localeView.Refresh();
            }
        }

        class LocaleEntry : PropertyChangeNotifier
        {
            private int _index;
            private string _stringId;
            private string _locale;

            public LocaleEntry(int index, string stringId, string locale)
            {
                _index = index;
                _stringId = stringId;
                _locale = locale;
            }

            public int Index
            {
                get { return _index; }
                set { _index = value; NotifyPropertyChanged("Index"); }
            }

            public string StringID
            {
                get { return _stringId; }
                set { _stringId = value; NotifyPropertyChanged("StringID"); }
            }

            public string Locale
            {
                get { return _locale; }
                set { _locale = value; NotifyPropertyChanged("Locale"); }
            }
        }

        class LanguageEntry
        {
            public int Index { get; set; }
            public string Language { get; set; }
        }

        class LocaleGroup
        {
            public string Name { get; set; }
            public LocaleRange Range { get; set; }
        }
    }
}
