namespace Blamite.Blam.Resources
{
	/// <summary>
	///     Points to a resource's page and its location within the page.
	/// </summary>
	public class ResourcePointer
	{
		/// <summary>
		///     Gets or sets the primary resource page that the resource belongs to. Can be null.
		/// </summary>
		public ResourcePage PrimaryPage { get; set; }

		/// <summary>
		///     Gets or sets the offset of the resource data within the primary page. Can be -1.
		/// </summary>
		public int PrimaryOffset { get; set; }

		/// <summary>
		///     Gets or sets an unknown reflexive index related to the primary page.
		/// </summary>
		public int PrimaryUnknown { get; set; }

		/// <summary>
		///     Gets or sets the secondary resource page that the resource belongs to. Can be null.
		/// </summary>
		public ResourcePage SecondaryPage { get; set; }

		/// <summary>
		///     Gets or sets the offset of the resource data within the secondary page. Can be -1.
		/// </summary>
		public int SecondaryOffset { get; set; }

		/// <summary>
		///     Gets or sets an unknown reflexive index related to the secondary page.
		/// </summary>
		public int SecondaryUnknown { get; set; }
	}
}