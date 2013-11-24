namespace Blamite.Flexibility.Settings
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
	}
}