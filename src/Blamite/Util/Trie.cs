using System.Collections.Generic;
using System.Linq;

namespace Blamite.Util
{
	/// <summary>
	///     A radix tree which allows a set of strings to be searched for common prefixes.
	/// </summary>
	public class Trie
	{
		private readonly Node _root = new Node();

		/// <summary>
		///     Adds a string to the trie.
		/// </summary>
		/// <param name="str">The string to add.</param>
		public void Add(string str)
		{
			if (str.Length == 0)
				return;

			_root.AddChild(str, 0);
		}

		/// <summary>
		///     Adds a collection of strings to the trie.
		/// </summary>
		/// <param name="strings">The collection of strings to add.</param>
		public void AddRange(IEnumerable<string> strings)
		{
			foreach (string str in strings)
				Add(str);
		}

		/// <summary>
		///     Determines whether or not a string exists in the trie.
		/// </summary>
		/// <param name="str">The string to find.</param>
		/// <returns><c>true</c> if the string exists in the trie.</returns>
		public bool Contains(string str)
		{
			return (str.Length == 0) || FindPrefix(str).Any(s => s == str);
		}

		/// <summary>
		///     Finds all of the strings in the trie which start with a given prefix.
		/// </summary>
		/// <param name="prefix">The case-sensitive prefix to search for.</param>
		/// <returns>An enumerable collection of strings which start with the prefix.</returns>
		public IEnumerable<string> FindPrefix(string prefix)
		{
			if (prefix == null || prefix.Length == 0)
				return new List<string>();

			Node node = _root.FindPrefix(prefix, 0);
			if (node != null)
				return node.FindChildStrings();
			return new List<string>();
		}

		/// <summary>
		///     A node in a trie.
		/// </summary>
		private class Node
		{
			private Node _firstChild;
			private Node _next;

			private int _sequenceLen;
			private int _sequenceStart;
			private string _sequenceStr;

			/// <summary>
			///     The value of the string at the node.
			///     Can be null if the string formed by the current path is not in the collection.
			/// </summary>
			public string Value { get; set; }

			/// <summary>
			///     Creates a child node for part of a string.
			/// </summary>
			/// <param name="str">The full string.</param>
			/// <param name="startChar">The zero-based index of the first character in the string which is a part of the node.</param>
			/// <returns>The node that was created.</returns>
			public Node AddChild(string str, int startChar)
			{
				Node currentChild = _firstChild;
				while (currentChild != null)
				{
					int commonPrefix = currentChild.FindCommonPrefix(str, startChar);
					if (commonPrefix > 0)
					{
						if (commonPrefix < currentChild._sequenceLen)
							currentChild.Split(commonPrefix);

						if (startChar + commonPrefix == str.Length)
						{
							currentChild.Value = str;
							return currentChild;
						}

						return currentChild.AddChild(str, startChar + commonPrefix);
					}
					currentChild = currentChild._next;
				}

				var result = new Node();
				result._sequenceStr = str;
				result._sequenceStart = startChar;
				result._sequenceLen = str.Length - startChar;
				result.Value = str;
				result._next = _firstChild;
				_firstChild = result;
				return result;
			}

			/// <summary>
			///     Splits this node.
			/// </summary>
			/// <param name="startChar">The zero-based index of the character from the start of this node's substring to split at.</param>
			public void Split(int startChar)
			{
				var newChild = new Node();
				newChild._sequenceStr = _sequenceStr;
				newChild._sequenceStart = _sequenceStart + startChar;
				newChild._sequenceLen = _sequenceLen - startChar;
				newChild.Value = Value;
				newChild._firstChild = _firstChild;
				_firstChild = newChild;

				_sequenceLen = startChar;
				Value = null;
			}

			/// <summary>
			///     Finds the first Node whose path string starts with a given string.
			/// </summary>
			/// <param name="str">The prefix to search for.</param>
			/// <param name="startChar">The zero-based index of the character in the prefix to start searching at.</param>
			/// <returns>The Node which was found, or null if there is no path in the tree with the given prefix.</returns>
			public Node FindPrefix(string str, int startChar)
			{
				Node currentChild = _firstChild;
				while (currentChild != null)
				{
					int commonPrefix = currentChild.FindCommonPrefix(str, startChar);
					if (commonPrefix > 0)
					{
						if (startChar + commonPrefix == str.Length)
							return currentChild;

						return currentChild.FindPrefix(str, startChar + commonPrefix);
					}
					currentChild = currentChild._next;
				}
				return null;
			}

			/// <summary>
			///     Gets a collection of all strings in the trie which are a child of this node.
			/// </summary>
			/// <returns>A collection of all strings in the trie which are a child of this node.</returns>
			public IEnumerable<string> FindChildStrings()
			{
				if (Value != null)
					yield return Value;

				Node currentChild = _firstChild;
				while (currentChild != null)
				{
					foreach (string str in currentChild.FindChildStrings())
						yield return str;

					currentChild = currentChild._next;
				}
			}

			/// <summary>
			///     Determines the number of characters that this node's string has in common with part of another string.
			/// </summary>
			/// <param name="str">The full string to compare against.</param>
			/// <param name="startChar">The zero-based index of the first character in <paramref name="str" /> to start comparing with.</param>
			/// <returns>The number of characters that the given string has in common with this node's string.</returns>
			private int FindCommonPrefix(string str, int startChar)
			{
				int i = 0;
				while (i < _sequenceLen && startChar + i < str.Length)
				{
					if (str[startChar + i] != _sequenceStr[_sequenceStart + i])
						break;
					i++;
				}
				return i;
			}
		}
	}
}