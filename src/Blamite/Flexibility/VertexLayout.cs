using System.Collections.Generic;

namespace Blamite.Flexibility
{
	/// <summary>
	///     Defines the layout of a vertex in a vertex buffer.
	/// </summary>
	public class VertexLayout
	{
		private readonly HashSet<VertexElementUsage> _availableUsages = new HashSet<VertexElementUsage>();
		private readonly List<VertexElementLayout> _elements;

		public VertexLayout(int type, string name)
		{
			Type = type;
			Name = name;
			_elements = new List<VertexElementLayout>();
		}

		/// <summary>
		///     Gets the type index for the vertex.
		/// </summary>
		public int Type { get; private set; }

		/// <summary>
		///     Gets the name of the vertex definition.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		///     Gets a read-only list of the elements in the layout.
		/// </summary>
		public IList<VertexElementLayout> Elements
		{
			get { return _elements.AsReadOnly(); }
		}

		/// <summary>
		///     Adds an element layout to the vertex layout.
		/// </summary>
		/// <param name="element">The element layout to add.</param>
		public void AddElement(VertexElementLayout element)
		{
			_elements.Add(element);
			_availableUsages.Add(element.Usage);
		}

		/// <summary>
		///     Adds a group of elements to the vertex layout.
		/// </summary>
		/// <param name="elements">The element layouts to add.</param>
		public void AddElements(IEnumerable<VertexElementLayout> elements)
		{
			foreach (VertexElementLayout element in elements)
				AddElement(element);
		}

		/// <summary>
		///     Determines if an element in the layout has a certain usage.
		/// </summary>
		/// <param name="usage">The VertexElementUsage to search for.</param>
		/// <returns>true if an element with the given usage is present in the layout.</returns>
		public bool HasElement(VertexElementUsage usage)
		{
			return _availableUsages.Contains(usage);
		}
	}
}