using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Assembly.MultiPlatform.Services;

namespace Assembly.MultiPlatform.ViewModels
{
	public class ObservableObject : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		protected void Raise([CallerMemberName] string? name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			Raise(name);
			return true;
		}
	}

	/// <summary>A tag group node in the browser tree.</summary>
	public sealed class GroupNode : ObservableObject
	{
		public GroupNode(TagGroupInfo info, IEnumerable<TagNode> tags)
		{
			Info = info;
			Tags = new ObservableCollection<TagNode>(tags);
		}

		public TagGroupInfo Info { get; }
		public ObservableCollection<TagNode> Tags { get; }

		public string Magic => Info.Magic;
		public string Description => Info.Description;
		public string CountLabel => Tags.Count.ToString();

		private bool _isExpanded;
		public bool IsExpanded { get => _isExpanded; set => Set(ref _isExpanded, value); }
	}

	/// <summary>A single tag in the browser tree.</summary>
	public sealed class TagNode
	{
		public TagNode(TagInfo info, string? displayName = null)
		{
			Info = info;
			Name = displayName ?? info.Name;
		}

		public TagInfo Info { get; }

		/// <summary>Leaf label: the full path in group view, just the file name in folder view.</summary>
		public string Name { get; }

		/// <summary>Leaf nodes never expand; present so the shared TreeViewItem style binds cleanly.</summary>
		public bool IsExpanded { get; set; }

		public string Group => Info.Group;
		public string OffsetLabel => $"0x{Info.Offset:X8}";
		public string SourceName => Info.SourceName;
	}

	public sealed class MainViewModel : ObservableObject
	{
		private readonly TagNamespace _namespace = new();
		private List<TagInfo> _allTags = new();
		private Dictionary<string, TagGroupInfo> _groupInfo = new();

		public MainViewModel()
		{
			EngineDatabaseService.Initialize();

			if (EngineDatabaseService.Error != null)
			{
				EngineStatus = "engine database: FAILED";
				EngineStatusOk = false;
				Log.Error("engine database failed to load: " + EngineDatabaseService.Error);
			}
			else
			{
				EngineStatus = $"engine database: {EngineDatabaseService.EngineCount} engines";
				EngineStatusOk = true;
				Log.Info($"engine database loaded: {EngineDatabaseService.EngineCount} engines, plugins at {EngineDatabaseService.PluginsRoot ?? "(not found)"}");
			}

			PluginsAvailable = EngineDatabaseService.PluginsRoot != null;
			_namespace.Log += Log.Info;

			MessageTitle = "Nothing mounted";
			Message = "Open a cache file, a folder, or a zip to mount a tag namespace.\n\n" +
			          "A Halo cache is not always one file: Campaign Evolved mods ship as N separate " +
			          "containers (one tag each), and the game's own Paks folder holds dozens. Opening " +
			          "a folder or a zip mounts every cache file found inside as one namespace, exactly " +
			          "like a single file does.";
		}

		// ---- environment ----
		public string EngineStatus { get; }
		public bool EngineStatusOk { get; }
		public bool PluginsAvailable { get; }
		public LogService Log { get; } = new();

		public string RuntimeStatus =>
			$"{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}  |  " +
			$"{System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}";

		// ---- message / error surface (shown when nothing is mounted, or on error) ----
		private string _messageTitle = "";
		public string MessageTitle { get => _messageTitle; set => Set(ref _messageTitle, value); }

		private string _message = "";
		public string Message { get => _message; set => Set(ref _message, value); }

		private bool _messageIsError;
		public bool MessageIsError { get => _messageIsError; set => Set(ref _messageIsError, value); }

		private bool _hasMessage = true;
		public bool HasMessage { get => _hasMessage; set => Set(ref _hasMessage, value); }

		// ---- mounted namespace ----
		public ObservableCollection<MountedSource> Sources => _namespace.Sources;
		public bool HasAnySource => _namespace.HasAnySource;
		public bool HasMultipleSources => _namespace.HasMultipleSources;

		/// <summary>Read-only view over the flattened tag list, for the command palette's tag
		/// search (Services/TagSearchIndex.cs) to build its own lightweight index from without
		/// needing its own reference to <see cref="TagNamespace" />. Same list <see cref="ApplyFilter" />
		/// already filters the tree from - not a copy, just not writable from outside.</summary>
		public IReadOnlyList<TagInfo> AllTagsSnapshot => _allTags;

		/// <summary>Bumped every time <see cref="_allTags" /> is rebuilt (a mount or unmount).
		/// The palette rebuilds its tag search index only when this changes, rather than on every
		/// open - see CommandPalette.EnsureTagIndexFresh.</summary>
		public int TagsVersion { get; private set; }

		private string _windowTitle = "Assembly";
		public string WindowTitle { get => _windowTitle; set => Set(ref _windowTitle, value); }

		// ---- tag tree ----
		public ObservableCollection<object> Nodes { get; } = new();

		public string[] TreeModes { get; } = { "Tag group", "Folder" };

		private string _treeMode = "Tag group";
		public string TreeMode
		{
			get => _treeMode;
			set { if (Set(ref _treeMode, value)) ApplyFilter(); }
		}

		private bool IsFolderMode => _treeMode == "Folder";

		private string _search = "";
		public string Search
		{
			get => _search;
			set { if (Set(ref _search, value)) ApplyFilter(); }
		}

		private TagNode? _selectedTag;
		public TagNode? SelectedTag
		{
			// Fire-and-forget: a property setter can't be async, and OpenTagAsync already reports
			// failure through Log/StatusText rather than an exception the caller would need to
			// observe (see its own try/catch), so there is nothing useful to await here anyway.
			get => _selectedTag;
			set { if (Set(ref _selectedTag, value) && value != null) _ = OpenTagAsync(value); }
		}

		// ---- documents (tabs) ----
		public ObservableCollection<TagDocumentViewModel> Documents { get; } = new();

		private TagDocumentViewModel? _activeDocument;
		public TagDocumentViewModel? ActiveDocument
		{
			get => _activeDocument;
			set => Set(ref _activeDocument, value);
		}

		public bool HasDocuments => Documents.Count > 0;

		// ---- tag-open loading state ----
		//
		// TagDocumentViewModel's constructor does real, non-trivial work for a Campaign Evolved
		// tag - parsing the whole self-describing payload (FifthGenTagFile), ~400ms measured
		// against b30-scenario - and used to run it synchronously on the UI thread the moment a
		// tag was clicked. OpenTagAsync below moves that onto a background thread; these two
		// properties are what the field table binds to show something other than a frozen window
		// while it runs (see the loading overlay in MainWindow.axaml's field-table Grid).

		private bool _isOpeningTag;
		public bool IsOpeningTag { get => _isOpeningTag; private set => Set(ref _isOpeningTag, value); }

		private string _openingTagLabel = "";
		public string OpeningTagLabel { get => _openingTagLabel; private set => Set(ref _openingTagLabel, value); }

		// Serializes tag opens rather than letting them run concurrently: TagDocumentViewModel's
		// constructor reads through the owning CacheSession (GetSchema's plugin cache, the shared
		// FileStreamManager), none of which is written to expect concurrent callers. One open at a
		// time keeps that honest without needing to touch CacheSession (owned elsewhere) to add
		// locking of its own; a second click while one is in flight just waits its turn instead of
		// racing it.
		private readonly SemaphoreSlim _openGate = new(1, 1);

		// ---- back/forward navigation history ----
		//
		// Tracks tag keys (TagDocumentViewModel.TagKey) rather than document instances, so a
		// history entry survives the tab it pointed at being closed - going back to it re-opens the
		// tag from the namespace instead of silently landing nowhere. Only OpenTagAsync's own two
		// call sites (tree selection, tag-reference navigation) push new entries; clicking an
		// already-open tab directly (OnTabClick, in the tab strip - not this file's to change) does
		// not, so switching tabs doesn't spam the history the way every browser also doesn't.
		private readonly List<string> _history = new();
		private int _historyIndex = -1;

		public bool CanGoBack => _historyIndex > 0;
		public bool CanGoForward => _historyIndex >= 0 && _historyIndex < _history.Count - 1;

		private void PushHistory(TagDocumentViewModel doc)
		{
			if (_historyIndex >= 0 && _historyIndex < _history.Count && _history[_historyIndex] == doc.TagKey)
				return; // re-activating the current history entry isn't a new navigation

			if (_historyIndex < _history.Count - 1)
				_history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);

			_history.Add(doc.TagKey);
			_historyIndex = _history.Count - 1;
			Raise(nameof(CanGoBack));
			Raise(nameof(CanGoForward));
		}

		public void GoBack()
		{
			if (!CanGoBack) return;
			_historyIndex--;
			NavigateToHistoryEntry();
		}

		public void GoForward()
		{
			if (!CanGoForward) return;
			_historyIndex++;
			NavigateToHistoryEntry();
		}

		private void NavigateToHistoryEntry()
		{
			Raise(nameof(CanGoBack));
			Raise(nameof(CanGoForward));
			if (_historyIndex < 0 || _historyIndex >= _history.Count) return;

			string key = _history[_historyIndex];
			var open = Documents.FirstOrDefault(d => d.TagKey == key);
			if (open != null) { ActiveDocument = open; return; }

			// The tab was closed since this entry was recorded - re-open the same tag rather than
			// landing on nothing, unless the source it came from was unmounted entirely, in which
			// case there is nothing left to reopen and this entry is just dead weight.
			string[] parts = key.Split("::", 3);
			var tag = parts.Length == 3
				? _allTags.FirstOrDefault(t => t.SourceName == parts[0] && t.Group == parts[1] && t.Name == parts[2])
				: null;

			if (tag != null) { _ = OpenTagAsync(new TagNode(tag), recordHistory: false); return; }

			_history.RemoveAt(_historyIndex);
			if (_historyIndex >= _history.Count) _historyIndex = _history.Count - 1;
			NavigateToHistoryEntry();
		}

		// ---- panes ----
		private bool _showTagTree = true;
		public bool ShowTagTree { get => _showTagTree; set => Set(ref _showTagTree, value); }

		private bool _showValueSidebar = true;
		public bool ShowValueSidebar { get => _showValueSidebar; set => Set(ref _showValueSidebar, value); }

		private bool _showConsole = true;
		public bool ShowConsole { get => _showConsole; set => Set(ref _showConsole, value); }

		private string _statusText = "Ready";
		public string StatusText { get => _statusText; set => Set(ref _statusText, value); }

		// ---- mount actions ----
		public async Task OpenFileAsync(string path)
		{
			var db = EngineDatabaseService.Database;
			if (db == null) { ShowError("Engine database unavailable", EngineDatabaseService.Error ?? "unknown"); return; }

			StatusText = $"Mounting {System.IO.Path.GetFileName(path)}...";
			var source = await Task.Run(() => _namespace.MountFile(path, db));
			AfterMount(source);
		}

		public async Task OpenFolderAsync(string path)
		{
			var db = EngineDatabaseService.Database;
			if (db == null) { ShowError("Engine database unavailable", EngineDatabaseService.Error ?? "unknown"); return; }

			StatusText = $"Scanning folder {System.IO.Path.GetFileName(path)}...";
			var source = await Task.Run(() => _namespace.MountFolder(path, db));
			AfterMount(source);
		}

		public async Task OpenZipAsync(string path)
		{
			var db = EngineDatabaseService.Database;
			if (db == null) { ShowError("Engine database unavailable", EngineDatabaseService.Error ?? "unknown"); return; }

			StatusText = $"Extracting {System.IO.Path.GetFileName(path)}...";
			var source = await Task.Run(() => _namespace.MountZip(path, db));
			AfterMount(source);
		}

		private void AfterMount(MountedSource source)
		{
			foreach (var (file, reason) in source.Failures)
				Log.Warn($"  \"{file}\": {reason}");

			RefreshAggregate();

			if (source.Sessions.Count == 0 && source.Failures.Count > 0)
				ShowError($"Could not mount \"{source.DisplayName}\"", source.Failures.First().Reason);
			else
				MessageIsError = false;
		}

		private void RefreshAggregate()
		{
			_allTags = _namespace.AllTags.ToList();
			_groupInfo = _namespace.Sessions.SelectMany(s => s.Groups)
				.GroupBy(g => g.Magic)
				.ToDictionary(g => g.Key, g => g.First());
			TagsVersion++;

			ApplyFilter();
			Raise(nameof(HasAnySource));
			Raise(nameof(HasMultipleSources));
			Raise(nameof(Sources));

			HasMessage = !_namespace.HasAnySource;

			WindowTitle = _namespace.HasAnySource
				? $"{_namespace.Sources.Count} source{(_namespace.Sources.Count == 1 ? "" : "s")}, {_namespace.TotalTags:N0} tags - Assembly"
				: "Assembly";

			StatusText = $"{_allTags.Count:N0} tags across {_namespace.Sources.Count} source(s) in {_groupInfo.Count} groups";
			if (!PluginsAvailable)
				StatusText += "   (tag definitions not found - meta view unavailable)";
		}

		public void CloseAll()
		{
			foreach (var doc in Documents.ToList()) CloseDocument(doc);
			_namespace.UnmountAll();
			_allTags.Clear();
			_groupInfo.Clear();
			Nodes.Clear();
			TagsVersion++;
			Raise(nameof(HasAnySource));
			Raise(nameof(HasMultipleSources));
			Raise(nameof(Sources));
			WindowTitle = "Assembly";
			MessageTitle = "Nothing mounted";
			Message = "Open a cache file, a folder, or a zip to mount a tag namespace.";
			MessageIsError = false;
			HasMessage = true;
			StatusText = "Ready";
			Log.Info("unmounted everything");
		}

		public void Unmount(MountedSource source)
		{
			// Close any open tabs that belonged to this source before dropping its sessions.
			foreach (var doc in Documents.Where(d => GetOwnerSession(d) is { } owner && source.Sessions.Contains(owner)).ToList())
				CloseDocument(doc);

			_namespace.Unmount(source);
			if (!_namespace.HasAnySource) { CloseAll(); return; }

			RefreshAggregate();
			Raise(nameof(HasAnySource));
			Raise(nameof(HasMultipleSources));
			Raise(nameof(Sources));
		}

		private static CacheSession? GetOwnerSession(TagDocumentViewModel doc) => doc.Tag.Owner;

		private void ApplyFilter()
		{
			Nodes.Clear();
			var q = _search.Trim();
			bool filtering = q.Length > 0;

			var matches = _allTags.Where(t =>
				!filtering ||
				t.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
				t.Group.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

			if (IsFolderMode)
			{
				foreach (var node in FolderNode.Build(matches, expandAll: filtering))
					Nodes.Add(node);
			}
			else
			{
				foreach (var g in matches.GroupBy(t => t.Group).OrderBy(g => g.Key, StringComparer.Ordinal))
				{
					var info = _groupInfo.TryGetValue(g.Key, out var gi)
						? gi
						: new TagGroupInfo(g.Key, "unknown");

					Nodes.Add(new GroupNode(info,
						g.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).Select(t => new TagNode(t)))
					{
						IsExpanded = filtering
					});
				}
			}

			if (_namespace.HasAnySource)
				StatusText = filtering
					? $"{matches.Count:N0} tags match \"{q}\""
					: $"{_allTags.Count:N0} tags in {_groupInfo.Count} groups";
		}

		// ---- documents ----

		/// <summary>
		///     Opens a tag as a new tab, or activates its existing one. The construction itself -
		///     <see cref="TagDocumentViewModel" />'s constructor, which for a Campaign Evolved tag
		///     means <c>new FifthGenTagFile(payload)</c> - runs on a thread-pool thread via
		///     <see cref="Task.Run(Func{TagDocumentViewModel})" />; everything that touches
		///     UI-bound collections (<see cref="Documents" />, <see cref="ActiveDocument" />, the
		///     new document's own <c>Rows</c> once it's handed back) happens after the await, back
		///     on whatever thread called this - the UI thread for every real caller. The freshly
		///     constructed <see cref="TagDocumentViewModel" /> itself is safe to build off-thread
		///     precisely because nothing is bound to it yet: its own <c>Rows</c> collection is
		///     populated by <c>Rebuild()</c> inside the constructor, but no ListBox observes it
		///     until <see cref="Documents" />.Add below runs on the UI thread.
		/// </summary>
		/// <param name="recordHistory">False when this open is itself a back/forward navigation
		/// re-opening a closed tab - that must not push a new history entry on top of the one
		/// already being navigated to.</param>
		private async Task OpenTagAsync(TagNode node, bool recordHistory = true)
		{
			string key = $"{node.Info.SourceName}::{node.Info.Group}::{node.Info.Name}";
			var existing = Documents.FirstOrDefault(d => d.TagKey == key);
			if (existing != null)
			{
				ActiveDocument = existing;
				if (recordHistory) PushHistory(existing);
				return;
			}

			IsOpeningTag = true;
			OpeningTagLabel = $"{node.Info.Name}.{node.Info.Group}";
			StatusText = $"Opening {OpeningTagLabel}...";

			await _openGate.WaitAsync();
			try
			{
				// TagDocumentViewModel still gets a real Log reference (Save(), later, logs directly
				// through it - that always runs on the UI thread already) but parsing itself never
				// writes through it - see PendingLogMessages' remarks for why not, given this runs
				// inside Task.Run. Replay whatever parsing collected now that we're back on this
				// thread, which is the UI thread for every real caller.
				var doc = await Task.Run(() => new TagDocumentViewModel(node.Info, Log));
				foreach (var (level, message) in doc.PendingLogMessages)
				{
					switch (level)
					{
						case LogLevel.Warn: Log.Warn(message); break;
						case LogLevel.Error: Log.Error(message); break;
						default: Log.Info(message); break;
					}
				}

				Documents.Add(doc);
				ActiveDocument = doc;
				Raise(nameof(HasDocuments));
				Log.Info($"opened {doc.HeaderTitle}  ({doc.SchemaStatus})");
				StatusText = $"Opened {doc.HeaderTitle}";
				if (recordHistory) PushHistory(doc);
			}
			catch (Exception ex)
			{
				Log.Error($"failed to open {node.Info.Name}.{node.Info.Group}: {ex.Message}");
				StatusText = $"Failed to open {node.Info.Name}.{node.Info.Group}: {ex.Message}";
			}
			finally
			{
				_openGate.Release();
				IsOpeningTag = false;
			}
		}

		/// <summary>
		///     Follows a tag-reference row's target (see <see cref="TagRefTarget" />), searching the
		///     whole mounted namespace rather than just the currently open tag's own cache - a
		///     Campaign Evolved reference names a path that can legitimately resolve to a tag mounted
		///     from a different sibling container than the one carrying the reference. When the
		///     target isn't mounted, this says so through <see cref="StatusText" /> and the console
		///     rather than doing nothing, which is what silently failing to find a match would look
		///     like to a user with no other signal.
		/// </summary>
		public void NavigateToTagReference(TagRefTarget target)
		{
			if (!target.IsFollowable)
			{
				StatusText = "This reference is null - there is nothing to open.";
				return;
			}

			TagInfo? hit = target.SameSourceHint != null
				? _allTags.FirstOrDefault(t =>
					string.Equals(t.SourceName, target.SameSourceHint, StringComparison.OrdinalIgnoreCase) &&
					string.Equals(t.Group, target.Group, StringComparison.OrdinalIgnoreCase) &&
					string.Equals(t.Name, target.Name, StringComparison.OrdinalIgnoreCase))
				: null;

			hit ??= _allTags.FirstOrDefault(t =>
				string.Equals(t.Group, target.Group, StringComparison.OrdinalIgnoreCase) &&
				string.Equals(t.Name, target.Name, StringComparison.OrdinalIgnoreCase));

			if (hit == null)
			{
				string where = target.SameSourceHint != null ? $" in \"{target.SameSourceHint}\"" : " in the mounted namespace";
				StatusText = $"\"{target.Name}.{target.Group}\" is not mounted{where} - open its container first.";
				Log.Warn($"reference navigation: {StatusText}");
				return;
			}

			_ = OpenTagAsync(new TagNode(hit));
		}

		public void CloseDocument(TagDocumentViewModel doc)
		{
			var idx = Documents.IndexOf(doc);
			if (idx < 0) return;
			Documents.RemoveAt(idx);
			if (ActiveDocument == doc)
				ActiveDocument = Documents.Count > 0 ? Documents[Math.Min(idx, Documents.Count - 1)] : null;
			Raise(nameof(HasDocuments));
		}

		public void SaveActive()
		{
			if (ActiveDocument == null) return;
			var (ok, message) = ActiveDocument.Save();
			StatusText = message;
			if (!ok) Log.Error(message);
		}

		/// <summary>Finds the first loaded tag whose name contains <paramref name="needle"/>. Used by the headless screenshot harness.</summary>
		public TagNode? FindTag(string needle)
		{
			var hit = _allTags.FirstOrDefault(t => t.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
			return hit == null ? null : new TagNode(hit);
		}

		private void ShowError(string title, string body)
		{
			MessageTitle = title;
			Message = body;
			MessageIsError = true;
			HasMessage = true;
			Log.Error($"{title}: {body}");
		}
	}
}
