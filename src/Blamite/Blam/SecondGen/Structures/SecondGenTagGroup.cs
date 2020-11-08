using Blamite.Serialization;

namespace Blamite.Blam.SecondGen.Structures
{
	public class SecondGenTagGroup : ITagGroup
	{
		public SecondGenTagGroup(StructureValueCollection values)
		{
			Load(values);
		}

		public int Magic { get; set; }
		public int ParentMagic { get; set; }
		public int GrandparentMagic { get; set; }
		public StringID Description { get; set; }

		public StructureValueCollection Serialize()
		{
			var result = new StructureValueCollection();

			result.SetInteger("magic", (uint)Magic);
			result.SetInteger("parent magic", (uint)ParentMagic);
			result.SetInteger("grandparent magic", (uint)GrandparentMagic);

			return result;
		}

		private void Load(StructureValueCollection values)
		{
			Magic = (int) values.GetInteger("magic");
			ParentMagic = (int) values.GetInteger("parent magic");
			GrandparentMagic = (int) values.GetInteger("grandparent magic");
			// No description stringid :(
		}
	}
}