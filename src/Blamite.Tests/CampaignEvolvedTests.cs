using Blamite.Blam;
using Blamite.Blam.FifthGen;
using Blamite.Blam.FifthGen.Structures;
using Blamite.IO;
using Blamite.Serialization;
using Blamite.Util;

namespace Blamite.Tests
{
	/// <summary>
	///     What mounting the reference container set is expected to produce for one tag.
	/// </summary>
	/// <param name="Name">The tag's name, as the mount resolves it.</param>
	/// <param name="Group">The four-CC of the tag's group.</param>
	/// <param name="PayloadSize">The size in bytes of the tag's <c>.ubulk</c> chunk, header included.</param>
	/// <param name="Fields">How many field definitions the payload's own schema declares.</param>
	/// <param name="Structs">How many struct definitions the payload's own schema declares.</param>
	/// <param name="Warnings">
	///     How many things the parser met and could not fully account for. Zero would be the wrong
	///     expectation to encode: real payloads contain sections nobody has decoded, and the parser
	///     is designed to say so rather than to hide them. Pinning the count catches a new one
	///     appearing <em>and</em> a known one disappearing, and both are worth a human looking.
	/// </param>
	public sealed record TagExpectation(
		string Name, string Group, int PayloadSize, int Fields, int Structs, int Warnings)
	{
		public override string ToString() => $"{Name}.{Group}";
	}

	/// <summary>
	///     End-to-end checks against a real Campaign Evolved container set: engine detection,
	///     container mounting and override resolution, and tag payload parsing.
	/// </summary>
	/// <remarks>
	///     <para>
	///         Everything asserted here was established by reading real bytes, not from published
	///         documentation, which is exactly why it needs pinning: nothing upstream will tell us
	///         if a refactor quietly changes what a tag parses into. The expected numbers are the
	///         measured output of the parser at the point Campaign Evolved support was first proven
	///         end-to-end, and a change to any of them is a finding either way - either the parser
	///         regressed, or it improved and the number here is stale. Neither should pass silently.
	///     </para>
	///     <para>
	///         These tests never write. The container set is opened read-only and no test here has
	///         any business modifying somebody's game files.
	///     </para>
	/// </remarks>
	[Collection(nameof(CampaignEvolvedCollection))]
	public class CampaignEvolvedTests
	{
		private readonly CampaignEvolvedFixture _fixture;

		public CampaignEvolvedTests(CampaignEvolvedFixture fixture)
		{
			_fixture = fixture;
		}

		private static readonly TagExpectation[] Expectations =
		[
			new("pelican_chin_gun_bullet-projectile", "proj",   21795,  310,  26, Warnings: 3),
			new("pelican_chin_gun-weapon",            "weap",   29733,  503,  46, Warnings: 1),
			new("pelican_chin_gun-vehicle",           "vehi",   46020, 1063,  79, Warnings: 1),
			new("pelican-vehicle",                    "vehi",   61041, 1063,  79, Warnings: 2),
			new("b30-scenario",                       "scnr", 4341994, 1806, 272, Warnings: 4)
		];

		public static TheoryData<TagExpectation> ExpectedTags => new(Expectations);

		// ---- engine detection ----

		[Fact]
		public void EngineDatabaseDescribesCampaignEvolved()
		{
			EngineDatabase database = _fixture.RequireDatabase();

			EngineDescription? ce = database.FirstOrDefault(e => e.Name == "Halo: Campaign Evolved");

			Assert.True(ce != null, "Engines.xml has no entry named \"Halo: Campaign Evolved\".");
			Assert.Equal(EngineType.FifthGeneration, ce!.Engine);
		}

		[Fact]
		public void CampaignEvolvedDeclaresAnIoStoreContainerRatherThanACacheHeader()
		{
			// The reason this matters: EngineDescription.LoadCrucialLayoutInfo requires a "header"
			// layout with a build string for a cache-shaped engine, and CE has neither - its build
			// is identified by the container magic instead. The container type is what exempts it.
			EngineDescription ce = _fixture.RequireDatabase().First(e => e.Name == "Halo: Campaign Evolved");

			Assert.Equal(EngineContainerType.IoStore, ce.Container);
		}

		[Fact]
		public void AContainerIsRecognizedAsCampaignEvolved()
		{
			List<EngineDescription> matches = FindEngines(_fixture.RequireAnyContainer());

			Assert.Single(matches);
			Assert.Equal("Halo: Campaign Evolved", matches[0].Name);
		}

