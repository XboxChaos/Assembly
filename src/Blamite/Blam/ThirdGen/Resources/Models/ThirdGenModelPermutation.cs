using Blamite.Blam.Resources.Models;
using Blamite.Flexibility;

namespace Blamite.Blam.ThirdGen.Resources.Models
{
	public class ThirdGenModelPermutation : IModelPermutation
	{
		public ThirdGenModelPermutation(StructureValueCollection values)
		{
			Load(values);
		}

		public StringID Name { get; set; }

		public int ModelSectionIndex { get; set; }

		private void Load(StructureValueCollection values)
		{
			Name = new StringID(values.GetInteger("name stringid"));
			ModelSectionIndex = (int) values.GetInteger("model section");
		}
	}
}