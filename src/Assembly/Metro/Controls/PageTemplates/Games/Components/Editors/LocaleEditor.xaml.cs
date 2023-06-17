using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Assembly.Metro.Dialogs;
using Blamite.Blam;
using Blamite.Blam.Localization;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Util;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.Editors
{
	/// <summary>
	///     Interaction logic for LocaleEditor.xaml
	/// </summary>
	public partial class LocaleEditor : UserControl
	{
		public static RoutedCommand DeleteStringCommand = new RoutedCommand();
		public static RoutedCommand GoToGroupCommand = new RoutedCommand();
		private readonly ICacheFile _cache;
		private readonly GameLanguage _currentLanguage;
		private readonly List<NamedStringList> _groups = new List<NamedStringList>();
		private readonly IStreamManager _streamManager;
		private readonly LocaleSymbolCollection _symbols;
		private LocalizedStringList _currentGroup;
		private LanguagePack _currentPack;
		private string _searchText;
		private LocalizedStringTableView _stringView;
		private List<StringEntry> _strings;

		public LocaleEditor(GameLanguage language, ICacheFile cache, IStreamManager streamManager, Trie stringIdTrie,
			LocaleSymbolCollection symbols)
		{
			InitializeComponent();

			_currentLanguage = language;
			_cache = cache;
			_streamManager = streamManager;
			_symbols = symbols;
			StringIDTrie = stringIdTrie;

			if (cache.Engine < EngineType.ThirdGeneration ||
				(cache.Engine == EngineType.ThirdGeneration && cache.HeaderSize == 0x800))
				btnSaveAll.Visibility = btnAddNew.Visibility = Visibility.Collapsed;

			// Thread the loading routine
			var thrd = new Thread(LoadAll);
			thrd.SetApartmentState(ApartmentState.STA);
			thrd.Start();
		}

		/// <summary>
		///     Gets the stringID trie to use for autocompletion.
		/// </summary>
		public Trie StringIDTrie { get; private set; }

		#region Helpers

		/// <summary>
		///     Loads the list of locale groups.
		/// </summary>
		private void LoadGroups()
		{
			// Wrap the groups stored in the cache file and sort them
			foreach (LocalizedStringList group in _currentPack.StringLists)
			{
				string name = _cache.FileNames.GetTagName(group.SourceTag) ?? group.SourceTag.Index.ToString();
				_groups.Add(new NamedStringList {Name = name, Base = group});
			}
			_groups.Sort((g1, g2) => g1.Name.CompareTo(g2.Name));

			// Create a group for everything
			_groups.Insert(0, new NamedStringList {Name = "(all strings)", Base = null});

			// Select the "everything" group
			Dispatcher.Invoke(new Action(delegate
			{
				cbLocaleGroups.ItemsSource = _groups;
				cbLocaleGroups.SelectedIndex = 0;
			}));
		}

		/// <summary>
		///     Loads the list of strings.
		/// </summary>
		private void LoadStrings()
		{
			_strings = new List<StringEntry>(_currentPack.StringLists.SelectMany(l => WrapStrings(l)));
			_stringView = new LocalizedStringTableView(_strings);
			_stringView.Filter = FilterString;
		}

		/// <summary>
		///     Loads everything.
		/// </summary>
		private void LoadAll()
		{
			using (IReader reader = _streamManager.OpenRead())
				_currentPack = _cache.Languages.LoadLanguage(_currentLanguage, reader);

			LoadStrings();
			LoadGroups();

			Dispatcher.Invoke(new Action(delegate { lvLocales.DataContext = _stringView; }));
		}

		/// <summary>
		///     Wraps the strings in a <see cref="LocalizedStringList" />.
		/// </summary>
		/// <param name="list">The list to wrap.</param>
		/// <returns>The wrapped strings.</returns>
		private IEnumerable<StringEntry> WrapStrings(LocalizedStringList list)
		{
			return list.Strings.Select(s =>
			{
				string stringIdName = _cache.StringIDs.GetString(s.Key) ?? s.Key.ToString();
				string adjustedStr = ReplaceSymbols(s.Value);
				var entry = new StringEntry(stringIdName, adjustedStr, list, s);
				return entry;
			});
		}

		/// <summary>
		///     Replaces special characters in a string.
		/// </summary>
		/// <param name="str">The string to replace special characters in.</param>
		/// <returns>The new string.</returns>
		private string ReplaceSymbols(string str)
		{
			return (_symbols != null) ? _symbols.ReplaceSymbols(str) : str;
		}

		/// <summary>
		///     Replaces tags in a string with special characters.
		/// </summary>
		/// <param name="locale">The string to replace tags in.</param>
		/// <returns>The new string.</returns>
		private string ReplaceTags(string locale)
		{
			return (_symbols != null) ? _symbols.ReplaceTags(locale) : locale;
		}

		/// <summary>
		///     Filter function for the string view.
		/// </summary>
		/// <param name="item">The item to filter.</param>
		/// <returns><c>true</c> if the item should be made visible.</returns>
		private bool FilterString(object item)
		{
			var entry = (StringEntry) item;
			if (_currentGroup != null && entry.Group != _currentGroup)
			{
				// Only show locales in the current group
				return false;
			}
			if (!string.IsNullOrEmpty(_searchText))
			{
				// Only show strings that match the filter
				if (!entry.Value.ToLower().Contains(_searchText) && !entry.StringID.ToLower().Contains(_searchText))
					return false;
			}
			return true;
		}

		/// <summary>
		///     Sets the text to search for in locales to determine their visibility.
		/// </summary>
		/// <param name="filter">The text to search for, or <c>null</c> to show everything.</param>
		private void SetSearchText(string filter)
		{
			if (filter != null)
				_searchText = filter.ToLower();
			else
				_searchText = null;

			btnReset.IsEnabled = !string.IsNullOrEmpty(filter);
			_stringView.Refresh();
			ShowCurrentItem();
		}

		/// <summary>
		///     Resolves the stringID for a string entry, creating a new stringID if it doesn't exist.
		/// </summary>
		/// <param name="entry">The entry to resolve a stringID for.</param>
		/// <returns>The resolved stringID.</returns>
		private StringID ResolveStringID(StringEntry entry)
		{
			if (entry.StringID == "")
			{
				return StringID.Null;
			}
			if (entry.Base != null)
			{
				// If the stringID didn't change, then re-use it
				string oldKeyStr = _cache.StringIDs.GetString(entry.Base.Key);
				if (oldKeyStr == entry.StringID)
					return entry.Base.Key;
			}
			if (StringIDTrie.Contains(entry.StringID))
			{
				// String already exists in the cache file
				return _cache.StringIDs.FindStringID(entry.StringID);
			}
			// Create a new string
			StringIDTrie.Add(entry.StringID);
			return _cache.StringIDs.AddString(entry.StringID);
		}

		/// <summary>
		///     Verifies that the user intended to add stringIDs that aren't in the cache file.
		/// </summary>
		/// <returns><c>true</c> if the user intended to add all of the strings.</returns>
		private bool VerifyAddedStringIDs()
		{
			List<string> newStringIds = _strings
				.Select(l => l.StringID)
				.Where(s => s != "" && !StringIDTrie.Contains(s))
				.ToList();

			if (newStringIds.Count > 0 &&
			    !MetroMessageBoxList.Show("Language Pack Editor",
				    "The following stringID(s) do not currently exist in the cache file and will be added.\r\nContinue?",
				    newStringIds))
				return false;
			return true;
		}

		/// <summary>
		///     Saves all strings back to the cache file
		/// </summary>
		private void SaveStrings()
		{
			foreach (StringEntry entry in _strings)
			{
				StringID key = ResolveStringID(entry);
				string val = ReplaceTags(entry.Value);
				if (entry.Base == null)
				{
					// Create a new entry for the string
					entry.Base = new LocalizedString(key, val);
					entry.Group.Strings.Add(entry.Base);
				}
				else
				{
					// Update the old entry
					entry.Base.Key = key;
					entry.Base.Value = val;
				}
			}
			using (IStream stream = _streamManager.OpenReadWrite())
			{
				_cache.Languages.SaveLanguage(_currentPack, stream);
				_cache.SaveChanges(stream);
			}
		}

		/// <summary>
		///     Adds a new string to the table.
		/// </summary>
		/// <returns>The added entry.</returns>
		private StringEntry NewEntry()
		{
			// The view has to be cast to IEditableCollectionView here so that the explicitly-implemented methods are called
			// Otherwise, the ListCollectionView methods will disallow this
			var entry = ((IEditableCollectionView) _stringView).AddNew() as StringEntry;
			((IEditableCollectionView) _stringView).CommitNew();
			return entry;
		}

		/// <summary>
		///     Begins editing a string entry.
		/// </summary>
		/// <param name="entry">The entry to begin editing.</param>
		private void StartEditing(StringEntry entry)
		{
			var cell = new DataGridCellInfo(entry, stringIdColumn);
			lvLocales.ScrollIntoView(entry);
			lvLocales.Focus();
			lvLocales.CurrentCell = cell;
			lvLocales.BeginEdit();
		}

		/// <summary>
		///     Scrolls the currently-selected item (if any) into view.
		/// </summary>
		private void ShowCurrentItem()
		{
			if (lvLocales.SelectedItem != null)
				lvLocales.ScrollIntoView(lvLocales.SelectedItem);
		}

		#endregion

		#region Event Handlers

		private void txtFilter_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return || e.Key == Key.Enter)
				SetSearchText(txtFilter.Text);
		}

		private void btnFilter_Click(object sender, RoutedEventArgs e)
		{
			SetSearchText(txtFilter.Text);
		}

		private void btnSaveAll_Click(object sender, RoutedEventArgs e)
		{
			if (!VerifyAddedStringIDs())
				return;

			// Disable everything and save in the background so the UI doesn't lag
			var worker = new BackgroundWorker();
			worker.DoWork += DoSave;
			worker.RunWorkerCompleted += SaveCompleted;
			IsEnabled = false;
			worker.RunWorkerAsync();
		}

		private void DoSave(object sender, DoWorkEventArgs e)
		{
			SaveStrings();
		}

		private void SaveCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			IsEnabled = true;
			if (e.Error == null)
				MetroMessageBox.Show("Language Pack Editor", "Strings saved successfully!");
			else
				MetroException.Show(e.Error);
		}

		private void btnReset_Click(object sender, RoutedEventArgs e)
		{
			txtFilter.Text = "";
			SetSearchText(null);
		}

		private void cbLocaleGroups_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
		{
			var group = cbLocaleGroups.SelectedItem as NamedStringList;
			if (group != null)
			{
				_currentGroup = group.Base;
				_stringView.CurrentGroup = group.Base;
				_stringView.Refresh();
				lvLocales.CanUserAddRows = (group.Base != null);
				ShowCurrentItem();
			}
		}

		private void btnAddNew_Click_1(object sender, RoutedEventArgs e)
		{
			if (_currentGroup == null)
			{
				MetroMessageBox.Show("Language Pack Editor",
					"You must select a specific string list first in order to add a new string.");
				return;
			}

			StringEntry entry = NewEntry();
			StartEditing(entry);
		}

		private void stringId_Loaded(object sender, RoutedEventArgs e)
		{
			// Hack to make the stringID textbox get focus
			var control = (FrameworkElement) sender;
			control.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
		}

		#endregion

		#region Commands

		/// <summary>
		///     Gets the <see cref="StringEntry" /> that a command originated from.
		/// </summary>
		/// <param name="commandSource">The command's source.</param>
		/// <returns>The entry that the command originated from, or <c>null</c> if not available.</returns>
		private StringEntry GetSourceEntry(object commandSource)
		{
			var elem = commandSource as FrameworkElement;
			if (elem != null)
			{
				var entry = elem.DataContext as StringEntry;
				if (entry != null)
					return entry;
			}
			return null;
		}

		private void StringMenuCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = (GetSourceEntry(e.Source) != null);
		}

		private void DeleteStringCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			StringEntry entry = GetSourceEntry(e.Source);
			if (entry != null)
				((IEditableCollectionView) _stringView).Remove(entry);
		}

		private void GoToGroupCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			StringEntry entry = GetSourceEntry(e.Source);
			if (entry != null)
				cbLocaleGroups.SelectedItem = _groups.FirstOrDefault(g => g.Base == entry.Group);
		}

		#endregion

		#region Private Classes

		/// <summary>
		///     Implementation of <see cref="ListCollectionView" /> for localized string lists.
		/// </summary>
		private class LocalizedStringTableView : ListCollectionView, IEditableCollectionView,
			IEditableCollectionViewAddNewItem
		{
			public LocalizedStringTableView(IList list)
				: base(list)
			{
			}

			/// <summary>
			///     Gets or sets the group that new strings should be added to.
			/// </summary>
			public LocalizedStringList CurrentGroup { get; set; }

			/// <summary>
			///     Starts an add transaction and returns the pending new item.
			/// </summary>
			/// <returns>
			///     The pending new item.
			/// </returns>
			object IEditableCollectionView.AddNew()
			{
				if (CurrentGroup == null)
					throw new InvalidOperationException("CurrentGroup is null");

				var result = new StringEntry("", "", CurrentGroup, null);
				return AddNewItem(result);
			}

			/// <summary>
			///     Removes the specified item from the collection.
			/// </summary>
			/// <param name="item">The item to remove.</param>
			void IEditableCollectionView.Remove(object item)
			{
				((IEditableCollectionView) this).RemoveAt(IndexOf(item));
			}

			/// <summary>
			///     Removes the item at the specified position from the collection.
			/// </summary>
			/// <param name="index">The zero-based index of the item to remove.</param>
			void IEditableCollectionView.RemoveAt(int index)
			{
				var entry = GetItemAt(index) as StringEntry;
				base.RemoveAt(index);

				// Remove the entry from its group
				if (entry.Base != null)
				{
					entry.Group.Strings.Remove(entry.Base);
					entry.Base = null;
					entry.Group = null;
				}
			}

			/// <summary>
			///     Gets a value that indicates whether a new item can be added to the collection.
			/// </summary>
			/// <returns>true if a new item can be added to the collection; otherwise, false.</returns>
			bool IEditableCollectionView.CanAddNew
			{
				// New items can be added as long as a group is set
				get { return CurrentGroup != null; }
			}

			/// <summary>
			///     Gets a value that indicates whether the collection view can discard pending changes and restore the original values
			///     of an edited object.
			/// </summary>
			/// <returns>
			///     true if the collection view can discard pending changes and restore the original values of an edited object;
			///     otherwise, false.
			/// </returns>
			bool IEditableCollectionView.CanCancelEdit
			{
				get { return true; }
			}

			/// <summary>
			///     Gets a value that indicates whether an item can be removed from the collection.
			/// </summary>
			/// <returns>true if an item can be removed from the collection; otherwise, false.</returns>
			bool IEditableCollectionView.CanRemove
			{
				get { return true; }
			}

			/// <summary>
			///     Gets a value that indicates whether a specified object can be added to the collection.
			/// </summary>
			/// <returns>true if a specified object can be added to the collection; otherwise, false.</returns>
			bool IEditableCollectionViewAddNewItem.CanAddNewItem
			{
				// New items can be added as long as a group is set
				get { return CurrentGroup != null; }
			}
		}

		/// <summary>
		///     A string list with a name associated with it.
		/// </summary>
		private class NamedStringList
		{
			/// <summary>
			///     Gets or sets the name of the string list.
			/// </summary>
			public string Name { get; set; }

			/// <summary>
			///     Gets or sets the base list.
			/// </summary>
			public LocalizedStringList Base { get; set; }
		}

		/// <summary>
		///     A localized string entry.
		/// </summary>
		private class StringEntry : PropertyChangeNotifier
		{
			private string _stringId;
			private string _val;

			public StringEntry(string stringId, string val, LocalizedStringList group, LocalizedString baseString)
			{
				_stringId = stringId;
				_val = val;
				Group = group;
				Base = baseString;
			}

			/// <summary>
			///     Gets or sets the entry's stringID as a string.
			/// </summary>
			public string StringID
			{
				get { return _stringId; }
				set
				{
					_stringId = value;
					NotifyPropertyChanged("StringID");
				}
			}

			/// <summary>
			///     Gets or sets the entry's string value.
			/// </summary>
			public string Value
			{
				get { return _val; }
				set
				{
					_val = value;
					NotifyPropertyChanged("Locale");
				}
			}

			/// <summary>
			///     Gets or sets the group that the string belongs to.
			/// </summary>
			public LocalizedStringList Group { get; set; }

			/// <summary>
			///     Gets or sets the base <see cref="LocalizedString" /> that the entry was created from.
			/// </summary>
			public LocalizedString Base { get; set; }
		}

		#endregion
	}
}