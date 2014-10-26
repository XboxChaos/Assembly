namespace Blamite.Blam.Resources.Sounds
{
	public interface ISoundResourceGestalt
	{
		ISoundPlatformCodec[] SoundPlatformCodecs { get; }

		ISoundPlaybackParameter[] SoundPlaybackParameters { get; }

		ISoundScale[] SoundScales { get; }

		ISoundPlayback[] SoundPlaybacks { get; }

		ISoundPermutation[] SoundPermutations { get; }

		ISoundPermutationChunk[] SoundPermutationChunks { get; }
	}
}
