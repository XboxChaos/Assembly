using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Assembly.MultiPlatform.Services;

namespace Assembly.MultiPlatform.ViewModels
{
	/// <summary>
	///     A directory in the tag-name hierarchy. Halo tag names are path-like
	///     ("objects/characters/masterchief/masterchief"), so the browser can present them
	///     as a folder tree instead of a flat list grouped by 4-char tag group.
	/// </summary>
	public sealed class FolderNode : ObservableObject
	{
		public FolderNode(string name) => Name = name;

		public string Name { get; }

		/// <summary>Sub-folders first, then tags; both live here so the tree can nest.</summary>
		public ObservableCollection<object> Children { get; } = new();

		private bool _isExpanded;
		public bool IsExpanded { get => _isExpanded; set => Set(ref _isExpanded, value); }

		public int TagCount { get; private set; }
		public string CountLabel => TagCount.ToString();

		/// <summary>
		///     Builds a folder tree from tag names. Returns the top-level items, which may be
		///     a mix of folders and tags that had no folder component.
		/// </summary>
		public static List<object> Build(IEnumerable<TagInfo> tags, bool expandAll)
		{
			var root = new FolderNode("");
			var index = new Dictionary<string, FolderNode> { [""] = root };

			foreach (var tag in tags)
			{
				var name = tag.Name ?? "";
				var slash = name.LastIndexOf('/');
				var dir = slash < 0 ? "" : name[..slash];
				var leaf = slash < 0 ? name : name[(slash + 1)..];

				EnsureFolder(index, root, dir).Children.Add(new TagNode(tag, leaf));
			}

			CountAndSort(root, expandAll);
			return root.Children.ToList();
		}

		private static FolderNode EnsureFolder(Dictionary<string, FolderNode> index, FolderNode root, string path)
		{
			if (index.TryGetValue(path, out var existing)) return existing;

			var slash = path.LastIndexOf('/');
			var parentPath = slash < 0 ? "" : path[..slash];
			var name = slash < 0 ? path : path[(slash + 1)..];

			var parent = EnsureFolder(index, root, parentPath);
			var node = new FolderNode(name);
			parent.Children.Add(node);
			index[path] = node;
			return node;
		}

		private static void CountAndSort(FolderNode node, bool expandAll)
		{
			foreach (var child in node.Children.OfType<FolderNode>())
				CountAndSort(child, expandAll);

			var folders = node.Children.OfType<FolderNode>()
				.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
			var tags = node.Children.OfType<TagNode>()
				.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();

			node.Children.Clear();
			foreach (var f in folders) node.Children.Add(f);
			foreach (var t in tags) node.Children.Add(t);

			node.TagCount = tags.Count + folders.Sum(f => f.TagCount);
			node.IsExpanded = expandAll;
		}

		public void Recount()
		{
			TagCount = Children.OfType<TagNode>().Count()
			           + Children.OfType<FolderNode>().Sum(f => f.TagCount);
		}
	}
}
