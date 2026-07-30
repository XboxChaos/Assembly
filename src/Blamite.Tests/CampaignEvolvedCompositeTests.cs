using Blamite.Blam;
using Blamite.Blam.FifthGen;
using Blamite.Blam.FifthGen.Structures;
using Blamite.IO;

namespace Blamite.Tests
{
	/// <summary>
	///     Checks that writing a composite field is the exact inverse of reading one.
	/// </summary>
	/// <remarks>
	///     <para>
	///         Composite types - points, vectors, planes, quaternions, bounds, colours, rectangles -
	///         decode as a plain run of same-width numbers in the payload's endianness. Nothing in
	///         the format states that layout; it was inferred from the declared widths and then
	///         checked against real data. That makes the encoder the riskiest part of the write
	///         path: a reader that is subtly wrong is a display bug, but an encoder that disagrees
	///         with the reader silently rewrites geometry.
	///     </para>
	///     <para>
	///         So these tests do not assert an expected byte pattern, which would only restate the
	///         encoder's own opinion. They assert the two halves agree: set a value, and confirm it
	///         reads back; and set a field to what it already holds, and confirm not one byte moved.
	///         The second is the sharper of the two - it fails for any disagreement in component
	///         order, width or endianness, without needing to know what the right bytes are.
	///     </para>
	/// </remarks>
	[Collection(nameof(CampaignEvolvedCollection))]
	public class CampaignEvolvedCompositeTests
	{
		private readonly CampaignEvolvedFixture _fixture;

		public CampaignEvolvedCompositeTests(CampaignEvolvedFixture fixture)
		{
			_fixture = fixture;
		}

		[Fact]
		public void RewritingAVectorWithItsOwnComponentsChangesNoBytes()
		{
			FifthGenTagFile tag = Parse("pelican-vehicle");
			FifthGenVectorValue vector = FindValue<FifthGenVectorValue>(tag);

			byte[] before = (byte[]) vector.RawData.Clone();
			vector.SetComponents(new List<float>(vector.Components), tag.Endianness);

			Assert.Equal(before, vector.RawData);
		}

		[Fact]
		public void AVectorReadsBackWhatWasWrittenToIt()
		{
			FifthGenTagFile tag = Parse("pelican-vehicle");
			FifthGenVectorValue vector = FindValue<FifthGenVectorValue>(tag);

			var written = new List<float>();
			for (var i = 0; i < vector.Components.Count; i++)
				written.Add(1.5f + i);

			vector.SetComponents(written, tag.Endianness);

			Assert.Equal(written, vector.Components);
			Assert.True(vector.Dirty, "Setting a component must mark the value dirty, or the writer will replay the original bytes.");
		}

		[Fact]
		public void AVectorRefusesAComponentCountTheSchemaDoesNotAllow()
		{
			// The width belongs to the schema, not the value: a real point 3d cannot become a
			// four-float one. Silently accepting it would corrupt every field after this one.
			FifthGenTagFile tag = Parse("pelican-vehicle");
			FifthGenVectorValue vector = FindValue<FifthGenVectorValue>(tag);

			var tooMany = new List<float>(vector.Components) {0f};

			Assert.Throws<ArgumentException>(() => vector.SetComponents(tooMany, tag.Endianness));
		}

		[Fact]
		public void RewritingBoundsWithTheirOwnValuesChangesNoBytes()
		{
			FifthGenTagFile tag = Parse("pelican-vehicle");
			FifthGenBoundsValue bounds = FindValue<FifthGenBoundsValue>(tag);

			byte[] before = (byte[]) bounds.RawData.Clone();
			bounds.SetBounds(bounds.Lo, bounds.Hi, tag.Endianness);

			Assert.Equal(before, bounds.RawData);
		}

		[Fact]
		public void BoundsPreserveAnInvertedRangeRatherThanReorderingIt()
		{
			// Several engines have used hi < lo to mean "disabled". Reordering a caller's numbers
			// would be this class inventing a rule the format does not state.
			FifthGenTagFile tag = Parse("pelican-vehicle");
			FifthGenBoundsValue bounds = FindValue<FifthGenBoundsValue>(tag);

			bounds.SetBounds(10f, -10f, tag.Endianness);

			Assert.Equal(10f, bounds.Lo);
			Assert.Equal(-10f, bounds.Hi);
		}

