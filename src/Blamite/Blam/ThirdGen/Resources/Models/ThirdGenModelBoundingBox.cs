using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Resources.Models;
using Blamite.Flexibility;

namespace Blamite.Blam.ThirdGen.Resources.Models
{
    public class ThirdGenModelBoundingBox : IModelBoundingBox
    {
        public ThirdGenModelBoundingBox(StructureValueCollection values)
        {
            Load(values);
        }

        public float MinX { get; private set; }

        public float MaxX { get; private set; }

        public float MinY { get; private set; }

        public float MaxY { get; private set; }

        public float MinZ { get; private set; }

        public float MaxZ { get; private set; }

        public float MinU { get; private set; }

        public float MaxU { get; private set; }

        public float MinV { get; private set; }

        public float MaxV { get; private set; }

        private void Load(StructureValueCollection values)
        {
            MinX = values.GetFloat("min x");
            MaxX = values.GetFloat("max x");
            MinY = values.GetFloat("min y");
            MaxY = values.GetFloat("max y");
            MinZ = values.GetFloat("min z");
            MaxZ = values.GetFloat("max z");
            MinU = values.GetFloat("min u");
            MaxU = values.GetFloat("max u");
            MinV = values.GetFloat("min v");
            MaxV = values.GetFloat("max v");
        }
    }
}
