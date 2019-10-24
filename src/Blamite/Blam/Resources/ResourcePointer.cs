namespace Blamite.Blam.Resources
{
	/// <summary>
	///     Points to a resource's page and its location within the page.
	/// </summary>
	public class ResourcePointer
	{
		/// <summary>
		///     Returns all 3 ResourcePage objects in an array.
		/// </summary>
		public ResourcePage[] PagesToArray()
		{
			return new ResourcePage[]
			{
				PrimaryPage,
				SecondaryPage,
				TertiaryPage
			};
		}

		/// <summary>
		///     Gets or sets the primary resource page that the resource belongs to. Can be null.
		/// </summary>
		public ResourcePage PrimaryPage { get; set; }

		/// <summary>
		///     Gets or sets the offset of the resource data within the primary page. Can be -1.
		/// </summary>
		public int PrimaryOffset { get; set; }

		/// <summary>
		///     Gets or sets a size reflexive index for the primary page.
		/// </summary>
		public ResourceSize PrimarySize { get; set; }

		/// <summary>
		///     Gets or sets the secondary resource page that the resource belongs to. Can be null.
		/// </summary>
		public ResourcePage SecondaryPage { get; set; }

		/// <summary>
		///     Gets or sets the offset of the resource data within the secondary page. Can be -1.
		/// </summary>
		public int SecondaryOffset { get; set; }

		/// <summary>
		///     Gets or sets a size reflexive index for the secondary page.
		/// </summary>
		public ResourceSize SecondarySize { get; set; }


		/// <summary>
		///     Gets or sets the secondary resource page that the resource belongs to. Can be null.
		/// </summary>
		public ResourcePage TertiaryPage { get; set; }

		/// <summary>
		///     Gets or sets the offset of the resource data within the secondary page. Can be -1.
		/// </summary>
		public int TertiaryOffset { get; set; }

		/// <summary>
		///     Gets or sets a size reflexive index for the secondary page.
		/// </summary>
		public ResourceSize TertiarySize { get; set; }
	}
}