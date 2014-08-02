using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Atlas.Common;
using Atlas.Dialogs;
using Atlas.Helpers.Tags;
using Atlas.Models;
using Atlas.Models.Cache;
using Atlas.Views;
using Atlas.Views.BLF;
using Atlas.Views.Cache;
using Blamite.Blam.Scripting;
using Blamite.IO;

namespace Atlas.ViewModels
{
    public class HomeViewModel : Base
    {
        public HomeViewModel()
        {
            _statusResetTimer.Tick += (sender, args) => { Status = "Ready..."; };

            _recentEditors.CollectionChanged += delegate { OnPropertyChanged("RecentEditors"); };

            OpenRecentEditorCommand = new RelayCommand<RecentEditor>(OpenRecentEditor);
        }

        #region Properties

        #region Application Stuff

        private readonly DispatcherTimer _statusResetTimer = new DispatcherTimer {Interval = new TimeSpan(0, 0, 5)};
        private Thickness _applicationBorderThickness;

        private string _applicationTitle = "Welcome";
        private string _status = "Ready...";

        public string Status
        {
            get { return _status; }
            set
            {
                if (!value.EndsWith("..."))
                    value += "...";

                _statusResetTimer.Stop();
                SetField(ref _status, value);
                _statusResetTimer.Start();
            }
        }

        public string ApplicationTitle
        {
            get { return _applicationTitle; }
            set { SetField(ref _applicationTitle, value); }
        }

        public Thickness ApplicationBorderThickness
        {
            get { return _applicationBorderThickness; }
            set { SetField(ref _applicationBorderThickness, value); }
        }

        #endregion

        #region Window Masks

        private Visibility _maskVisibility = Visibility.Collapsed;

        public Visibility MaskVisibility
        {
            get { return _maskVisibility; }
            set { SetField(ref _maskVisibility, value); }
        }

        #endregion

        #region Window Actions/Resizing

        private Visibility _actionMaximizeVisibility = Visibility.Collapsed;
        private Visibility _actionRestoreVisibility = Visibility.Collapsed;
        private Visibility _resizingVisibility = Visibility.Collapsed;

        public Visibility ActionRestoreVisibility
        {
            get { return _actionRestoreVisibility; }
            set { SetField(ref _actionRestoreVisibility, value); }
        }

        public Visibility ActionMaximizeVisibility
        {
            get { return _actionMaximizeVisibility; }
            set { SetField(ref _actionMaximizeVisibility, value); }
        }

        public Visibility ResizingVisibility
        {
            get { return _resizingVisibility; }
            set { SetField(ref _resizingVisibility, value); }
        }

        #endregion

        #region Content

        private IAssemblyPage _assemblyPage;

        public IAssemblyPage AssemblyPage
        {
            get { return _assemblyPage; }
            set
            {
                // try closing current page
                if (_assemblyPage != null && !_assemblyPage.Close())
                    return;

                // aite, we can
                SetField(ref _assemblyPage, value);
            }
        }

        #endregion

        #endregion

        #region Events

        #region Overrides

        public void OnStateChanged(object sender, EventArgs eventArgs)
        {
            switch ((WindowState) sender)
            {
                case WindowState.Normal:
                    ApplicationBorderThickness = new Thickness(1, 1, 1, 25);
                    ActionRestoreVisibility = Visibility.Collapsed;
                    ActionMaximizeVisibility =
                        ResizingVisibility = Visibility.Visible;
                    break;
                case WindowState.Maximized:
                    ApplicationBorderThickness = new Thickness(7, 7, 7, 32);
                    ActionRestoreVisibility = Visibility.Visible;
                    ActionMaximizeVisibility =
                        ResizingVisibility = Visibility.Collapsed;
                    break;
            }
        }

        #endregion

        #endregion

        #region Dialog Management

        private int _maskCount;

        public void ShowDialog(bool showMask = true)
        {
            if (showMask)
                _maskCount++;

            if (_maskCount > 0)
                MaskVisibility = Visibility.Visible;
        }

        public void HideDialog(bool maskShown = true)
        {
            if (maskShown)
                _maskCount--;

            if (_maskCount == 0)
                MaskVisibility = Visibility.Collapsed;
        }

        #endregion

        #region Helpers

        public enum Type
        {
            BlamCache,
            MapInfo,
            MapImage,
            Campaign,
            Patch,

            Other
        }

        public void UpdateStatus(string status)
        {
            ApplicationTitle = App.Storage.HomeWindow.Title = String.Format("{0} - Assembly", status);
        }

        /// <summary>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string FindFile(Type type = Type.Other)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Blam Cache Files (*.map)|*.map|" +
                         "Blam Map Info (*.mapinfo)|*.mapinfo|" +
                         "Blam Map Image (*.blf)|*.blf|" +
                         "Blam Campaign (*.campaign)|*.campaign|" +
                         "Assembly Patch (*.asmp)|*.asmp|" +
                         "All Files (*.*)|*.*",
                FilterIndex = 1 + (int) type
            };

