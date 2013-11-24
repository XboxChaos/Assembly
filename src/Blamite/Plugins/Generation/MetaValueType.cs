namespace Blamite.Plugins.Generation
{
	public enum MetaValueType
	{
		TagReference,
		DataReference, // Data1 = Size, Pointer = Address
		Reflexive // Data1 = Entry count, Pointer = Address
	}
}