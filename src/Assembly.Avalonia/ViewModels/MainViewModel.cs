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
	}

	public sealed class MainViewModel : ObservableObject
	{
		private CacheSession? _session;
		private List<TagInfo> _allTags = new();
		private Dictionary<string, TagGroupInfo> _groupInfo = new();

		public MainViewModel()
		{
			EngineDatabaseService.Initialize();

			if (EngineDatabaseService.Error != null)
			{
				EngineStatus = "engine database: FAILED";
				EngineStatusOk = false;
				Message = EngineDatabaseService.Error;
				MessageTitle = "Blamite engine database failed to load";
			}
			else
			{
				EngineStatus = $"engine database: {EngineDatabaseService.EngineCount} engines";
				EngineStatusOk = true;
				MessageTitle = "No cache file open";
				Message = "Open a Halo cache file (.map) to browse its tags.";
			}

			PluginsAvailable = EngineDatabaseService.PluginsRoot != null;
		}

		// ---- environment ----
		public string EngineStatus { get; }
		public bool EngineStatusOk { get; }
		public bool PluginsAvailable { get; }

		public string RuntimeStatus =>
			$"{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}  |  " +
			$"{System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}";

		// ---- message / error surface ----
		private string _messageTitle = "";
		public string MessageTitle { get => _messageTitle; set => Set(ref _messageTitle, value); }

		private string _message = "";
		public string Message { get => _message; set => Set(ref _message, value); }

		private bool _messageIsError;
		public bool MessageIsError { get => _messageIsError; set => Set(ref _messageIsError, value); }

		private bool _hasMessage = true;
		public bool HasMessage { get => _hasMessage; set => Set(ref _hasMessage, value); }

		// ---- cache ----
		private bool _hasCache;
		public bool HasCache { get => _hasCache; set => Set(ref _hasCache, value); }

		private string _windowTitle = "Assembly";
		public string WindowTitle { get => _windowTitle; set => Set(ref _windowTitle, value); }

		public ObservableCollection<KeyValuePair<string, string>> CacheInfo { get; } = new();

		private bool _isSynthetic;
		public bool IsSynthetic { get => _isSynthetic; set => Set(ref _isSynthetic, value); }

		// ---- tag tree ----
		/// <summary>Top-level tree items: GroupNode in group mode, FolderNode/TagNode in folder mode.</summary>
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
			set { if (Set(ref _selectedTag, value)) LoadMeta(); }
		}

		// ---- meta ----
		public ObservableCollection<MetaFieldValue> MetaFields { get; } = new();

		private bool _hasMeta;
		public bool HasMeta { get => _hasMeta; set => Set(ref _hasMeta, value); }

		private string _metaStatus = "";
		public string MetaStatus { get => _metaStatus; set => Set(ref _metaStatus, value); }

		private string _tagHeader = "";
		public string TagHeader { get => _tagHeader; set => Set(ref _tagHeader, value); }

		private string _statusText = "Ready";
		public string StatusText { get => _statusText; set => Set(ref _statusText, value); }

		// ---- actions ----
		public async Task OpenAsync(string path)
		{
			StatusText = $"Opening {System.IO.Path.GetFileName(path)}...";
			MetaFields.Clear();
			HasMeta = false;
			TagHeader = "";

			var db = EngineDatabaseService.Database;
			if (db == null)
			{
				ShowError("Engine database unavailable", EngineDatabaseService.Error ?? "unknown");
				return;
			}

			CacheSession session;
			try
			{
				session = await Task.Run(() => CacheSession.Open(path, db));
			}
			catch (Exception ex)
			{
				Nodes.Clear();
				_allTags.Clear();
				_groupInfo.Clear();
				HasCache = false;
				ShowError("Could not open cache file", $"{path}\n\n{ex.Message}");
				StatusText = "Open failed";
				return;
			}

			_session?.Dispose();
			_session = session;

			_allTags = session.Groups.SelectMany(g => g.Tags).ToList();
			_groupInfo = session.Groups.ToDictionary(g => g.Magic, g => g);

			ApplyFilter();

			CacheInfo.Clear();
			void Info(string k, string v) => CacheInfo.Add(new KeyValuePair<string, string>(k, v));
			var c = session.Cache;
			Info("path", session.FilePath);
			Info("engine", session.Engine.Name);
			Info("build", c.BuildString);
			Info("internal name", c.InternalName);
			Info("scenario", c.ScenarioName);
			Info("type / generation", $"{c.Type} / {c.Engine}");
			Info("endianness", c.Endianness.ToString());
			Info("file size", $"{c.FileSize:N0} bytes");
			Info("tag names", c.FileNames != null ? "present" : "absent");
			Info("string IDs", $"{c.StringIDs?.Count ?? 0:N0}");
			Info("tag groups", $"{session.Groups.Count}");
			Info("tags", $"{session.TotalTags:N0} ({session.SkippedTags} skipped)");
			if (session.AmbiguousMatches > 1)
				Info("engine matches", $"{session.AmbiguousMatches} (first used)");

			IsSynthetic = (c.InternalName ?? "").Contains("SYNTHETIC", StringComparison.OrdinalIgnoreCase);

			HasCache = true;
			HasMessage = false;
			WindowTitle = $"{session.DisplayName} - Assembly";
			if (!PluginsAvailable)
				StatusText += "   (tag definitions not found - meta view unavailable)";
		}

		private void ApplyFilter()
		{
			Nodes.Clear();
			var q = _search.Trim();
			bool filtering = q.Length > 0;

			// Filter once, at the TagInfo level, then build whichever tree shape is selected.
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

			StatusText = filtering
				? $"{matches.Count:N0} tags match \"{q}\""
				: $"{_allTags.Count:N0} tags in {_groupInfo.Count} groups";
		}

		private void LoadMeta()
		{
			MetaFields.Clear();
			HasMeta = false;

			if (_selectedTag == null || _session == null)
			{
				TagHeader = "";
				MetaStatus = "";
				return;
			}

			var t = _selectedTag.Info;
			TagHeader = $"{t.Name}.{t.Group}";

			var values = _session.ReadMeta(t, out var status);
			MetaStatus = status;

			if (values == null) return;

			foreach (var v in values) MetaFields.Add(v);
			HasMeta = MetaFields.Count > 0;
		}

		/// <summary>Finds the first loaded tag whose name contains <paramref name="needle"/>.</summary>
		public TagNode? FindTag(string needle)
		{
			var hit = _allTags.FirstOrDefault(t => t.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
			return hit == null ? null : new TagNode(hit);
		}

		public void CloseCache()
		{
			_session?.Dispose();
			_session = null;
			_allTags.Clear();
			_groupInfo.Clear();
			Nodes.Clear();
			MetaFields.Clear();
			CacheInfo.Clear();
			HasCache = false;
			HasMeta = false;
			IsSynthetic = false;
			TagHeader = "";
			MetaStatus = "";
			WindowTitle = "Assembly";
			MessageTitle = "No cache file open";
			Message = "Open a Halo cache file (.map) to browse its tags.";
			MessageIsError = false;
			HasMessage = true;
			StatusText = "Ready";
		}

		private void ShowError(string title, string body)
		{
			MessageTitle = title;
			Message = body;
			MessageIsError = true;
			HasMessage = true;
		}
	}
}
