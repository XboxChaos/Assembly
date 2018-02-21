using Blamite.Blam.Resources.Models;
using Blamite.Serialization;

namespace Blamite.Blam.FourthGen.Resources.Models
{
	public class FourthGenModelPermutation : IModelPermutation
	{
		public FourthGenModelPermutation(StructureValueCollection values)
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