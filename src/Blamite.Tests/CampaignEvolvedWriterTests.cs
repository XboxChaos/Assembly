using Blamite.Blam;
using Blamite.Blam.FifthGen;
using Blamite.Blam.FifthGen.Structures;

namespace Blamite.Tests
{
	/// <summary>
	///     Pins the guarantee the Campaign Evolved write path rests on: an unedited tag serialises
	///     back to the bytes it was parsed from, exactly.
	/// </summary>
	/// <remarks>
	///     <para>
	///         This is the load-bearing property of the whole writer, and it is worth stating why
	///         rather than only asserting it. Two things about a payload are provably unrecoverable
	///         from the decoded model: whether a stringID or tag-reference text section was
	///         NUL-terminated on disk - real data contains both spellings, 165 of 171 tag references
	///         unterminated and 6 terminated - and whether a struct field's wrapper was elided to
	///         zero bytes. A writer that re-encoded everything from the parse tree would therefore
	///         have to guess, and would corrupt a file in a way no round-trip of its own output
	///         would reveal.
	///     </para>
	///     <para>
	///         The writer avoids the guess by replaying anything undirtied verbatim. That makes
	///         byte-exactness not merely a nice property but the mechanism itself, and it means a
	///         regression here is not a cosmetic diff - it is the writer having started to invent
	///         bytes. Hence a test that compares whole payloads and, on failure, reports the first
	///         differing offset rather than "arrays differ".
	///     </para>
	///     <para>
	///         Repacking a whole IoStore container is deliberately not covered here. It writes files,
	///         and the only real container set available is a third-party mod that these tests must
	///         not touch; that path is verified separately against scratch copies.
	///     </para>
	/// </remarks>
	[Collection(nameof(CampaignEvolvedCollection))]
	public class CampaignEvolvedWriterTests
	{
		private readonly CampaignEvolvedFixture _fixture;

		public CampaignEvolvedWriterTests(CampaignEvolvedFixture fixture)
		{
			_fixture = fixture;
		}

		[Theory]
		[MemberData(nameof(CampaignEvolvedTests.ExpectedTags), MemberType = typeof(CampaignEvolvedTests))]
		public void AnUneditedTagSerialisesBackToTheBytesItWasParsedFrom(TagExpectation expected)
		{
			byte[] payload = LoadPayload(expected.Name);
			var parsed = new FifthGenTagFile(payload);

			byte[] written = FifthGenTagWriter.Write(parsed);

			AssertBytesIdentical(expected.Name, payload, written);
		}

		[Theory]
		[MemberData(nameof(CampaignEvolvedTests.ExpectedTags), MemberType = typeof(CampaignEvolvedTests))]
		public void ParsingKeepsTheOriginalPayloadVerbatim(TagExpectation expected)
		{
			// OriginalPayload is what the writer replays for anything undirtied, so it has to be
			// the input bytes and not a re-render of them.
			byte[] payload = LoadPayload(expected.Name);

			var parsed = new FifthGenTagFile(payload);

			AssertBytesIdentical(expected.Name, payload, parsed.OriginalPayload);
		}

		[Fact]
		public void WritingTwiceProducesTheSameBytes()
		{
			// A writer that mutated the parse tree as a side effect of serialising would still pass
			// a single round-trip and fail here.
			var parsed = new FifthGenTagFile(LoadPayload("pelican-vehicle"));

			byte[] first = FifthGenTagWriter.Write(parsed);
			byte[] second = FifthGenTagWriter.Write(parsed);

			AssertBytesIdentical("pelican-vehicle (second write)", first, second);
		}

		[Fact]
		public void AWrittenPayloadParsesBackIntoTheSameSchema()
		{
			var parsed = new FifthGenTagFile(LoadPayload("b30-scenario"));

			var reparsed = new FifthGenTagFile(FifthGenTagWriter.Write(parsed));

			Assert.Equal(parsed.Layout.Fields.Count, reparsed.Layout.Fields.Count);
			Assert.Equal(parsed.Layout.Structs.Count, reparsed.Layout.Structs.Count);
			Assert.Equal(parsed.Warnings.Count, reparsed.Warnings.Count);
		}

		// ---- helpers ----

		private byte[] LoadPayload(string tagName)
		{
			string container = _fixture.RequireAnyContainer();
			Blamite.Serialization.EngineDatabase database = _fixture.RequireDatabase();

			using FileStream stream = File.OpenRead(container);
			using var reader = new Blamite.IO.EndianReader(stream, Blamite.IO.Endian.BigEndian);
			Blamite.Serialization.EngineDescription engine =
				CacheFileLoader.FindEngineDescriptions(reader, database).Single();
			ICacheFile cache = CacheFileLoader.LoadCacheFileWithEngineDescription(reader, container, engine);

			ITag? match = cache.Tags.FirstOrDefault(t => t != null && cache.FileNames.GetTagName(t) == tagName);
			Assert.True(match != null, $"No tag named \"{tagName}\" in the mount.");
			return Assert.IsType<FifthGenTag>(match).RawPayload;
		}

		private static void AssertBytesIdentical(string what, byte[] expected, byte[] actual)
		{
			Assert.True(expected.Length == actual.Length,
				$"{what}: length changed, {expected.Length} -> {actual.Length}.");

			for (var i = 0; i < expected.Length; i++)
			{
				if (expected[i] == actual[i])
					continue;

				// The offset is the finding. Somebody chasing this needs to know where to look in a
				// hex editor, and what the neighbourhood looks like, not that two arrays differ.
				Assert.Fail(
					$"{what}: first difference at 0x{i:X}, expected 0x{expected[i]:X2} got 0x{actual[i]:X2}.\n" +
					$"  expected: {Window(expected, i)}\n" +
					$"  actual:   {Window(actual, i)}");
			}
		}

		private static string Window(byte[] bytes, int centre)
		{
			int start = Math.Max(0, centre - 8);
			int end = Math.Min(bytes.Length, centre + 9);
			return string.Join(" ", bytes[start..end].Select(b => b.ToString("X2")));
		}
	}
}
