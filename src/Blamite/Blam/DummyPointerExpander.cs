using Blamite.IO;

namespace Blamite.Blam
{
	/// <summary>
	/// PointerExpander which does nothing.
	/// </summary>
	public class DummyPointerExpander : IPointerExpander
	{
		public bool IsValid { get { return false; } }//lol

		public long Expand(uint pointer)
		{
			return pointer;
		}

		public uint Contract(long pointer)
		{
			return (uint)pointer;
		}
	}
}