		[Fact]
		public void ANonContainerIsNotMistakenForOne()
		{
			// A .pak sits beside every .utoc and is not one. Whatever happens to it, it must not
			// come back claiming to be a Campaign Evolved container.
			string? pak = Directory
				.EnumerateFiles(_fixture.RequireDataDirectory(), "*.pak", SearchOption.AllDirectories)
				.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
				.FirstOrDefault();
			Assert.SkipWhen(pak == null, "No .pak alongside the container set.");

			List<EngineDescription> matches;
			try
			{
				matches = FindEngines(pak!);
			}
			catch (Exception ex) when (ex is ArgumentException || ex is EndOfStreamException)
			{
				// Both are correct rejections. ArgumentException means a value did not match;
				// EndOfStreamException means the file ran out before detection had what it needed,
				// which is what a .pak does now that EndianReader refuses to pad a short read with
				// zeroes rather than reporting it. Returning no matches is equally acceptable - the
				// only unacceptable outcome is this file being claimed as a container.
				return;
			}

			Assert.DoesNotContain(matches, m => m.Name == "Halo: Campaign Evolved");
		}

		// ---- mounting ----

		[Fact]
		public void MountingOneContainerMountsItsSiblingsAndResolvesOverrides()
		{
			// A Campaign Evolved "cache" is a mount of every container in a directory, with later
			// containers overriding earlier ones by package ID. Handing the loader any one .utoc
			// must therefore produce the whole namespace, not that file's tags alone.
			ICacheFile cache = OpenReferenceSet();

			Assert.Equal(EngineType.FifthGeneration, cache.Engine);
			Assert.Equal(Expectations.Length, cache.Tags.Count(t => t != null));
		}

		[Theory]
		[MemberData(nameof(ExpectedTags))]
		public void EachExpectedTagIsMountedWithItsPayloadIntact(TagExpectation expected)
		{
			ICacheFile cache = OpenReferenceSet();
			FifthGenTag tag = RequireTag(cache, expected.Name);

			Assert.Equal(expected.Group, CharConstant.ToString(tag.Group.Magic));
			Assert.Equal(expected.PayloadSize, tag.RawPayload.Length);
		}

