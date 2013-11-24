namespace Blamite.Patching
{
	/// <summary>
	///     Represents a change that should be made to the contents of a file segment.
	/// </summary>
	public class DataChange
	{
		public DataChange(uint offset, byte[] data)
		{
			Offset = offset;
			Data = data;
		}

		/// <summary>
		///     The offset of the change from the start of the segment.
		/// </summary>
		public uint Offset { get; set; }

		/// <summary>
		///     The data that should be written to the offset.
		/// </summary>
		public byte[] Data { get; set; }
	}
}