namespace Blamite.Blam.Resources.Sounds
{
	public interface ISoundPlayback
	{
		StringID SoundName { get; }

		int ParametersIndex { get; }

		int FirstRuntimePermutationFlagIndex { get; }

		int EncodedPermutationCount { get; }

		int FirstPermutationIndex { get; }
	}
}
