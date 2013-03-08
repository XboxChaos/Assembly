using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Flexibility
{
    /// <summary>
    /// A collection of vertex layouts which allows layouts to be looked up by type or name.
    /// </summary>
    public class VertexLayoutCollection : IEnumerable<VertexLayout>
    {
        private Dictionary<int, VertexLayout> _layoutsByType = new Dictionary<int, VertexLayout>();
        private Dictionary<string, VertexLayout> _layoutsByName = new Dictionary<string, VertexLayout>();

        /// <summary>
        /// Adds a vertex layout to the collection.
        /// </summary>
        /// <param name="layout">The layout to add.</param>
        public void AddLayout(VertexLayout layout)
        {
            _layoutsByType[layout.Type] = layout;
            _layoutsByName[layout.Name] = layout;
        }

        /// <summary>
        /// Adds a collection of layouts to the collection.
        /// </summary>
        /// <param name="layouts">The layouts to add.</param>
        public void AddLayouts(IEnumerable<VertexLayout> layouts)
        {
            foreach (VertexLayout layout in layouts)
                AddLayout(layout);
        }

        /// <summary>
        /// Determines if a layout is present in the collection.
        /// </summary>
        /// <param name="type">The type index of the of layout to search for.</param>
        /// <returns>true if the layout is present in the collection.</returns>
        public bool HasLayout(int type)
        {
            return _layoutsByType.ContainsKey(type);
        }

        /// <summary>
        /// Determines if a layout is present in the collection.
        /// </summary>
        /// <param name="name">The name of the layout to search for.</param>
        /// <returns>true if the layout is present in the collection.</returns>
        public bool HasLayout(string name)
        {
            return _layoutsByName.ContainsKey(name);
        }

        /// <summary>
        /// Finds and returns a layout in the collection.
        /// </summary>
        /// <param name="type">The type index of the layout to search for.</param>
        /// <returns>The VertexLayout with the corresponding type.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the layout is not found.</exception>
        public VertexLayout GetLayout(int type)
        {
            VertexLayout result;
            if (_layoutsByType.TryGetValue(type, out result))
                return result;
            throw new KeyNotFoundException("Unable to find a vertex layout with type 0x" + type.ToString("X"));
        }

        /// <summary>
        /// Finds and returns a layout in the collection.
        /// </summary>
        /// <param name="name">The name of the layout to search for.</param>
        /// <returns>The VertexLayout with the corresponding name.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the layout is not found.</exception>
        public VertexLayout GetLayout(string name)
        {
            VertexLayout result;
            if (_layoutsByName.TryGetValue(name, out result))
                return result;
            throw new KeyNotFoundException("Unable to find a vertex layout named \"" + name + "\"");
        }

        public IEnumerator<VertexLayout> GetEnumerator()
        {
            return _layoutsByType.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _layoutsByType.Values.GetEnumerator();
        }
    }
}
