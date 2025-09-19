namespace Blamite.Serialization.Settings
{
	/// <summary>
	///     Loads complex setting data from external files.
	/// </summary>
	public interface IComplexSettingLoader
	{
		/// <summary>
		///     Loads setting data from a path.
		/// </summary>
		/// <param name="path">The path to load from.</param>
		/// <returns>The loaded setting data.</returns>
		object LoadSetting(string path);

		/// <summary>
		///     Loads setting data from a path, with a secondary path.
		/// </summary>
		/// <param name="path">The path to load from.</param>
		/// <param name="altPath">The alternate path to load from.</param>
		/// <returns>The loaded setting data.</returns>
		object LoadSetting(string path, string altPath);
	}
}