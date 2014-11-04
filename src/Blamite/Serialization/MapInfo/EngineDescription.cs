using Blamite.Serialization.Settings;

namespace Blamite.Serialization.MapInfo
{
	public enum ZoneType
	{
		None,
		Index,
		Name
	}

	/// <summary>
	///     Describes information about a supported engine.
	/// </summary>
	public class EngineDescription
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="EngineDescription" /> class.
		/// </summary>
		/// <param name="name">The engine's name.</param>
		/// <param name="levlSize">The size of the levl chunk.</param>
		/// <param name="version">The engine's version.</param>
		/// <param name="settings">The engine's settings.</param>
		public EngineDescription(string name, int levlSize, int version, SettingsGroup settings)
		{
			Name = name;
			LevlSize = levlSize;
			Version = version;
			Settings = settings;

			LoadSettings();
		}

		/// <summary>
		///     Gets the name of the engine.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		///     Gets the size of the levl chunk.
		/// </summary>
		public int LevlSize { get; private set; }

		/// <summary>
		///     Gets the engine's version.
		/// </summary>
		public int Version { get; private set; }

		/// <summary>
		///     Gets the settings for the engine.
		/// </summary>
		public SettingsGroup Settings { get; private set; }

		/// <summary>
		///     Gets the number of languages.
		/// </summary>
		public int LanguageCount { get; private set; }

		/// <summary>
		///     Gets if the engine uses a default author.
		/// </summary>
		public bool UsesDefaultAuthor { get; private set; }

		/// <summary>
		///     Gets if the engine uses visibility in insertion points.
		/// </summary>
		public bool InsertionUsesVisibility { get; private set; }

		/// <summary>
		///     Gets if the engine indicates if an insertion point is used.
		/// </summary>
		public bool InsertionUsesUsage { get; private set; }

		/// <summary>
		///     Gets the method the engine uses to indentify zones.
		/// </summary>
		public ZoneType InsertionZoneType { get; private set; }

		/// <summary>
		///     Gets the number of insertion points.
		/// </summary>
		public int InsertionCount { get; private set; }

		/// <summary>
		///     Gets the size of a single insertion point.
		/// </summary>
		public int InsertionSize { get; private set; }

		/// <summary>
		///     Gets the base offset for names inside an insertion point.
		/// </summary>
		public int InsertionNameOffset { get; private set; }

		/// <summary>
		///     Gets the base offset for descriptions inside an insertion point.
		/// </summary>
		public int InsertionDescriptionOffset { get; private set; }

		/// <summary>
		///     Gets max team layouts for the engine.
		///     Can be <c>null</c> if not present.
		/// </summary>
		public NameIndexCollection MaxTeamCollection { get; private set; }

		/// <summary>
		///     Gets multiplayer object layouts for the engine.
		///     Can be <c>null</c> if not present.
		/// </summary>
		public NameIndexCollection MultiplayerObjectCollection { get; private set; }

		private void LoadSettings()
		{
			LoadEngineSettings();
			LoadDatabases();
		}

		private void LoadEngineSettings()
		{
			LanguageCount = Settings.GetSettingOrDefault("engineInfo/languageCount", 0);
			UsesDefaultAuthor = Settings.GetSettingOrDefault("engineInfo/usesDefaultAuthor", false);
			InsertionUsesVisibility = Settings.GetSettingOrDefault("insertionPointInfo/usesVisibility", false);
			InsertionUsesUsage = Settings.GetSettingOrDefault("insertionPointInfo/usesUsage", false);
			var zoneType = Settings.GetSettingOrDefault("insertionPointInfo/zoneType", "none");
			InsertionCount = Settings.GetSettingOrDefault("insertionPointInfo/count", 0);
			InsertionSize = Settings.GetSettingOrDefault("insertionPointInfo/size", 0);
			InsertionNameOffset = Settings.GetSettingOrDefault("insertionPointInfo/nameOffset", 0);
			InsertionDescriptionOffset = Settings.GetSettingOrDefault("insertionPointInfo/descriptionOffset", 0);
			switch (zoneType)
			{
				case "index":
					InsertionZoneType = ZoneType.Index;
					break;
				case "name":
					InsertionZoneType = ZoneType.Name;
					break;
				default:
					InsertionZoneType = ZoneType.None;
					break;
			}
		}

		private void LoadDatabases()
		{
			MaxTeamCollection = Settings.GetSettingOrDefault<NameIndexCollection>("databases/maxTeams", null);
			MultiplayerObjectCollection = Settings.GetSettingOrDefault<NameIndexCollection>("databases/multiplayerObjects", null);
		}
	}
}