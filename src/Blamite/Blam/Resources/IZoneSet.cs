namespace Blamite.Blam.Resources
{
	/// <summary>
	///     A zone set definition in a cache file.
	/// </summary>
	public interface IZoneSet
	{
		/// <summary>
		///     Gets the stringID pointing to the zone set's name.
		/// </summary>
		StringID Name { get; }

		/// <summary>
		///     Activates or deactivates a resource in the zone set.
		/// </summary>
		/// <param name="index">The datum index of the resource to activate or deactivate.</param>
		/// <param name="activate"><c>true</c> if the resource should be made active, <c>false</c> otherwise.</param>
		void ActivateResource(DatumIndex index, bool activate);

		/// <summary>
		///     Activates or deactivates a resource in the zone set.
		/// </summary>
		/// <param name="resource">The resource to activate or deactivate.</param>
		/// <param name="activate"><c>true</c> if the resource should be made active, <c>false</c> otherwise.</param>
		void ActivateResource(Resource resource, bool activate);

		/// <summary>
		///     Determines whether or not a resource is marked as active.
		/// </summary>
		/// <param name="index">The datum index of the resource to check.</param>
		/// <returns><c>true</c> if the resource is active, <c>false</c> otherwise.</returns>
		bool IsResourceActive(DatumIndex index);

		/// <summary>
		///     Determines whether or not a resource is marked as active.
		/// </summary>
		/// <param name="index">The resource to check.</param>
		/// <returns><c>true</c> if the resource is active, <c>false</c> otherwise.</returns>
		bool IsResourceActive(Resource resource);

		/// <summary>
		///     Activates or deactivates a tag in the zone set.
		/// </summary>
		/// <param name="index">The datum index of the tag to activate or deactivate.</param>
		/// <param name="activate"><c>true</c> if the tag should be made active, <c>false</c> otherwise.</param>
		void ActivateTag(DatumIndex index, bool activate);

		/// <summary>
		///     Activates or deactivates a tag in the zone set.
		/// </summary>
		/// <param name="tag">The tag to activate or deactivate.</param>
		/// <param name="activate"><c>true</c> if the tag should be made active, <c>false</c> otherwise.</param>
		void ActivateTag(ITag tag, bool activate);

		/// <summary>
		///     Determines whether or not a tag is marked as active.
		/// </summary>
		/// <param name="index">The datum index of the tag to check.</param>
		/// <returns><c>true</c> if the tag is active, <c>false</c> otherwise.</returns>
		bool IsTagActive(DatumIndex index);

		/// <summary>
		///     Determines whether or not a tag is marked as active.
		/// </summary>
		/// <param name="tag">The tag to check.</param>
		/// <returns><c>true</c> if the tag is active, <c>false</c> otherwise.</returns>
		bool IsTagActive(ITag tag);
	}
}