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
    public class ThirdGenRenderModel : IRenderModel
    {
        public ThirdGenRenderModel(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, EngineDescription buildInfo)
        {
            Load(values, reader, metaArea, buildInfo);
        }

        public IModelRegion[] Regions { get; private set; }

        public IModelSection[] Sections { get; private set; }

        public IModelBoundingBox BoundingBox { get; private set; }

        public DatumIndex ResourceIndex { get; private set; }

        private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, EngineDescription buildInfo)
        {
            ResourceIndex = new DatumIndex(values.GetInteger("resource datum index"));

            LoadRegions(values, reader, metaArea, buildInfo);
            LoadSections(values, reader, metaArea, buildInfo);
            LoadBoundingBox(values, reader, metaArea, buildInfo);
        }

        private void LoadRegions(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, EngineDescription buildInfo)
        {
            int count = (int)values.GetInteger("number of regions");
            uint address = values.GetInteger("region table address");
            var layout = buildInfo.Layouts.GetLayout("model region");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

            Regions = (from entry in entries
                       select new ThirdGenModelRegion(entry, reader, metaArea, buildInfo)).ToArray();
        }

        private void LoadSections(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, EngineDescription buildInfo)
        {
            int count = (int)values.GetInteger("number of sections");
            uint address = values.GetInteger("section table address");
            var layout = buildInfo.Layouts.GetLayout("model section");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

            Sections = (from entry in entries
                        select new ThirdGenModelSection(entry, reader, metaArea, buildInfo)).ToArray();
        }

        private void LoadBoundingBox(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, EngineDescription buildInfo)
        {
            int count = (int)values.GetInteger("number of bounding boxes");
            if (count < 1)
                return;

            uint address = values.GetInteger("bounding box table address");
            var layout = buildInfo.Layouts.GetLayout("model bounding box");
            var entries = ReflexiveReader.ReadReflexive(reader, 1, address, layout, metaArea);

            // Just take the first bounding box
            // Is it even possible for models to have more than one?
            BoundingBox = new ThirdGenModelBoundingBox(entries.First());
        }
    }
}
