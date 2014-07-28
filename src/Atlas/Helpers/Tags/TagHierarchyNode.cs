using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Atlas.Models;
using Blamite.Blam;
using Blamite.Util;

namespace Atlas.Helpers.Tags
{
	/// <summary>
	/// A node in a tag hierarchy.
	/// </summary>
	public class TagHierarchyNode : Base
	{
		private const string SoundClassMagic = "snd!";
		private const string ModelClassMagic = "mode";

		private string _name;
		private string _suffix;

		private readonly IComparer<TagHierarchyNode> _sortComparison = new NodeSortComparison();

		/// <summary>
		/// Initializes a new instance of the <see cref="TagHierarchyNode"/> class.
		/// The node will represent a folder with no associated tag or tag class.
		/// </summary>
		/// <param name="name">The node's name.</param>
		/// <param name="suffix">The node's suffix. Can be <c>null</c>.</param>
		/// <param name="cacheFile">The cache file the hierarchy belongs to. Can be <c>null</c>.</param>
		/// <param name="path">The node's file path. Can be <c>null</c>.</param>
		public TagHierarchyNode(string name, string suffix, ICacheFile cacheFile, string path)
		{
			FullPath = path;
			CacheFile = cacheFile;
			Name = name;
			Suffix = suffix;
			Children = new ObservableCollection<TagHierarchyNode>();
			Children.CollectionChanged += Children_CollectionChanged; // Handles keeping track of a node's parent
			Editors = new ObservableCollection<CacheEditorNode>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TagHierarchyNode"/> class.
		/// The node will represent a tag.
		/// </summary>
		/// <param name="name">The node's name.</param>
		/// <param name="suffix">The node's suffix. Can be <c>null</c>.</param>
		/// <param name="cacheFile">The cache file the hierarchy belongs to. Can be <c>null</c>.</param>
		/// <param name="tag">The tag to associate with the node.</param>
		/// <param name="path">The node's file path. Can be <c>null</c>.</param>
		public TagHierarchyNode(string name, string suffix, ICacheFile cacheFile, ITag tag, string path)
			: this(name, suffix, cacheFile, path)
		{
			Tag = tag;
			TagClass = Tag.Class;

			CreateApplicableEditors();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TagHierarchyNode"/> class.
		/// The node will be a folder representing a tag class.
		/// </summary>
		/// <param name="name">The node's name.</param>
		/// <param name="suffix">The node's suffix. Can be <c>null</c>.</param>
		/// <param name="cacheFile">The cache file the hierarchy belongs to. Can be <c>null</c>.</param>
		/// <param name="tagClass">The tag class to associate with the node.</param>
		/// <param name="path">The node's file path. Can be <c>null</c>.</param>
		public TagHierarchyNode(string name, string suffix, ICacheFile cacheFile, ITagClass tagClass, string path)
			: this(name, suffix, cacheFile, path)
		{
			TagClass = tagClass;

			CreateApplicableEditors();
		}

		private void CreateApplicableEditors()
		{
			if (CacheFile == null || TagClass == null) return;
			var className = CharConstant.ToString(TagClass.Magic);

			if (className == ModelClassMagic &&
				CacheFile.ResourceMetaLoader.SupportsRenderModels)
				Editors.Add(new CacheEditorNode(CacheEditorType.Model));
			else if (className == SoundClassMagic &&
				CacheFile.ResourceMetaLoader.SupportsSounds)
				Editors.Add(new CacheEditorNode(CacheEditorType.Sound));
		}

		/// <summary>
		/// Gets or sets the node's name. (If it's a tag, it will include the class as an extention. ie; masterchief.bipd)
		/// </summary>
		public string Name
		{
			get { return _name; }
			set { SetField(ref _name, value); }
		}

		/// <summary>
		/// Gets or sets the node's suffix.
		/// </summary>
		public string Suffix
		{
			get { return _suffix; }
			set { SetField(ref _suffix, value); }
		}

		/// <summary>
		/// Gets whether or not the node has a valid suffix.
		/// </summary>
		public bool HasSuffix
		{
			get { return !string.IsNullOrEmpty(Suffix); }
		}

		/// <summary>
		/// Gets the full path (including tag name if applicable) of the node.
		/// </summary>
		public string FullPath { get; private set; }

		public ICacheFile CacheFile { get; private set; }

		/// <summary>
		/// Gets the tag associated with the node. Can be <c>null</c>.
		/// </summary>
		public ITag Tag { get; private set; }

		/// <summary>
		/// Gets the tag class associated with the node. Can be <c>null</c>.
		/// </summary>
		public ITagClass TagClass { get; private set; }

		/// <summary>
		/// Gets a list of child nodes of this node.
		/// </summary>
		public ObservableCollection<TagHierarchyNode> Children { get; private set; }

		/// <summary>
		/// TODO: name
		/// </summary>
		public ObservableCollection<CacheEditorNode> Editors { get; private set; }

		/// <summary>
		/// TODO: name
		/// </summary>
		public ObservableCollection<object> Babies
		{
			get
			{
				var babyNodes = new ObservableCollection<object>();

				foreach(var child in Children)
					babyNodes.Add(child);
				foreach(var editor in Editors)
					babyNodes.Add(editor);

				return babyNodes;
			}
		} 

		/// <summary>
		/// Gets the node's parent. Can be <c>null</c>.
		/// </summary>
		public TagHierarchyNode Parent { get; private set; }

		/// <summary>
		/// Gets whether or not this node represents a folder.
		/// </summary>
		public bool IsFolder
		{
			get
			{
				// If the node doesn't have a tag associated with it OR it has children, consider it to be a folder
				return (Children.Count > 0) || (Tag == null);
			}
		}

		/// <summary>
		/// Gets whether or not this node represents a tag.
		/// </summary>
		public bool IsTag
		{
			get { return (Tag != null); }
		}

		/// <summary>
		/// Gets whether or not this node represents a tag class.
		/// </summary>
		public bool IsTagClass
		{
			get { return (TagClass != null); }
		}

		/// <summary>
		/// Adds a child node to this node, sorting it by name and suffix.
		/// The other children of this node must be sorted as well.
		/// If a node with the same name and suffix is already present, nothing will happen.
		/// </summary>
		/// <param name="child">The child node to insert.</param>
		/// <returns>If the child was added, it will be returned, otherwise the already-existing node will be returned.</returns>
		public TagHierarchyNode InsertChildSorted(TagHierarchyNode child)
		{
			// Binary search the tree to determine where the node should be inserted
			var insertPos = ListSearching.BinarySearch(Children, child, _sortComparison);

			// If a node with the same name and suffix is already in the tree,
			// then just return that and don't do anything further
			if (insertPos >= 0)
				return Children[insertPos];

			// Insert the child before the next greater node
			insertPos = ~insertPos;
			Children.Insert(insertPos, child);
			return child;
		}

		/// <summary>
		/// Renames a child node by moving it, maintaining a sorted list of children.
		/// </summary>
		/// <param name="child">The child node.</param>
		/// <param name="newName">The new name for the node.</param>
		public void RenameChildSorted(TagHierarchyNode child, string newName)
		{
			if (child.Parent != this)
				throw new ArgumentException("The node is not a direct child node");

			// Create a temporary node to determine its new index in the list
			var temp = new TagHierarchyNode(newName, child.Suffix, CacheFile, child.FullPath.Replace(child.Name, newName));
			var newPos = ListSearching.BinarySearch(Children, temp, _sortComparison);
			if (newPos >= 0)
				throw new InvalidOperationException("A node with the new name already exists");

			// Because we used a temporary node to get the new position,
			// if the new position is greater than the current position, we have to subtract 1
			newPos = ~newPos;
			var oldPos = Children.IndexOf(child);
			if (newPos > oldPos)
				newPos--;

			// Rename and move the node
			child.Name = newName;
			Children.Move(oldPos, newPos);
		}

		/// <summary>
		/// Detaches this node from its parent.
		/// </summary>
		public void Detach()
		{
			if (Parent != null)
				Parent.Children.Remove(this);
			// No need to set Parent to null here because it will be done in the former parent's CollectionChanged handler
		}

		private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			// If nodes are added or removed from this node, update their Parent properties
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (TagHierarchyNode node in e.NewItems)
					{
						if (node.Parent != null)
							throw new InvalidOperationException("A tag hierarchy node cannot have more than one parent node.");
						node.Parent = this;
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					foreach (TagHierarchyNode node in e.OldItems)
						node.Parent = null;
					break;
			}
		}

		/// <summary>
		/// Sorts hierarchy nodes by name, suffix, and whether or not they represent tags.
		/// </summary>
		/// <seealso cref="InsertChildSorted"/>
		private class NodeSortComparison : IComparer<TagHierarchyNode>
		{
			public int Compare(TagHierarchyNode x, TagHierarchyNode y)
			{
				// Non-tag nodes come first
				if (!x.IsTag && y.IsTag)
					return -1;
				if (x.IsTag && !y.IsTag)
					return 1;

				// Then sort the nodes by name
				var nameCompare = String.Compare(x.Name, y.Name, StringComparison.Ordinal);
				if (nameCompare != 0)
					return nameCompare;

				// Then sort them by suffix, putting ones without suffixes at the bottom
				if (!x.HasSuffix && !y.HasSuffix)
					return 0;
				if (x.HasSuffix && !y.HasSuffix)
					return -1;
				if (!x.HasSuffix && y.HasSuffix)
					return 1;

				return String.Compare(x.Suffix, y.Suffix, StringComparison.Ordinal);
			}
		}
	}
}
