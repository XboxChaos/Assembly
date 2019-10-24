using System.Collections.Generic;

namespace Blamite.Blam.Resources
{
	/// <summary>
	///     A table of resources in a cache file.
	/// </summary>
	public class ResourceTable
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="ResourceTable" /> class.
		/// </summary>
		public ResourceTable()
		{
			Resources = new List<Resource>();
			Sizes = new List<ResourceSize>();
			Pages = new List<ResourcePage>();
			Predictions = new List<ResourcePredictionD>();
		}

		/// <summary>
		///     Gets a list of resources in the table.
		/// </summary>
		/// <seealso cref="Resource" />
		public List<Resource> Resources { get; private set; }

		/// <summary>
		///     Gets a list of sizes in the table.
		/// </summary>
		/// <seealso cref="ResourceSize" />
		public List<ResourceSize> Sizes { get; private set; }

		/// <summary>
		///     Gets a list of pages in the table.
		/// </summary>
		/// <seealso cref="ResourcePage" />
		public List<ResourcePage> Pages { get; private set; }

		/// <summary>
		///     Gets a list of predictions in the table.
		/// </summary>
		/// <seealso cref="ResourcePredictionD" />
		public List<ResourcePredictionD> Predictions { get; private set; }
	}
}