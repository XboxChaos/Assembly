
namespace Blamite.Blam
{
	public enum TagSource
	{
		Null,
		MetaArea,//main tag data section
		Data,//first gen when a flag is set
		BSP,//first and second gen xbox where bsp(s) data is a separate section(s)
	}
}