		[Fact]
		public void RewritingIntegerBoundsWithTheirOwnValuesChangesNoBytes()
		{
			FifthGenTagFile tag = Parse("b30-scenario");
			FifthGenIntegerBoundsValue bounds = FindValue<FifthGenIntegerBoundsValue>(tag);

			byte[] before = (byte[]) bounds.RawData.Clone();
			bounds.SetBounds(bounds.Lo, bounds.Hi, tag.Endianness);

			Assert.Equal(before, bounds.RawData);
		}

		[Fact]
		public void RewritingAPackedColourWithItsOwnWordChangesNoBytes()
		{
			FifthGenTagFile tag = Parse("b30-scenario");
			FifthGenColorValue colour = FindValue<FifthGenColorValue>(tag);

			byte[] before = (byte[]) colour.RawData.Clone();
			colour.SetPacked(colour.Packed, tag.Endianness);

			Assert.Equal(before, colour.RawData);
		}

		[Fact]
		public void APackedColourKeepsItsAlphaByte()
		{
			// Every one of the 490 rgb color fields measured in the real mod carries 0xFF here
			// rather than zero, so the byte means something to the engine even when the type name
			// says only "rgb". Blanking it on write would be a change nobody asked for.
			FifthGenTagFile tag = Parse("b30-scenario");
			FifthGenColorValue colour = FindValue<FifthGenColorValue>(tag);

			colour.SetPacked(0xFF336699, tag.Endianness);

			Assert.Equal(0xFFu, colour.A);
			Assert.Equal(0x33u, colour.R);
			Assert.Equal(0x66u, colour.G);
			Assert.Equal(0x99u, colour.B);
		}

		[Fact]
		public void RewritingARealColourWithItsOwnChannelsChangesNoBytes()
		{
			// Only twelve real-colour fields exist across the whole reference set, and which tag
			// holds them is not something to hardcode - a test pinned to the wrong tag would skip
			// forever and quietly stop covering anything.
			(FifthGenTagFile tag, FifthGenRealColorValue colour) = FindAcrossTags<FifthGenRealColorValue>();

			byte[] before = (byte[]) colour.RawData.Clone();
			colour.SetComponents(new List<float>(colour.Components), tag.Endianness);

			Assert.Equal(before, colour.RawData);
		}

		[Fact]
		public void AnEditedCompositeSurvivesAWriteAndReparse()
		{
			// The end-to-end shape: change a vector, serialise the whole tag, parse the result
			// back, and find the new value where it should be. This is what an edit in the UI does.
			FifthGenTagFile tag = Parse("pelican-vehicle");
			List<PathStep> route = RequireRoute<FifthGenVectorValue>(tag);
			FifthGenVectorValue target = Resolve<FifthGenVectorValue>(tag, route);

			var written = new List<float>();
			for (var i = 0; i < target.Components.Count; i++)
				written.Add(-7.25f * (i + 1));
			target.SetComponents(written, tag.Endianness);

			var reparsed = new FifthGenTagFile(FifthGenTagWriter.Write(tag));

			Assert.Equal(written, Resolve<FifthGenVectorValue>(reparsed, route).Components);
		}

		// ---- helpers ----

		private FifthGenTagFile Parse(string tagName)
		{
			string container = _fixture.RequireAnyContainer();
			Blamite.Serialization.EngineDatabase database = _fixture.RequireDatabase();

			using FileStream stream = File.OpenRead(container);
			using var reader = new EndianReader(stream, Endian.BigEndian);
			Blamite.Serialization.EngineDescription engine =
				CacheFileLoader.FindEngineDescriptions(reader, database).Single();
			ICacheFile cache = CacheFileLoader.LoadCacheFileWithEngineDescription(reader, container, engine);

			ITag? match = cache.Tags.FirstOrDefault(t => t != null && cache.FileNames.GetTagName(t) == tagName);
			Assert.True(match != null, $"No tag named \"{tagName}\" in the mount.");
			return new FifthGenTagFile(Assert.IsType<FifthGenTag>(match).RawPayload);
		}

		private static FifthGenTagStruct Root(FifthGenTagFile tag) => tag.Data.Elements[0];

