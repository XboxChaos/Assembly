namespace Blamite.Blam.Resources.Sounds
{
	public interface ISoundResourceGestalt
	{
		ISoundPlayback[] SoundPlaybacks { get; }

		ISoundPermutation[] SoundPermutations { get; }

		ISoundRawChunk[] SoundRawChunks { get; }

		ISoundPlatformCodec[] SoundPlatformCodecs { get; }
	}
}
