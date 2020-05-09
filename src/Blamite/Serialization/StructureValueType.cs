namespace Blamite.Serialization
{
	/// <summary>
	///     The different types of basic values that a structure field can hold.
	/// </summary>
	public enum StructureValueType
	{
		Byte,
		SByte,
		UInt16, // ushort
		Int16, // short
		UInt32, // uint
		Int32, // int
		UInt64, // ulong
		Int64, // long
		Asciiz, // Null-terminated ASCII string
		Float32
	}
}