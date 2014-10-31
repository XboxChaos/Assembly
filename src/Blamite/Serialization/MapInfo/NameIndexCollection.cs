using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Blamite.Serialization.MapInfo
{
	/// <summary>
	///     A collection of name/index layouts which allows layouts to be looked up by type or name.
	/// </summary>
	public class NameIndexCollection : IEnumerable<NameIndexLayout>
	{
		public readonly List<NameIndexLayout> Layouts = new List<NameIndexLayout>();

		public int Count { get { return Layouts.Count; } }

		public IEnumerator<NameIndexLayout> GetEnumerator()
		{
			return Layouts.OrderBy(o => o.Index).ToList().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Layouts.OrderBy(o => o.Index).ToList().GetEnumerator();
		}

		/// <summary>
		///     Adds a name/index layout to the collection.
		/// </summary>
		/// <param name="layout">The layout to add.</param>
		public void AddLayout(NameIndexLayout layout)
		{
			if (HasLayout(layout.Index))
				throw new ArgumentOutOfRangeException("layout", "The index " + layout.Index + " is already taken in the collection");

			Layouts.Add(layout);
		}

		/// <summary>
		///     Adds a collection of layouts to the collection.
		/// </summary>
		/// <param name="layouts">The layouts to add.</param>
		public void AddLayouts(IEnumerable<NameIndexLayout> layouts)
		{
			foreach (NameIndexLayout layout in layouts)
				AddLayout(layout);
		}

		/// <summary>
		///     Determines if a layout is present in the collection.
		/// </summary>
		/// <param name="index">The index of the of layout to search for.</param>
		/// <returns>true if the layout is present in the collection.</returns>
		public bool HasLayout(int index)
		{
			return Layouts.Any(layout => layout.Index == index);
		}

		/// <summary>
		///     Determines if a layout is present in the collection.
		/// </summary>
		/// <param name="name">The name of the layout to search for.</param>
		/// <returns>true if the layout is present in the collection.</returns>
		public bool HasLayout(string name)
		{
			return Layouts.Any(layout => layout.Name == name);
		}

		/// <summary>
		///     Finds and returns a layout in the collection.
		/// </summary>
		/// <param name="index">The index of the layout to get.</param>
		/// <returns>The MaxTeamLayout with the corresponding type.</returns>
		/// <exception cref="KeyNotFoundException">Thrown if the layout is not found.</exception>
		public NameIndexLayout GetLayout(int index)
		{
			foreach (var layout in Layouts.Where(layout => layout.Index == index))
				return layout;
			
			throw new KeyNotFoundException("Unable to find a name/index layout with index " + index.ToString("D"));
		}

		/// <summary>
		///     Finds and returns a layout in the collection.
		/// </summary>
		/// <param name="name">The name of the layout to get.</param>
		/// <returns>The MaxTeamLayout with the corresponding name.</returns>
		/// <exception cref="KeyNotFoundException">Thrown if the layout is not found.</exception>
		public NameIndexLayout GetLayout(string name)
		{
			foreach (var layout in Layouts.Where(layout => layout.Name == name))
				return layout;

			throw new KeyNotFoundException("Unable to find a name/index layout named \"" + name + "\"");
		}
	}
}