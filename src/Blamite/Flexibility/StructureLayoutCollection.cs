using System.Collections.Generic;

namespace Blamite.Flexibility
{
	/// <summary>
	///     A collection of StructureLayouts which can be accessed by name.
	/// </summary>
	public class StructureLayoutCollection
	{
		private readonly Dictionary<string, StructureLayout> _layouts = new Dictionary<string, StructureLayout>();

		/// <summary>
		///     Adds a layout to the collection, associating it with a certain name.
		/// </summary>
		/// <param name="name">The name to associate the layout with.</param>
		/// <param name="layout">The StructureLayout to add.</param>
		public void AddLayout(string name, StructureLayout layout)
		{
			_layouts[name] = layout;
		}

		/// <summary>
		///     Adds a collection of layouts to the collection.
		/// </summary>
		/// <param name="layouts">The layouts to add.</param>
		public void AddLayouts(StructureLayoutCollection layouts)
		{
			foreach (var layout in layouts._layouts)
				AddLayout(layout.Key, layout.Value);
		}

		/// <summary>
		///     Retrieves a layout in the collection by name.
		/// </summary>
		/// <param name="name">The name of the layout to retrieve.</param>
		/// <returns>The layout with the corresponding name.</returns>
		/// <exception cref="KeyNotFoundException">Thrown if the layout is not found.</exception>
		public StructureLayout GetLayout(string name)
		{
			StructureLayout result;
			if (_layouts.TryGetValue(name, out result))
				return result;
			throw new KeyNotFoundException("Unable to find a structure layout named \"" + name + "\"");
		}

		/// <summary>
		///     Determines whether or not a layout is defined in the collection.
		/// </summary>
		/// <param name="name">The name of the layout to search for.</param>
		/// <returns>true if a layout is associated with the given name.</returns>
		public bool HasLayout(string name)
		{
			return _layouts.ContainsKey(name);
		}

		/// <summary>
		///     Imports layouts from another layout collection.
		/// </summary>
		/// <param name="other">The collection to import layouts from.</param>
		public void Import(StructureLayoutCollection other)
		{
			foreach (var layout in other._layouts)
				AddLayout(layout.Key, layout.Value);
		}
	}
}