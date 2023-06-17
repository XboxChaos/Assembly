using Blamite.Blam;
using Blamite.Blam.Scripting;
using Blamite.Blam.Util;
using Blamite.IO;
using Blamite.RTE;
using Blamite.Serialization.Settings;

namespace Blamite.Serialization
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
		public EngineDescription(string name, int version, int versionalt, string build, SettingsGroup settings)
		{
			Name = name;
			Version = version;
			VersionAlt = versionalt;
			BuildVersion = build;
			Settings = settings;

			LoadSettings();
		}

		/// <summary>
		///     Gets the name of the engine.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		///     Gets the engine's version number.
		/// </summary>
		public int Version { get; private set; }

		/// <summary>
		///     Gets the engine's alternate version number in cases there are more that 1 known.
		/// </summary>
		public int VersionAlt { get; private set; }

		/// <summary>
		///     Gets the engine's build string.
		/// </summary>
		public string BuildVersion { get; private set; }

		/// <summary>
		///     Gets the settings for the engine.
		/// </summary>
		public SettingsGroup Settings { get; private set; }

		/// <summary>
		///     Gets the size of a map header. Pulled from the header layout.
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
		public StringIDNamespaceResolver StringIDs { get; private set; }

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
		
		/// <summary>
		///     Gets group names for the engine.
		///     Can be <c>null</c> if not present.
		/// </summary>
		public GroupNameCollection GroupNames { get; private set; }

		/// <summary>
		///		Gets the magic number used to expand tag addresses for the engine
		/// </summary>
		public int ExpandMagic { get; private set; }

		/// <summary>
		///     Gets the name of the game executable for poking purposes.
		///     Can be <c>null</c> if not present.
		/// </summary>
		public string PokingExecutable { get; private set; }

		/// <summary>
		///     Gets the name of the alternate game executable for poking purposes. (win store)
		///     Can be <c>null</c> if not present.
		/// </summary>
		public string PokingExecutableAlt { get; private set; }

		/// <summary>
		///     Gets the name of the engine's module for poking purposes.
		///     Can be <c>null</c> if not present.
		/// </summary>
		public string PokingModule { get; private set; }

		/// <summary>
		///     Gets the collection of pointers to allow for poking on PC.
		///     Can be <c>null</c> if not present.
		/// </summary>
		public PokingCollection Poking { get; private set; }

		/// <summary>
		///		Whether the cache file itself uses compression.
		/// </summary>
		public bool UsesCompression { get; private set; }

		/// <summary>
		///		MCC sometimes ships maps with string hashes 0'd out, in some cases adding a hash can cause issues.
		/// </summary>
		public bool UsesStringHashes { get; private set; }

		/// <summary>
		///		MCC sometimes ships maps with resource hashes and checksums 0'd out, this makes some injection optimizations impossible.
		/// </summary>
		public bool UsesRawHashes { get; private set; }

		/// <summary>
		///		Some/later builds have optimization which removes unused shader code from maps to save space. Injection can mean these removed shaders can be referenced and cause issues.
		/// </summary>
		public bool OptimizedShaders { get; private set; }

		/// <summary>
		///		Expected Endianness of the build.
		/// </summary>
		public Endian Endian { get; private set; }

		/// <summary>
		///		The location of the build string for this engine. Pulled from the header layout.
		/// </summary>
		public int BuildStringOffset { get; private set; }

		/// <summary>
		///		The engine's generation.
		/// </summary>
		public EngineType Engine { get; set; }

		/// <summary>
		///     Gets the platform of the game for poking purposes.
		/// </summary>
		public RTEConnectionType PokingPlatform { get; private set; }

		/// <summary>
		///		Starting with Halo 4 localization is expected to be sorted by their string id keys for performance reasons.
		/// </summary>
		public bool SortLocalesByStringID { get; private set; }

		/// <summary>
		///     Gets the value to align the tag segment on.
		/// </summary>
		public int TagSegmentAlignment { get; private set; }

		/// <summary>
		///     Reverses the endian for the checksum during write, which is a thing I guess.
		/// </summary>
		public bool ReverseChecksum { get; private set; }

		private void LoadSettings()
		{
			LoadEngineSettings();
			LoadPokingSettings();
			LoadDatabases();
			LoadCrucialLayoutInfo();
		}

		private void LoadEngineSettings()
		{
			SegmentAlignment = Settings.GetSettingOrDefault("engineInfo/segmentAlignment", 0x1000);
			ExpandMagic = Settings.GetSettingOrDefault("engineInfo/expandMagic", -1);
			TagSegmentAlignment = Settings.GetSettingOrDefault("engineInfo/tagSegmentAlignment", 0x10000);

			UsesCompression = Settings.GetSettingOrDefault("engineInfo/usesCompression", false);
			UsesStringHashes = Settings.GetSettingOrDefault("engineInfo/usesStringHashes", true);
			UsesRawHashes = Settings.GetSettingOrDefault("engineInfo/usesRawHashes", true);
			OptimizedShaders = Settings.GetSettingOrDefault("engineInfo/optimizedShaders", false);
			SortLocalesByStringID = Settings.GetSettingOrDefault("engineInfo/sortLocalesByStringId", false);
			ReverseChecksum = Settings.GetSettingOrDefault("engineInfo/reverseChecksum", false);

			string endian = Settings.GetSettingOrDefault("engineInfo/endian", "undefined");

			if (endian.Contains("big"))
				Endian = Endian.BigEndian;
			else if (endian.Contains("little"))
				Endian = Endian.LittleEndian;
			else
				throw new System.Exception("Invalid endian type \"" + endian + "\" for build " + Name + "in engines.xml. Only \"big\", and \"little\" are valid.");

			string generation = Settings.GetSettingOrDefault("engineInfo/generation", "undefined");

			if (generation.Contains("first"))
				Engine = EngineType.FirstGeneration;
			else if (generation.Contains("second"))
				Engine = EngineType.SecondGeneration;
			else if (generation.Contains("third"))
				Engine = EngineType.ThirdGeneration;
			else
				throw new System.Exception("Invalid generation type \"" + generation + "\" for build " + Name + "in engines.xml. Only \"first\", \"second\", and \"third\" are valid.");

			if (Settings.PathExists("engineInfo/encryption/tagNameKey"))
				TagNameKey = new AESKey(Settings.GetSettingOrDefault<string>("engineInfo/encryption/tagNameKey", null));
			if (Settings.PathExists("engineInfo/encryption/stringIdKey"))
				StringIDKey = new AESKey(Settings.GetSettingOrDefault<string>("engineInfo/encryption/stringIdKey", null));
			if (Settings.PathExists("engineInfo/encryption/localeKey"))
				LocaleKey = new AESKey(Settings.GetSettingOrDefault<string>("engineInfo/encryption/localeKey", null));
		}

		private void LoadPokingSettings()
		{
			string platform = Settings.GetSettingOrDefault("pokingInfo/platform", "undefined");

			if (platform.Contains("xbox360"))
				PokingPlatform = RTEConnectionType.ConsoleXbox360;
			else if (platform.Contains("xbox"))
				PokingPlatform = RTEConnectionType.ConsoleXbox;
			else if (platform.Contains("pc32"))
				PokingPlatform = RTEConnectionType.LocalProcess32;
			else if (platform.Contains("pc64"))
				PokingPlatform = RTEConnectionType.LocalProcess64;
			else if (platform.Contains("undefined"))
				PokingPlatform = RTEConnectionType.None;
			else
				throw new System.Exception("Invalid platform type \"" + platform + "\" for build " + Name + "in engines.xml. Only \"xbox\", \"xbox360\", \"pc32\", and \"pc64\" are valid.");

			if (Settings.PathExists("pokingInfo/executable"))
				PokingExecutable = Settings.GetSetting<string>("pokingInfo/executable");
			if (Settings.PathExists("pokingInfo/executableAlt"))
				PokingExecutableAlt = Settings.GetSetting<string>("pokingInfo/executableAlt");
			if (Settings.PathExists("pokingInfo/module"))
				PokingModule = Settings.GetSetting<string>("pokingInfo/module");
		}

		private void LoadDatabases()
		{
			Layouts = Settings.GetSettingOrDefault<StructureLayoutCollection>("databases/layouts", null);
			StringIDs = Settings.GetSettingOrDefault<StringIDNamespaceResolver>("databases/stringIds", null);
			ScriptInfo = Settings.GetSettingOrDefault<OpcodeLookup>("databases/scripting", null);
			LocaleSymbols = Settings.GetSettingOrDefault<LocaleSymbolCollection>("databases/localeSymbols", null);
			VertexLayouts = Settings.GetSettingOrDefault<VertexLayoutCollection>("databases/vertexLayouts", null);
			GroupNames = Settings.GetSettingOrDefault<GroupNameCollection>("databases/groupNames", null);
			Poking = Settings.GetSettingOrDefault<PokingCollection>("databases/poking", null);
		}

		private void LoadCrucialLayoutInfo()
		{
			var header = Layouts.GetLayout("header");
			if (header == null)
				throw new System.Exception("Build " + Name + "in engines.xml is missing a layout for \"header\".");

			if (header.Size == 0)
				throw new System.Exception("Header layout for build " + Name + "in engines.xml is missing a valid size attribute.");
			HeaderSize = header.Size;

			if (!header.HasField("build string"))
				throw new System.Exception("Header layout for build " + Name + "in engines.xml is missing a \"build string\" field.");
			BuildStringOffset = header.GetFieldOffset("build string");
		}
	}
}