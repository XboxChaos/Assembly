using System.Runtime.Serialization;

namespace Assembly.Helpers.Net
{
	/// <summary>
	///     Contains information about available updates.
	/// </summary>
	[DataContract]
	public class UpdateInfo : ServerResponse
	{
		/// <summary>
		///     The download link for the update, if one is available.
		/// </summary>
		[DataMember(Name = "download_link")]
		public string DownloadLink { get; set; }

		/// <summary>
		///     The latest available version.
		/// </summary>
		[DataMember(Name = "latest_version")]
		public string LatestVersion { get; set; }

		/// <summary>
		///     Changelogs for both the current and past versions, sorted in descending order by version number.
		/// </summary>
		[DataMember(Name = "change_logs")]
		public UpdateChangelog[] Changelogs { get; set; }


		/// <summary>
		///     Contains information about what was changed in an update.
		/// </summary>
		[DataContract]
		public class UpdateChangelog
		{
			/// <summary>
			///     The version number that the changelog applies to.
			/// </summary>
			[DataMember(Name = "version")]
			public string Version { get; set; }

			/// <summary>
			///     The contents of the changelog.
			/// </summary>
			[DataMember(Name = "change_log")]
			public string Changelog { get; set; }
		}
	}

	public static class Updates
	{
		/// <summary>
		///     Retrieves information about available updates.
		/// </summary>
		/// <returns>The update information returned by the server, or null if the request failed.</returns>
		public static UpdateInfo GetUpdateInfo()
		{
			var updateCommand = new ServerRequest("update_beta");
			return AssemblyServer.SendRequest<ServerRequest, UpdateInfo>(updateCommand);
		}
	}
}