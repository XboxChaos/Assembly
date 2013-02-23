using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Resources.Models;
using ExtryzeDLL.Flexibility;

namespace ExtryzeDLL.Blam.ThirdGen.Resources.Models
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
            Name = new StringID((int)values.GetNumber("name stringid"));
            ModelSectionIndex = (int)values.GetNumber("model section");
        }
    }
}
