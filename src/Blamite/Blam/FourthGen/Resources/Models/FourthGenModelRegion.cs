using System.Linq;
using Blamite.Blam.Resources.Models;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.FourthGen.Resources.Models
{
	public class FourthGenModelRegion : IModelRegion
	{
		public FourthGenModelRegion(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			Load(values, reader, metaArea, buildInfo);
		}

		public StringID Name { get; private set; }

		public IModelPermutation[] Permutations { get; private set; }

		private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			Name = new StringID(values.GetInteger("name stringid"));

			LoadPermutations(values, reader, metaArea, buildInfo);
		}

		private void LoadPermutations(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			var count = (int) values.GetInteger("number of permutations");
			uint address = values.GetInteger("permutation table address");
			StructureLayout layout = buildInfo.Layouts.GetLayout("model permutation");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

			Permutations = (from entry in entries
				select new FourthGenModelPermutation(entry)).ToArray();
		}
	}
}