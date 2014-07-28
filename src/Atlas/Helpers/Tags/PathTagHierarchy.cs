using System;
using System.Collections.Generic;
using Blamite.Blam;
using Blamite.Util;

namespace Atlas.Helpers.Tags
{
	public class PathTagHierarchy : TagHierarchy
	{
		private ICacheFile _cacheFile;

		/// <summary>
		/// Initializes a new instance of the <see cref="PathTagHierarchy"/> class.
		/// </summary>
		/// <param name="cacheFile">The cache file to use.</param>
		public PathTagHierarchy(ICacheFile cacheFile)
		{
			_cacheFile = cacheFile;
		}

		/// <summary>
		/// Adds a tag to the hierarchy if it is not already present.
		/// </summary>
		/// <param name="tag">The tag to add.</param>
		/// <param name="name">The full name of the tag.</param>
		/// <returns>
		/// The node that was created for the tag if it was added, otherwise the node that already exists for the tag.
		/// </returns>
		/// <remarks>
		/// Hierarchy-specific logic for <see cref="AddTag" /> should go here.
		/// </remarks>
		protected override TagHierarchyNode AddTagImpl(ITag tag, string name)
		{
			// Create folders for everything but the tag's name
			var components = name.Split('\\', '/');
			var parentNode = Root;
			var parentDirectory = "";
			for (var i = 0; i < components.Length - 1; i++)
			{
				parentDirectory += components[i] + "\\";
				var node = new TagHierarchyNode(components[i], null, _cacheFile, parentDirectory);
				parentNode = parentNode.InsertChildSorted(node);
			}

			// Determine the extension and suffix of the tag node based upon the class
			string suffix = null;
			string extension = "";
			if (tag.Class != null)
			{
				// Use the class description as the suffix if available
				suffix = _cacheFile.StringIDs.GetString(tag.Class.Description);
				if (suffix != null)
					suffix = " - " + suffix;

				// Convert the class magic to a string and use it as the extension
				extension = CharConstant.ToString(tag.Class.Magic);
			}
			var nodeName = components[components.Length - 1] + "." + extension;
			return parentNode.InsertChildSorted(new TagHierarchyNode(nodeName, suffix, _cacheFile, tag, parentDirectory + nodeName));
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
			DetachNodeAndEmptyAncestors(node);
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
			// Any non-root node can be renamed
			return (node.Parent != null);
		}

		/// <summary>
		/// Renames a node. The hierarchy must support renaming it.
		/// </summary>
		/// <param name="node">The node to rename.</param>
		/// <param name="newName">The new name to give the node.</param>
		/// <seealso cref="CanRenameNode" />
		/// <exception cref="System.InvalidOperationException">Node cannot be renamed</exception>
		/// <exception cref="System.ArgumentException">Cannot change a tag's extension</exception>
		public override void RenameNode(TagHierarchyNode node, string newName)
		{
			if (!CanRenameNode(node))
				throw new InvalidOperationException("Node cannot be renamed");

			// The extension on the new name must match the one on the old name
			if (node.IsTag)
			{
				var oldExtension = GetExtension(node.Name);
				var newExtension = GetExtension(newName);
				if (oldExtension != newExtension)
					throw new ArgumentException("Cannot change a tag's extension");
			}

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
			BuildTagNameList(Root, "", result);
			return result;
		}

		private void BuildTagNameList(TagHierarchyNode parent, string parentPath, List<string> result)
		{
			foreach (var node in parent.Children)
			{
				if (node.IsTag)
				{
					// Add null entries to the result list until the name fits
					var index = node.Tag.Index.Index;
					while (result.Count <= index)
						result.Add(null);

					// Append the tag's filename to the path of the parent
					result[index] = parentPath + GetFileName(node.Name);
				}
				else if (node.Children.Count > 0)
				{
					// Recurse into the folder
					BuildTagNameList(node, parentPath + node.Name + "\\", result);
				}
			}
		}

		/// <summary>
		/// Detaches a node from the tree, also detaching ancestor nodes which would become empty.
		/// </summary>
		/// <param name="node">The node to detach. It must have no children.</param>
		private void DetachNodeAndEmptyAncestors(TagHierarchyNode node)
		{
			if (node.Children.Count == 0)
			{
				var oldParent = node.Parent;
				node.Detach();
				DetachNodeAndEmptyAncestors(oldParent);
			}
		}

		private static string GetFileName(string name)
		{
			var index = name.LastIndexOf('.');
			if (index >= 0)
				return name.Substring(0, index);
			return name;
		}

		private static string GetExtension(string name)
		{
			var index = name.LastIndexOf('.');
			if (index >= 0)
				return name.Substring(index);
			return null;
		}
	}
}
