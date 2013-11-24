using Blamite.IO;

namespace Blamite.Blam.Resources
{
	/// <summary>
	///     Manages resources in a cache file.
	/// </summary>
	public interface IResourceManager
	{
		/// <summary>
		///     Loads the resource table from the cache file.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <returns>The loaded resource table, or <c>null</c> if loading failed.</returns>
		ResourceTable LoadResourceTable(IReader reader);

		/// <summary>
		///     Saves the resource table back to the file.
		/// </summary>
		/// <param name="table">The resource table to save.</param>
		/// <param name="stream">The stream to save to.</param>
		void SaveResourceTable(ResourceTable table, IStream stream);

		/// <summary>
		///     Loads the zone set table from the cache file.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <returns>The loaded zone set table, or <c>null</c> if loading failed.</returns>
		IZoneSetTable LoadZoneSets(IReader reader);
	}
}