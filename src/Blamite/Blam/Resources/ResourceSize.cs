using System.Collections.Generic;

namespace Blamite.Blam.Resources
{
	/// <summary>
	///     A resource size in a cache file.
	/// </summary>
	public class ResourceSize
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="ResourceSize" /> class.
		/// </summary>
		public ResourceSize()
		{
			Parts = new List<ResourceSizePart>();
		}

		/// <summary>
		///     Gets or sets the size's index.
		/// </summary>
		public int Index { get; set; }

		/// <summary>
		///     Gets or sets the overall size of this resource.
		/// </summary>
		public int Size { get; set; }

		/// <summary>
		///     Gets or sets the parts that this resource is broken up into.
		/// </summary>
		public List<ResourceSizePart> Parts { get; set; }
	}

	public class ResourceSizePart
	{
		/// <summary>
		///     Gets or sets the offset for this part.
		/// </summary>
		public int Offset { get; set; }

		/// <summary>
		///     Gets or sets the size of this part.
		/// </summary>
		public int Size { get; set; }
	}
}