namespace Blamite.Blam.Resources.Sounds
{
	public interface ISoundRawChunk
	{
		int Offset { get; }

		int Size { get; }

		int RuntimeIndex { get; }
	}
}
