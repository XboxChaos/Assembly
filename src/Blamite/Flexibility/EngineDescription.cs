using Blamite.Blam;
using Blamite.Blam.Scripting;
using Blamite.Blam.Util;
using Blamite.Flexibility.Settings;

namespace Blamite.Flexibility
{
	/// <summary>
	///     Describes information about a supported engine.
	/// </summary>
	public class EngineDescription
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="EngineDescription" /> class.
		/// </summary>
		/// <param name="name">The engine's name.</param>
		/// <param name="version">The engine's version.</param>
		/// <param name="settings">The engine's settings.</param>
		public EngineDescription(string name, string version, SettingsGroup settings)
		{
			Name = name;
			Version = version;
			Settings = settings;

			LoadSettings();
		}

		/// <summary>
		///     Gets the name of the engine.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		///     Gets the engine's version string.
		/// </summary>
		public string Version { get; private set; }

		/// <summary>
		///     Gets the settings for the engine.
		/// </summary>
		public SettingsGroup Settings { get; private set; }

		/// <summary>
		///     Gets the size of a map header.
		/// </summary>
		public int HeaderSize { get; private set; }

		/// <summary>
		///     Gets the value to align segment boundaries on.
		/// </summary>
		public int SegmentAlignment { get; private set; }

		/// <summary>
		///     Gets the key to decrypt tagnames with.
		///     Can be <c>null</c> if decryption is not required.
		/// </summary>
		public AESKey TagNameKey { get; private set; }

		/// <summary>
		///     Gets the key to decrypt stringIDs with.
		///     Can be <c>null</c> if decryption is not required.
		/// </summary>
		public AESKey StringIDKey { get; private set; }

		/// <summary>
		///     Gets the key to decrypt localized strings with.
		///     Can be <c>null</c> if decryption is not required.
		/// </summary>
		public AESKey LocaleKey { get; private set; }

		/// <summary>
		///     Gets the layouts for the engine.
		///     Can be <c>null</c> if not present.
		/// </summary>
		public StructureLayoutCollection Layouts { get; private set; }

		/// <summary>
		///     Gets the stringID set resolver for the engine.
		///     Can be <c>null</c> if not present.
		/// </summary>
		public StringIDSetResolver StringIDs { get; private set; }

		/// <summary>
		///     Gets scripting info for the engine.
		///     Can be <c>null</c> if not present.
		/// </summary>
		public OpcodeLookup ScriptInfo { get; private set; }

		/// <summary>
		///     Gets locale symbols for the engine.
		///     Can be <c>null</c> if not present.
		/// </summary>
		public LocaleSymbolCollection LocaleSymbols { get; private set; }

		/// <summary>
		///     Gets vertex layouts for the engine.
		///     Can be <c>null</c> if not present.
		/// </summary>
		public VertexLayoutCollection VertexLayouts { get; private set; }

		private void LoadSettings()
		{
			LoadEngineSettings();
			LoadDatabases();
		}

		private void LoadEngineSettings()
		{
			HeaderSize = Settings.GetSetting<int>("engineInfo/headerSize");
			SegmentAlignment = Settings.GetSettingOrDefault("engineInfo/segmentAlignment", 0x1000);
			if (Settings.PathExists("engineInfo/encryption/tagNameKey"))
				TagNameKey = new AESKey(Settings.GetSettingOrDefault<string>("engineInfo/encryption/tagNameKey", null));
			if (Settings.PathExists("engineInfo/encryption/stringIdKey"))
				StringIDKey = new AESKey(Settings.GetSettingOrDefault<string>("engineInfo/encryption/stringIdKey", null));
			if (Settings.PathExists("engineInfo/encryption/localeKey"))
				LocaleKey = new AESKey(Settings.GetSettingOrDefault<string>("engineInfo/encryption/localeKey", null));
		}

		private void LoadDatabases()
		{
			Layouts = Settings.GetSettingOrDefault<StructureLayoutCollection>("databases/layouts", null);
			StringIDs = Settings.GetSettingOrDefault<StringIDSetResolver>("databases/stringIds", null);
			ScriptInfo = Settings.GetSettingOrDefault<OpcodeLookup>("databases/scripting", null);
			LocaleSymbols = Settings.GetSettingOrDefault<LocaleSymbolCollection>("databases/localeSymbols", null);
			VertexLayouts = Settings.GetSettingOrDefault<VertexLayoutCollection>("databases/vertexLayouts", null);
		}
	}
}