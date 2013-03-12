using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Resources.Models;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Resources.Models
{
    public class ThirdGenModelRegion : IModelRegion
    {
        public ThirdGenModelRegion(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            Load(values, reader, metaArea, buildInfo);
        }

        public StringID Name { get; private set; }

        public IModelPermutation[] Permutations { get; private set; }

        private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            Name = new StringID(values.GetInteger("name stringid"));

            LoadPermutations(values, reader, metaArea, buildInfo);
        }

        private void LoadPermutations(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            int count = (int)values.GetInteger("number of permutations");
            uint address = values.GetInteger("permutation table address");
            var layout = buildInfo.GetLayout("model permutation");
            var entries = ReflexiveReader.ReadReflexive(count, address, reader, layout, metaArea);

            Permutations = (from entry in entries
                            select new ThirdGenModelPermutation(entry)).ToArray();
        }
    }
}
