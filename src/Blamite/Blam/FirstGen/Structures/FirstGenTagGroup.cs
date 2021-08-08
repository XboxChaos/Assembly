using Blamite.Serialization;

namespace Blamite.Blam.FirstGen.Structures
{
	// TODO (Dragon): firstgen doesnt have an explicit group table
	//                so we have to "spoof" one while loading tags
	public class FirstGenTagGroup : ITagGroup
	{
		public FirstGenTagGroup(StructureValueCollection values)
		{
			Load(values);
		}

		public int Magic { get; set; }
		public int ParentMagic { get; set; }
		public int GrandparentMagic { get; set; }
		public StringID Description { get; set; }

		private void Load(StructureValueCollection values)
		{
			Magic = (int)values.GetInteger("tag group magic");
			ParentMagic = (int)values.GetInteger("parent group magic");
			GrandparentMagic = (int)values.GetInteger("grandparent group magic");
			// No description stringid :(
		}
	}
}