		[Fact]
		public void EveryMountedTagKeepsARealNameRatherThanFallingBackToAPackageId()
		{
			// The regression this guards is subtle and did happen: a container holding several tags
			// cannot attribute its filename to any one of them, so it names them by package ID hex.
			// When such a container overrides a package that a single-tag container had already
			// named properly, the good name must survive the override even though the data does not.
			ICacheFile cache = OpenReferenceSet();

			string[] unnamed = cache.Tags
				.Where(t => t != null)
				.Select(t => cache.FileNames.GetTagName(t) ?? "")
				.Where(n => n.Length == 0 || n.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				.ToArray();

			Assert.True(unnamed.Length == 0,
				$"{unnamed.Length} tag(s) fell back to a package ID: {string.Join(", ", unnamed)}");
		}

		[Fact]
		public void TagsHaveNoCacheRelativeMetaLocation()
		{
			// Unlike every other generation, a fifth-generation tag is a whole file in its own
			// container chunk and has no address inside a shared cache. Anything that treats a null
			// MetaLocation as "this tag is empty" - as the Avalonia shell's tag projection once did,
			// dropping all five tags - is wrong for this engine specifically.
			ICacheFile cache = OpenReferenceSet();

			Assert.All(cache.Tags.Where(t => t != null), t => Assert.Null(t.MetaLocation));
		}

		// ---- payload parsing ----

		[Theory]
		[MemberData(nameof(ExpectedTags))]
		public void EachTagPayloadParsesIntoTheSchemaItDeclares(TagExpectation expected)
		{
			ICacheFile cache = OpenReferenceSet();
			FifthGenTag tag = RequireTag(cache, expected.Name);

			Assert.True(FifthGenTagFile.IsTagPayload(tag.RawPayload),
				$"{expected.Name}'s payload does not carry a BLAM/MALB signature.");

			var parsed = new FifthGenTagFile(tag.RawPayload);

			Assert.Equal(expected.Fields, parsed.Layout.Fields.Count);
			Assert.Equal(expected.Structs, parsed.Layout.Structs.Count);
		}

		[Theory]
		[MemberData(nameof(ExpectedTags))]
		public void EachTagPayloadReportsOnlyTheThingsAlreadyKnownToBeUndecoded(TagExpectation expected)
		{
			ICacheFile cache = OpenReferenceSet();
			var parsed = new FifthGenTagFile(RequireTag(cache, expected.Name).RawPayload);

			// The warning text is the finding, so a mismatch prints all of it rather than just the
			// count. What is currently expected, and why each is tolerated:
			//
			//   * every tag - the 'dtnm' table's fixed-width records are skipped by width and kept
			//                 verbatim; nothing is lost, and nothing states what they mean.
			//   * projectile and pelican-vehicle - effect references name 'effe' in their section
			//                 but leave the inline four-CC blank. The section is the authority; the
			//                 inline copy is a third-generation habit the cooker fills in only
			//                 sometimes, which is precisely why both are surfaced separately.
			//   * scenario   - 'csbn' likewise, plus the empty-wrapper and trailing-section findings
			//                 in scenario_effect_scenery_block that FifthGenTagStruct's remarks
			//                 document.
			Assert.True(expected.Warnings == parsed.Warnings.Count,
				$"{expected.Name}: expected {expected.Warnings} warning(s), got {parsed.Warnings.Count}:\n  " +
				string.Join("\n  ", parsed.Warnings));
		}

		[Fact]
		public void ATagPayloadCarriesExactlyOneRootElement()
		{
			// The root block is the tag itself. More than one element would mean the walk started
			// in the wrong place, and the field table only ever renders element zero.
			ICacheFile cache = OpenReferenceSet();
			var parsed = new FifthGenTagFile(RequireTag(cache, "pelican-vehicle").RawPayload);

			Assert.Single(parsed.Data.Elements);
		}

		[Fact]
		public void PayloadEndiannessIsDetectedRatherThanAssumed()
		{
			ICacheFile cache = OpenReferenceSet();
			var parsed = new FifthGenTagFile(RequireTag(cache, "pelican-vehicle").RawPayload);

			// Only little-endian payloads have ever been measured. If a big-endian one turns up,
			// this assertion is the right place to find out about it rather than a silent mis-parse.
			Assert.Equal(Endian.LittleEndian, parsed.Endianness);
		}

		[Fact]
		public void TheScenarioParsesWithinAnInteractiveBudget()
		{
			// b30-scenario is 4.3 MB and is parsed when its tag is clicked. This is a smoke test for
			// an order-of-magnitude regression, not a benchmark - the threshold is several times the
			// ~400 ms measured on the development machine so that a slower or busier machine does
			// not fail the suite spuriously.
			ICacheFile cache = OpenReferenceSet();
			byte[] payload = RequireTag(cache, "b30-scenario").RawPayload;

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			var parsed = new FifthGenTagFile(payload);
			stopwatch.Stop();

			Assert.NotEmpty(parsed.Layout.Fields);
			Assert.True(stopwatch.ElapsedMilliseconds < 5000,
				$"Parsing b30-scenario took {stopwatch.ElapsedMilliseconds} ms.");
		}

		// ---- helpers ----

		private List<EngineDescription> FindEngines(string path)
		{
			EngineDatabase database = _fixture.RequireDatabase();
			using FileStream stream = File.OpenRead(path);
			using var reader = new EndianReader(stream, Endian.BigEndian);
			return CacheFileLoader.FindEngineDescriptions(reader, database);
		}

		private ICacheFile OpenReferenceSet()
		{
			string container = _fixture.RequireAnyContainer();
			EngineDatabase database = _fixture.RequireDatabase();

			using FileStream stream = File.OpenRead(container);
			using var reader = new EndianReader(stream, Endian.BigEndian);
			EngineDescription engine = CacheFileLoader.FindEngineDescriptions(reader, database).Single();

			// The reader is finished with once this returns: a fifth-generation mount reads every
			// container's chunks eagerly, so no tag's bytes are still owed to an open stream.
			return CacheFileLoader.LoadCacheFileWithEngineDescription(reader, container, engine);
		}

		private static FifthGenTag RequireTag(ICacheFile cache, string name)
		{
			ITag? match = cache.Tags.FirstOrDefault(t => t != null && cache.FileNames.GetTagName(t) == name);
			Assert.True(match != null,
				$"No tag named \"{name}\" in the mount. Present: " +
				string.Join(", ", cache.Tags.Where(t => t != null).Select(t => cache.FileNames.GetTagName(t))));
			return Assert.IsType<FifthGenTag>(match);
		}
	}
}
