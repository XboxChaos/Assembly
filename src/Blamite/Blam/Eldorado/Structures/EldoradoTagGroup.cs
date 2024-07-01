using Blamite.Serialization;

namespace Blamite.Blam.Eldorado.Structures
{
	public class EldoradoTagGroup : ITagGroup
	{
		public EldoradoTagGroup(StructureValueCollection values)
		{
			Load(values);
		}

		public int Magic { get; set; }
		public int ParentMagic { get; set; }
		public int GrandparentMagic { get; set; }
		public StringID Description { get; set; }

		private void Load(StructureValueCollection values)
		{
			Magic = (int)values.GetInteger("magic");
			ParentMagic = (int)values.GetInteger("parent magic");
			GrandparentMagic = (int)values.GetInteger("grandparent magic");
			Description = new StringID(values.GetIntegerOrDefault("group stringid", 0));
		}
	}
}
