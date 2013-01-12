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
        private ThirdGenCacheFile _cache;
        private IStreamManager _streamManager;
        private ILanguage _currentLanguage;
        private LocaleTable _currentLocaleTable;
        private List<LocaleEntry> _locales;
        private ICollectionView _localeView;
        private ObservableCollection<LanguageEntry> languages = new ObservableCollection<LanguageEntry>();
        private string _filter;

        public LocaleEditor(ThirdGenCacheFile cache, IStreamManager streamManager, int index)
        {
            InitializeComponent();

            _cache = cache;
            _streamManager = streamManager;
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
            using (EndianReader reader = new EndianReader(_streamManager.OpenRead(), Endian.BigEndian))
            {
                _currentLocaleTable = _currentLanguage.LoadStrings(reader);
            }

            _locales = new List<LocaleEntry>();
            _localeView = CollectionViewSource.GetDefaultView(_locales);

            for (int i = 0; i < _currentLocaleTable.Strings.Count; i++)
            {
                Locale locale = _currentLocaleTable.Strings[i];
                _locales.Add(new LocaleEntry()
                    {
                        Index = i,
                        Locale = locale.Value,
                        StringID = _cache.StringIDs.GetString(locale.ID)
                    });
            }

            Dispatcher.Invoke(new Action( delegate { lvLocales.DataContext = _localeView; }));
        }

        private bool LocaleFilter(object item)
        {
            LocaleEntry locale = (LocaleEntry)item;
            return (locale.Locale.Contains(_filter) || (locale.StringID != null && locale.StringID.Contains(_filter)));
        }

        /// <summary>
        /// Filter the currently selected language
        /// </summary>
        /// <param name="filter">The filter string</param>
        private void FilterLanguage(string filter)
        {
            _filter = filter;
            if (!string.IsNullOrEmpty(filter))
            {
                _localeView.Filter = LocaleFilter;
                btnReset.IsEnabled = true;
            }
            else
            {
                _localeView.Filter = null;
                btnReset.IsEnabled = false;
            }
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
            MetroMessageBox.Show("Not implemented yet, fool.");
        }

        private void btnReset_Click_1(object sender, RoutedEventArgs e)
        {
            txtFilter.Text = "";
            FilterLanguage(null);
        }
    }

    public class LocaleEntry
    {
        public int Index { get; set; }
        public string StringID { get; set; }
        public string Locale { get; set; }
    }
    public class LanguageEntry
    {
        public int Index { get; set; }
        public string Language { get; set; }
    }
}
