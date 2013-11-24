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
			Pages = new List<ResourcePage>();
		}

		/// <summary>
		///     Gets a list of resources in the table.
		/// </summary>
		/// <seealso cref="Resource" />
		public List<Resource> Resources { get; private set; }

		/// <summary>
		///     Gets a list of pages in the table.
		/// </summary>
		/// <seealso cref="ResourcePage" />
		public List<ResourcePage> Pages { get; private set; }
	}
}