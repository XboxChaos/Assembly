using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Util
{
    /// <summary>
    /// A prefix tree which allows a set of strings to be searched for common prefixes.
    /// </summary>
    public class Trie
    {
        private Node _root = new Node('\0');

        /// <summary>
        /// Adds a string to the trie.
        /// </summary>
        /// <param name="str">The string to add.</param>
        public void Add(string str)
        {
            if (str.Length == 0)
                return;

            Node currentNode = _root;
            foreach (char ch in str)
                currentNode = currentNode.CreateChild(ch);

            currentNode.Value = str;
        }

        /// <summary>
        /// Adds a collection of strings to the trie.
        /// </summary>
        /// <param name="strings">The collection of strings to add.</param>
        public void AddRange(IEnumerable<string> strings)
        {
            foreach (string str in strings)
                Add(str);
        }

        /// <summary>
        /// Finds all of the strings in the trie which start with a given prefix.
        /// </summary>
        /// <param name="prefix">The case-sensitive prefix to search for.</param>
        /// <returns>An enumerable collection of strings which start with the prefix.</returns>
        public IEnumerable<string> FindPrefix(string prefix)
        {
            if (prefix == null || prefix.Length == 0)
                return new List<string>();

            Node currentNode = _root;
            foreach (char ch in prefix)
            {
                currentNode = currentNode.GetChild(ch);
                if (currentNode == null)
                    return new List<string>();
            }

            return currentNode.FindChildStrings();
        }

        /// <summary>
        /// A node in a trie.
        /// </summary>
        class Node
        {
            private Node _firstChild;
            private Node _next;
            private char _ch;

            public Node(char ch)
            {
                _ch = ch;
            }

            /// <summary>
            /// The value of the string at the node.
            /// Can be null if the string formed by the current path is not in the collection.
            /// </summary>
            public string Value { get; set; }

            /// <summary>
            /// Gets the child node labelled with a character, creating it if it does not exist.
            /// </summary>
            /// <param name="ch">The character of the child node to create.</param>
            /// <returns>The child node.</returns>
            public Node CreateChild(char ch)
            {
                Node result = GetChild(ch);
                if (result != null)
                    return result;

                // Create the node
                result = new Node(ch);
                result._next = _firstChild;
                _firstChild = result;
                return result;
            }

            /// <summary>
            /// Gets the child node labelled with a character.
            /// </summary>
            /// <param name="ch">The character of the child node to retrieve.</param>
            /// <returns>The child node, or null if it does not exist.</returns>
            public Node GetChild(char ch)
            {
                Node currentChild = _firstChild;
                while (currentChild != null)
                {
                    if (currentChild._ch == ch)
                        return currentChild;
                    currentChild = currentChild._next;
                }
                return null;
            }

            /// <summary>
            /// Gets a collection of all strings in the trie which are a child of this node.
            /// </summary>
            /// <returns>A collection of all strings in the trie which are a child of this node.</returns>
            public IEnumerable<string> FindChildStrings()
            {
                List<string> list = new List<string>();
                if (Value != null)
                    list.Add(Value);

                IEnumerable<string> result = list;
                Node currentChild = _firstChild;
                while (currentChild != null)
                {
                    result = result.Concat(currentChild.FindChildStrings());
                    currentChild = currentChild._next;
                }
                return result;
            }
        }
    }
}
