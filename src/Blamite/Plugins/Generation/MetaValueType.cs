namespace Blamite.Plugins.Generation
{
	public enum MetaValueType
	{
		TagReference,
		DataReference, // Data1 = Size, Pointer = Address
		TagBlock // Data1 = Entry count, Pointer = Address
	}
}