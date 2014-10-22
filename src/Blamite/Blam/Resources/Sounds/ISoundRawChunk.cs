namespace Blamite.Blam.Resources.Sounds
{
	public interface ISoundPermutationChunk
	{
		int Offset { get; }

		int Size { get; }

		int RuntimeIndex { get; }
	}
}
