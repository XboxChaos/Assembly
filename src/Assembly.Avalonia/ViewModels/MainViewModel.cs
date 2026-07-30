using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Assembly.Avalonia.Services;

namespace Assembly.Avalonia.ViewModels
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
			get => _selectedTag;
			set { if (Set(ref _selectedTag, value) && value != null) OpenTag(value); }
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
			foreach (var doc in Documents.Where(d => source.Sessions.Contains(GetOwnerSession(d))).ToList())
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
		private void OpenTag(TagNode node)
		{
			var existing = Documents.FirstOrDefault(d => d.TagKey ==
				$"{node.Info.SourceName}::{node.Info.Group}::{node.Info.Name}");
			if (existing != null)
			{
				ActiveDocument = existing;
				return;
			}

			try
			{
				var doc = new TagDocumentViewModel(node.Info, Log);
				Documents.Add(doc);
				ActiveDocument = doc;
				Raise(nameof(HasDocuments));
				Log.Info($"opened {doc.HeaderTitle}  ({doc.SchemaStatus})");
			}
			catch (Exception ex)
			{
				Log.Error($"failed to open {node.Info.Name}.{node.Info.Group}: {ex.Message}");
			}
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
