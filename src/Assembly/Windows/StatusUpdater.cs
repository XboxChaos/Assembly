namespace Assembly.Windows
{
	public static class StatusUpdater
	{
		/// <summary>
		///     Update the status of the application.
		/// </summary>
		/// <param name="update">The new status</param>
		public static void Update(string update)
		{
			App.AssemblyStorage.AssemblySettings.HomeWindow.UpdateStatusText(update);
		}
	}
}