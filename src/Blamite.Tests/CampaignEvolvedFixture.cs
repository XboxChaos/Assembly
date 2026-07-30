using System.Text;
using Blamite.Serialization;
using Blamite.Serialization.Settings;

namespace Blamite.Tests
{
	/// <summary>
	///     Loads Blamite's engine database once for the whole suite, and locates the real Campaign
	///     Evolved container set the format tests read.
	/// </summary>
	/// <remarks>
	///     <para>
	///         The container set is a third-party mod and is deliberately not vendored into this
	///         repository — it is not ours to redistribute, and it is four megabytes. Tests that
	///         need it therefore skip, loudly and by name, when it is absent, rather than passing
	///         vacuously. Point <c>ASM_CE_TEST_DATA</c> at a directory holding a
	///         <c>.utoc</c>/<c>.ucas</c> set to run them somewhere else.
	///     </para>
	///     <para>
	///         Two pieces of environment setup are not optional and are the reason this is a shared
	///         fixture rather than a helper called per test. Blamite's Windows-1252 string reads
	///         need a code page .NET does not register by default outside .NET Framework, and
	///         <c>Engines.xml</c> resolves the nested database paths it names against the process
	///         current directory with no rebasing — so the current directory has to be the output
	///         directory before the database loads. Both are process-global, so both belong here.
	///     </para>
	/// </remarks>
	public sealed class CampaignEvolvedFixture
	{
		/// <summary>The environment variable that overrides where the container set is looked for.</summary>
		public const string PathVariable = "ASM_CE_TEST_DATA";

		private const string DefaultPath = "~/Downloads/Flyable Pelican July 29 2026";

		public CampaignEvolvedFixture()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			Directory.SetCurrentDirectory(AppContext.BaseDirectory);

			string enginesXml = Path.Combine(AppContext.BaseDirectory, "Formats", "Engines.xml");
			if (!File.Exists(enginesXml))
			{
				DatabaseError = $"Engines.xml was not copied to the test output directory ({enginesXml}).";
				return;
			}

			try
			{
				Database = XMLEngineDatabaseLoader.LoadDatabase(enginesXml);
			}
			catch (Exception ex)
			{
				DatabaseError = $"{ex.GetType().Name}: {ex.Message}";
			}
		}

		/// <summary>Gets the loaded engine database, or <c>null</c> if it could not be loaded.</summary>
		public EngineDatabase? Database { get; }

		/// <summary>Gets why the engine database could not be loaded, or <c>null</c> if it was.</summary>
		public string? DatabaseError { get; }

		/// <summary>
		///     Gets the directory holding the Campaign Evolved container set, or <c>null</c> if no
		///     directory containing a <c>.utoc</c> was found.
		/// </summary>
		public string? DataDirectory
		{
			get
			{
				string configured = Environment.GetEnvironmentVariable(PathVariable) ?? Expand(DefaultPath);
				if (!Directory.Exists(configured))
					return null;
				return Directory.EnumerateFiles(configured, "*.utoc", SearchOption.AllDirectories).Any()
					? configured
					: null;
			}
		}

		/// <summary>
		///     Returns the container set's directory, or skips the calling test with an explanation
		///     of what to set to make it run.
		/// </summary>
		public string RequireDataDirectory()
		{
			string? directory = DataDirectory;
			if (directory == null)
			{
				Assert.Skip(
					$"No Campaign Evolved container set found. Set {PathVariable} to a directory " +
					$"containing a .utoc/.ucas pair (default: {DefaultPath}).");
			}

			return directory!;
		}

		/// <summary>Returns the loaded engine database, or fails the calling test explaining why it is missing.</summary>
		public EngineDatabase RequireDatabase()
		{
			Assert.True(Database != null, $"Engine database unavailable: {DatabaseError}");
			return Database!;
		}

		/// <summary>Returns any one <c>.utoc</c> from the container set. Mounting one mounts all its siblings.</summary>
		public string RequireAnyContainer()
		{
			return Directory.EnumerateFiles(RequireDataDirectory(), "*.utoc", SearchOption.AllDirectories)
				.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
				.First();
		}

		private static string Expand(string path)
		{
			if (!path.StartsWith("~/", StringComparison.Ordinal))
				return path;
			string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			return Path.Combine(home, path[2..]);
		}
	}

	[CollectionDefinition(nameof(CampaignEvolvedCollection))]
	public sealed class CampaignEvolvedCollection : ICollectionFixture<CampaignEvolvedFixture>
	{
	}
}