		/// <summary>
		///     One step from a struct down to a nested one: which field, and which of its elements.
		/// </summary>
		/// <remarks>
		///     Composite fields are rarely on a tag's root struct - they sit inside blocks, arrays
		///     and inlined sub-structs, several levels down. A test that wants to find the same field
		///     again in a freshly-parsed copy therefore needs a positional route to it rather than an
		///     object reference, since reparsing produces an entirely new object graph. Element index
		///     is -1 for an inlined struct, which has exactly one.
		/// </remarks>
		private readonly record struct PathStep(int FieldIndex, int ElementIndex);

		private static IList<FifthGenTagStruct> ChildrenOf(FifthGenTagValue value) => value switch
		{
			FifthGenStructValue s => new[] {s.Value},
			FifthGenArrayValue a => a.Elements,
			FifthGenBlockValue b => b.Value?.Elements ?? (IList<FifthGenTagStruct>) Array.Empty<FifthGenTagStruct>(),
			_ => Array.Empty<FifthGenTagStruct>()
		};

		/// <summary>Walks the whole value tree for the first value of a given type, returning the route to it.</summary>
		private static List<PathStep>? RouteToFirst<T>(FifthGenTagStruct instance, int depth = 0)
			where T : FifthGenTagValue
		{
			// A block's element struct can legitimately be the struct containing the block, so an
			// unbounded walk can recurse forever on a perfectly valid tag. A depth cap is enough
			// here: every composite type appears well within a handful of levels.
			if (depth > 8) return null;

			IList<FifthGenTagValue> values = instance.Values;
			for (var i = 0; i < values.Count; i++)
			{
				if (values[i] is T)
					return new List<PathStep> {new(i, -1)};

				IList<FifthGenTagStruct> children = ChildrenOf(values[i]);
				for (var e = 0; e < children.Count; e++)
				{
					List<PathStep>? deeper = RouteToFirst<T>(children[e], depth + 1);
					if (deeper == null) continue;

					deeper.Insert(0, new PathStep(i, values[i] is FifthGenStructValue ? -1 : e));
					return deeper;
				}
			}

			return null;
		}

		private static List<PathStep> RequireRoute<T>(FifthGenTagFile tag) where T : FifthGenTagValue
		{
			List<PathStep>? route = RouteToFirst<T>(Root(tag));
			Assert.SkipWhen(route == null, $"No {typeof(T).Name} field anywhere in this tag.");
			return route!;
		}

		private static T Resolve<T>(FifthGenTagFile tag, IReadOnlyList<PathStep> route) where T : FifthGenTagValue
		{
			FifthGenTagStruct instance = Root(tag);
			for (var i = 0; i < route.Count - 1; i++)
			{
				PathStep step = route[i];
				IList<FifthGenTagStruct> children = ChildrenOf(instance.Values[step.FieldIndex]);
				instance = children[step.ElementIndex < 0 ? 0 : step.ElementIndex];
			}

			return Assert.IsType<T>(instance.Values[route[^1].FieldIndex]);
		}

		private static T FindValue<T>(FifthGenTagFile tag) where T : FifthGenTagValue =>
			Resolve<T>(tag, RequireRoute<T>(tag));

		/// <summary>
		///     Finds the first value of a given type in whichever reference tag happens to have one.
		/// </summary>
		/// <remarks>
		///     Some composite types are rare - there are only twelve real-colour fields in the whole
		///     reference set. Naming a tag that turns out not to contain one produces a test that
		///     skips forever and silently covers nothing, so search instead of guessing.
		/// </remarks>
		private (FifthGenTagFile, T) FindAcrossTags<T>() where T : FifthGenTagValue
		{
			foreach (string name in new[]
			{
				"pelican_chin_gun_bullet-projectile", "pelican_chin_gun-weapon",
				"pelican_chin_gun-vehicle", "pelican-vehicle", "b30-scenario"
			})
			{
				FifthGenTagFile tag = Parse(name);
				List<PathStep>? route = RouteToFirst<T>(Root(tag));
				if (route != null)
					return (tag, Resolve<T>(tag, route));
			}

			Assert.Skip($"No {typeof(T).Name} field in any reference tag.");
			return default;
		}
	}
}
