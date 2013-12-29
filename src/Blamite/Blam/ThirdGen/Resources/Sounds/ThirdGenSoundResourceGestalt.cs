using System.Linq;
using Blamite.Blam.Resources.Sounds;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Resources.Sounds
{
	public class ThirdGenSoundResourceGestalt : ISoundResourceGestalt
	{
		public ThirdGenSoundResourceGestalt(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, EngineDescription buildInfo)
		{
			Load(values, reader, metaArea, buildInfo);
		}

		public ISoundPlatformCodec[] SoundPlatformCodecs { get; private set; }
		public ISoundPlaybackParameter[] SoundPlaybackParameters { get; private set; }
		public ISoundScale[] SoundScales { get; private set; }
		public StringID[] SoundNames { get; private set; }
		public ISoundPlayback[] SoundPlaybacks { get; private set; }
		public ISoundPermutation[] SoundPermutations { get; private set; }
		public ISoundPermutationChunk[] SoundPermutationChunks { get; private set; }

		private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, EngineDescription buildInfo)
		{
			LoadSoundNames(values, reader, metaArea, buildInfo);
			LoadPlatformCodecs(values, reader, metaArea, buildInfo);
			LoadPlaybackParameters(values, reader, metaArea, buildInfo);
			LoadScales(values, reader, metaArea, buildInfo);
			LoadSoundPlaybacks(values, reader, metaArea, buildInfo);
			LoadSoundPermutations(values, reader, metaArea, buildInfo);
			LoadSoundPermutationChunks(values, reader, metaArea, buildInfo);
		}
		private void LoadPlatformCodecs(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, EngineDescription buildInfo)
		{
			var count = (int)values.GetInteger("number of platform codecs");
			var address = values.GetInteger("platform codecs table address");
			var layout = buildInfo.Layouts.GetLayout("sound platform codecs");
			var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

			SoundPlatformCodecs = (from entry in entries
						   select new ThirdGenSoundPlatformCodec(entry)).ToArray<ISoundPlatformCodec>();
		}
		private void LoadPlaybackParameters(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, EngineDescription buildInfo)
		{
			var count = (int)values.GetInteger("number of playback parameters");
			var address = values.GetInteger("playback parameters table address");
			var layout = buildInfo.Layouts.GetLayout("sound playback parameters");
			var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

			SoundPlaybackParameters = (from entry in entries
								   select new ThirdGenSoundPlaybackParameter(entry)).ToArray<ISoundPlaybackParameter>();
		}
		private void LoadScales(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, EngineDescription buildInfo)
		{
			var count = (int)values.GetInteger("number of scales");
			var address = values.GetInteger("scales table address");
			var layout = buildInfo.Layouts.GetLayout("sound scales");
			var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

			SoundScales = (from entry in entries
						   select new ThirdGenSoundScale(entry)).ToArray<ISoundScale>();
		}
		private void LoadSoundNames(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, EngineDescription buildInfo)
		{
			var count = (int)values.GetInteger("number of sound names");
			var address = values.GetInteger("sound name table address");
			var layout = buildInfo.Layouts.GetLayout("sound names");
			var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

			SoundNames = entries.Select(e => new StringID(e.GetInteger("name index"))).ToArray();
		}
		private void LoadSoundPlaybacks(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, EngineDescription buildInfo)
		{
			var count = (int)values.GetInteger("number of playbacks");
			var address = values.GetInteger("playback table address");
			var layout = buildInfo.Layouts.GetLayout("sound playbacks");
			var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

			SoundPlaybacks = (from entry in entries
							  select new ThirdGenSoundPlayback(entry, SoundNames)).ToArray<ISoundPlayback>();
		}
		private void LoadSoundPermutations(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, EngineDescription buildInfo)
		{
			var count = (int)values.GetInteger("number of sound permutations");
			var address = values.GetInteger("sound permutation table address");
			var layout = buildInfo.Layouts.GetLayout("sound permutations");
			var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

			SoundPermutations = (from entry in entries
								 select new ThirdGenSoundPermutation(entry, SoundNames)).ToArray<ISoundPermutation>();
		}
		private void LoadSoundPermutationChunks(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, EngineDescription buildInfo)
		{
			var count = (int)values.GetInteger("number of permutation chunks");
			var address = values.GetInteger("permutation chunk table address");
			var layout = buildInfo.Layouts.GetLayout("sound permutation chunks");
			var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

			SoundPermutationChunks = (from entry in entries
							  select new ThirdGenSoundPermutationChunk(entry)).ToArray<ISoundPermutationChunk>();
		}
	}
}

