namespace Blamite.Blam.Resources.Sounds
{
	public interface ISoundPermutation
	{
		StringID SoundName { get; }

		int EncodedSkipFraction { get; }

		int EncodedGain { get; }

		int PermutationInfoIndex { get; }

		int LanguageNeutralTime { get; }

		int PermutationChunkIndex { get; }

		int ChunkCount { get; }

		int EncodedPermutationIndex { get; }
	}
}
