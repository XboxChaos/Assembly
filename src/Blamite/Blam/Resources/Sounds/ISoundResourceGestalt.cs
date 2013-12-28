namespace Blamite.Blam.Resources.Sounds
{
	public interface ISoundResourceGestalt
	{
		ISoundPlayback[] SoundPlaybacks { get; }

		ISoundPermutation[] SoundPermutations { get; }

		ISoundPermutationChunk[] SoundPermutationChunks { get; }

		ISoundPlatformCodec[] SoundPlatformCodecs { get; }
	}
}
