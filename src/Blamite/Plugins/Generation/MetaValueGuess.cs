namespace Blamite.Plugins.Generation
{
	public class MetaValueGuess
	{
		public MetaValueGuess(int offset, MetaValueType type, long pointer, uint data1)
		{
			Offset = offset;
			Type = type;
			Pointer = pointer;
			Data1 = data1;
		}

		public MetaValueType Type { get; set; }
		public int Offset { get; set; }
		public long Pointer { get; set; }
		public uint Data1 { get; set; }
	}
}