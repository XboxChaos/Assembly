using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Blamite.Blam;

namespace Atlas.Helpers.Tags
{
	/// <summary>
	/// A hierarchy of tags grouped into folders.
	/// </summary>
	public abstract class TagHierarchy
	{
		private readonly List<TagHierarchyNode> _tagNodes = new List<TagHierarchyNode>(); // In order by datum index, can have null values

		/// <summary>
		/// Initializes a new instance of the <see cref="TagHierarchy"/> class.
		/// </summary>
		protected TagHierarchy()
		{
			Root = new TagHierarchyNode("(root)", null, null, "");
		}

		/// <summary>
		/// Gets the node at the root of the hierarchy.
		/// </summary>
		/// <seealso cref="Nodes"/>
		public TagHierarchyNode Root { get; private set; }

		/// <summary>
		/// Gets the nodes belonging to the root node of the hierarchy.
		/// </summary>
		/// <seealso cref="Root"/>
		public ObservableCollection<TagHierarchyNode> Nodes
		{
			get { return Root.Children; }
		}

		/// <summary>
		/// Adds a tag to the hierarchy if it is not already present.
		/// </summary>
		/// <param name="tag">The tag to add.</param>
		/// <param name="name">The full name of the tag.</param>
		/// <returns>The node that was created for the tag if it was added, otherwise the node that already exists for the tag.</returns>
		public TagHierarchyNode AddTag(ITag tag, string name)
		{
			if (tag == null || tag.MetaLocation == null || tag.Index == DatumIndex.Null)
				throw new ArgumentException("Invalid tag");

			var result = AddTagImpl(tag, name);
			RegisterTagNode(result);
			return result;
		}

		/// <summary>
		/// Adds a tag to the hierarchy if it is not already present.
		/// </summary>
		/// <remarks>
		/// Hierarchy-specific logic for <see cref="AddTag"/> should go here.
		/// </remarks>
		/// <param name="tag">The tag to add.</param>
		/// <param name="name">The full name of the tag.</param>
		/// <returns>The node that was created for the tag if it was added, otherwise the node that already exists for the tag.</returns>
		protected abstract TagHierarchyNode AddTagImpl(ITag tag, string name);

		/// <summary>
		/// Given a datum index for a tag, finds the node that represents the tag.
		/// </summary>
		/// <param name="index">The datum index of the tag to search for.</param>
		/// <returns>If found, the node representing the tag, otherwise <c>null</c>.</returns>
		public TagHierarchyNode FindNodeByTagIndex(DatumIndex index)
		{
			if (index == DatumIndex.Null)
				return null;
			return index.Index >= _tagNodes.Count ? null : _tagNodes[index.Index];
		}

		/// <summary>
		/// Given a tag, finds the node that represents it.
		/// </summary>
		/// <param name="tag">The tag to search for.</param>
		/// <returns>If found, the node representing the tag, otherwise <c>null</c>.</returns>
		public TagHierarchyNode FindNodeByTag(ITag tag)
		{
			return (tag != null) ? FindNodeByTagIndex(tag.Index) : null;
		}

		/// <summary>
		/// Removes a tag from the hierarchy if it is present.
		/// </summary>
		/// <param name="tag">The tag to remove.</param>
		public void RemoveTag(ITag tag)
		{
			var node = FindNodeByTag(tag);
			RemoveTagImpl(tag, node);
			UnRegisterTagNode(node);
		}

		/// <summary>
		/// Removes a tag from the hierarchy if it is present.
		/// </summary>
		/// <remarks>
		/// Hierarchy-specific logic for <see cref="RemoveTag"/> should go here.
		/// </remarks>
		/// <param name="tag">The tag to remove.</param>
		/// <param name="node">The tag's node.</param>
		protected abstract void RemoveTagImpl(ITag tag, TagHierarchyNode node);

		/// <summary>
		/// Gets whether or not the hierarchy supports renaming a given node.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <returns><c>true</c> if the hierarchy supports renaming the node.</returns>
		public abstract bool CanRenameNode(TagHierarchyNode node);

		/// <summary>
		/// Renames a node. The hierarchy must support renaming it.
		/// </summary>
		/// <param name="node">The node to rename.</param>
		/// <param name="newName">The new name to give the node.</param>
		/// <seealso cref="CanRenameNode"/>
		public abstract void RenameNode(TagHierarchyNode node, string newName);

		/// <summary>
		/// Builds a list of tag names from the tag hierarchy.
		/// Positions in the list will correspond to positions in the cache file's tag list.
		/// </summary>
		/// <returns>The tag name list that was built.</returns>
		public abstract List<string> GetTagNames();

		/// <summary>
		/// Registers a newly-added tag node with the hierarchy so that it can be found by its tag's datum index.
		/// </summary>
		/// <param name="node">The tag node to register.</param>
		private void RegisterTagNode(TagHierarchyNode node)
		{
			if (node == null || node.Tag == null || node.Tag.Index == DatumIndex.Null)
				return;

			// Add null entries to the list until we have room for the node
			// (using a list assumes that tag indices will be densely spaced).
			var index = node.Tag.Index.Index;
			while (index >= _tagNodes.Count)
				_tagNodes.Add(null);

			// Register it
			_tagNodes[index] = node;
		}

		/// <summary>
		/// Unregisters a tag node with the hierarchy so that it can no longer be found by its tag's datum index.
		/// </summary>
		/// <param name="node">The tag node to unregister.</param>
		private void UnRegisterTagNode(TagHierarchyNode node)
		{
			if (node == null || node.Tag == null || node.Tag.Index == DatumIndex.Null)
				return;

			// Just set the node's entry in the list to null
			var index = node.Tag.Index.Index;
			if (index < _tagNodes.Count)
				_tagNodes[index] = null;
		}
	}
}