            return openFileDialog.ShowDialog() == DialogResult.OK ? openFileDialog.FileName : null;
        }

        /// <summary>
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="type"></param>
        public void ValidateFile(string filePath, Type type = Type.Other)
        {
            if (filePath == null)
                return;

            // try via type
            switch (type)
            {
                case Type.BlamCache:
                case Type.MapInfo:
                case Type.MapImage:
                case Type.Campaign:
                    OpenFile(filePath, type);
                    return;

                case Type.Patch:
                    throw new NotImplementedException();
            }

            // try via file extension
            var fileInfo = new FileInfo(filePath);
            switch (fileInfo.Extension.Remove(0, 1))
            {
                case "map":
                    OpenFile(filePath, Type.BlamCache);
                    return;

                case "mapinfo":
                    OpenFile(filePath, Type.MapInfo);
                    return;

                case "blf":
                    OpenFile(filePath, Type.MapImage);
                    return;

                case "campaign":
                    OpenFile(filePath, Type.Campaign);
                    return;

                case "asmp":
                    throw new NotImplementedException();
            }

            // ugh, try via magic
            string magic;
            using (var reader =
                new EndianReader(new FileStream(filePath, FileMode.Open, FileAccess.Read),
                    Endian.BigEndian))
                magic = reader.ReadAscii(0x04);

            switch (magic.Trim())
            {
                case "head":
                case "daeh":
                    OpenFile(filePath, Type.BlamCache);
                    return;

                case "asmp":
                case "blf":
                    throw new NotImplementedException();
            }

            // just fuck this
            MetroMessageBox.Show("This type of file is not supported by Assembly.");
        }

        /// <summary>
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="type"></param>
        public void OpenFile(string filePath, Type type)
        {
            switch (type)
            {
                case Type.BlamCache:
                    AssemblyPage = new CachePage(filePath);
                    break;

                case Type.MapInfo:
                    AssemblyPage = new MapInfoPage(filePath);
                    break;

                case Type.MapImage:
                    AssemblyPage = new MapImagePage(filePath);
                    break;

                case Type.Campaign:
                    AssemblyPage = new CampaignPage(filePath);
                    break;

                    //case Type.Patch:
                default:
                    throw new NotImplementedException();
            }

            var existing = App.Storage.Settings.RecentFiles.FirstOrDefault(r => r.Type == type && r.FilePath == filePath);
            if (existing != null)
                App.Storage.Settings.RecentFiles.Remove(existing);
            App.Storage.Settings.RecentFiles.Insert(0, new RecentFile(filePath, type));
        }

        #endregion

        #region Commands

        private ICommand _openRecentEditorCommand;

        public ICommand OpenRecentEditorCommand
        {
            get { return _openRecentEditorCommand; }
            set { SetField(ref _openRecentEditorCommand, value); }
        }

        #endregion

        #region Menu

        private const string TagEditorName = "Tag Editor";
        private const string ScriptEditorName = "Script Editor";
        private const string UnknownEditorName = "Unknown Editor";
        private ObservableCollection<RecentEditor> _recentEditors = new ObservableCollection<RecentEditor>();

        public ObservableCollection<RecentEditor> RecentEditors
        {
            get { return _recentEditors; }
            set { SetField(ref _recentEditors, value); }
        }

        public void AddRecentEditor(CachePageViewModel cachePageViewModel, ICacheEditor editor, object editorParamater,
            string tagPath)
        {
            string editorName;
            if (editor is TagEditor)
                editorName = TagEditorName;
            else if (editor is ScriptEditor)
                editorName = ScriptEditorName;
            else
                editorName = UnknownEditorName;

            RecentEditors.Insert(0,
                new RecentEditor
                {
                    EditorName = editorName,
                    EditorParamater = editorParamater,
                    InternalName = cachePageViewModel.CacheFile.InternalName,
                    TagPath = tagPath,
                    OpenCommand = OpenRecentEditorCommand
                });

            while (RecentEditors.Count > 10)
                RecentEditors.RemoveAt(10);
        }

        public void OpenRecentEditor(RecentEditor recentEditor)
        {
            var cachePage = AssemblyPage as CachePage;
            if (cachePage == null) return;
            switch (recentEditor.EditorName)
            {
                case TagEditorName:
                    cachePage.ViewModel.LoadTagEditor((TagHierarchyNode) recentEditor.EditorParamater);
                    break;

                case ScriptEditorName:
                    cachePage.ViewModel.LoadScriptEditor((IScriptFile) recentEditor.EditorParamater);
                    break;
            }
            RemoveRecentEditor(recentEditor);
        }

        public void RemoveRecentEditor(RecentEditor recentEditor)
        {
            RecentEditors.Remove(recentEditor);
        }

        #endregion
    }
}