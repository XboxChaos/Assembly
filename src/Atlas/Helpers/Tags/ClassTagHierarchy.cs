using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.Blam;
using Blamite.Util;

namespace Atlas.Helpers.Tags
{
	/// <summary>
	/// A tag hierarchy that sorts tags into folders based upon their classes.
	/// </summary>
	class ClassTagHierarchy : TagHierarchy
	{
		private ICacheFile _cacheFile;
		private Dictionary<ITagClass, TagHierarchyNode> _nodesByClass;

		/// <summary>
		/// Initializes a new instance of the <see cref="ClassTagHierarchy"/> class.
		/// </summary>
		/// <param name="cacheFile">The cache file to use.</param>
		public ClassTagHierarchy(ICacheFile cacheFile)
		{
			_cacheFile = cacheFile;
			BuildClassFolders();
		}

		/// <summary>
		/// Adds a tag to the hierarchy if it is not already present.
		/// </summary>
		/// <param name="tag">The tag to add.</param>
		/// <param name="name">The full name of the tag.</param>
		/// <returns>
		/// The node that was created for the tag if it was added, otherwise the node that already exists for the tag.
		/// </returns>
		/// <exception cref="System.InvalidOperationException">Tag does not have a tag class</exception>
		/// <remarks>
		/// Hierarchy-specific logic for <see cref="AddTag" /> should go here.
		/// </remarks>
		protected override TagHierarchyNode AddTagImpl(ITag tag, string name)
		{
			if (tag.Class == null)
				throw new InvalidOperationException("Tag does not have a tag class");

			// Find the class node to put the tag under
			var classNode = _nodesByClass[tag.Class];

			// Insert the node
			var node = new TagHierarchyNode(name, null, _cacheFile, tag, null);
			return classNode.InsertChildSorted(node);
		}

		/// <summary>
		/// Removes a tag from the hierarchy if it is present.
		/// </summary>
		/// <param name="tag">The tag to remove.</param>
		/// <param name="node">The tag's node.</param>
		/// <remarks>
		/// Hierarchy-specific logic for <see cref="RemoveTag" /> should go here.
		/// </remarks>
		protected override void RemoveTagImpl(ITag tag, TagHierarchyNode node)
		{
			node.Detach();
		}

		/// <summary>
		/// Builds the class folders.
		/// </summary>
		private void BuildClassFolders()
		{
			// Add the nodes to a separate list so they can be sorted easily
			var folderList = new List<TagHierarchyNode>();
			folderList.AddRange(_cacheFile.TagClasses.Where(IsClassUsed).Select(BuildClassNode));
			folderList.Sort((a, b) => a.Name.CompareTo(b.Name));

			// Build the lookup dictionary
			_nodesByClass = folderList.ToDictionary((n) => n.TagClass);

			// Now add the folders to the hierarchy
			Nodes.Clear();
			foreach (var folder in folderList)
				Nodes.Add(folder);
		}

		/// <summary>
		/// Determines whether a tag class contains any tags in the cache file.
		/// </summary>
		/// <param name="tagClass">The tag class to check.</param>
		/// <returns><c>true</c> if at least one tag is a member of the class.</returns>
		private bool IsClassUsed(ITagClass tagClass)
		{
			return _cacheFile.Tags.Any((tag) => (tag != null && tag.Class == tagClass));
		}

		/// <summary>
		/// Creates a node for a tag class.
		/// </summary>
		/// <param name="tagClass">The tag class.</param>
		/// <returns>The created node.</returns>
		private TagHierarchyNode BuildClassNode(ITagClass tagClass)
		{
			// Get the name by converting the class's magic value into a string
			var name = CharConstant.ToString(tagClass.Magic);

			// Get the suffix by resolving its description stringID if it has one
			string suffix = null;
			if (tagClass.Description != StringID.Null)
				suffix = _cacheFile.StringIDs.GetString(tagClass.Description);
			if (suffix != null)
				suffix = " - " + suffix;

			return new TagHierarchyNode(name, suffix, _cacheFile, tagClass, null);
		}

		/// <summary>
		/// Gets whether or not the hierarchy supports renaming a given node.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <returns>
		///   <c>true</c> if the hierarchy supports renaming the node.
		/// </returns>
		public override bool CanRenameNode(TagHierarchyNode node)
		{
			// Only tag nodes can be renamed
			return (node.IsTag);
		}

		/// <summary>
		/// Renames a node. The hierarchy must support renaming it.
		/// </summary>
		/// <param name="node">The node to rename.</param>
		/// <param name="newName">The new name to give the node.</param>
		/// <seealso cref="CanRenameNode" />
		/// <exception cref="System.InvalidOperationException">Only tag nodes can be renamed</exception>
		public override void RenameNode(TagHierarchyNode node, string newName)
		{
			if (!CanRenameNode(node))
				throw new InvalidOperationException("Only tag nodes can be renamed");

			node.Parent.RenameChildSorted(node, newName);
		}

		/// <summary>
		/// Builds a list of tag names from the tag hierarchy.
		/// Positions in the list will correspond to positions in the cache file's tag list.
		/// </summary>
		/// <returns>
		/// The tag name list that was built.
		/// </returns>
		public override List<string> GetTagNames()
		{
			var result = new List<string>();
			foreach (var classNode in Nodes)
			{
				foreach (var tagNode in classNode.Children)
				{
					// Add null entries to the result list until the name fits
					var index = tagNode.Tag.Index.Index;
					while (result.Count <= index)
						result.Add(null);
					result[index] = tagNode.Name;
				}
			}
			return result;
		}
	}
}
