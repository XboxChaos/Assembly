using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Resources.BSP;
using Blamite.Blam.Resources.Models;
using Blamite.Blam.ThirdGen.Resources.Models;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Resources.BSP
{
    public class ThirdGenScenarioBSP : IScenarioBSP
    {
        public ThirdGenScenarioBSP(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            Load(values, reader, metaArea, buildInfo);
        }

        public IModelSection[] Sections { get; private set; }

        public IModelBoundingBox[] BoundingBoxes { get; private set; }

        public DatumIndex ModelResourceIndex { get; private set; }

        private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            ModelResourceIndex = new DatumIndex(values.GetInteger("model resource datum index"));

            LoadSections(values, reader, metaArea, buildInfo);
            LoadBoundingBoxes(values, reader, metaArea, buildInfo);
        }

        private void LoadSections(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            int count = (int)values.GetInteger("number of sections");
            uint address = values.GetInteger("section table address");
            var layout = buildInfo.GetLayout("model section");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

            Sections = (from entry in entries
                        select new ThirdGenModelSection(entry, reader, metaArea, buildInfo)).ToArray();
        }

        private void LoadBoundingBoxes(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            int count = (int)values.GetInteger("number of bounding boxes");
            uint address = values.GetInteger("bounding box table address");
            var layout = buildInfo.GetLayout("model bounding box");
            var entries = ReflexiveReader.ReadReflexive(reader, 1, address, layout, metaArea);

            BoundingBoxes = (from entry in entries
                             select new ThirdGenModelBoundingBox(entry)).ToArray();
        }
    }
}
