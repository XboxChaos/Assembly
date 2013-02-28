using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Resources.Models;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam.ThirdGen.Resources.Models
{
    public class ThirdGenRenderModel : IRenderModel
    {
        public ThirdGenRenderModel(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            Load(values, reader, metaArea, buildInfo);
        }

        public IModelRegion[] Regions { get; private set; }

        public IModelSection[] Sections { get; private set; }

        public DatumIndex ResourceIndex { get; private set; }

        private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            ResourceIndex = new DatumIndex(values.GetInteger("resource datum index"));

            LoadRegions(values, reader, metaArea, buildInfo);
            LoadSections(values, reader, metaArea, buildInfo);
        }

        private void LoadRegions(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            int count = (int)values.GetInteger("number of regions");
            uint address = values.GetInteger("region table address");
            var layout = buildInfo.GetLayout("model region");
            var entries = ReflexiveReader.ReadReflexive(count, address, reader, layout, metaArea);

            Regions = (from entry in entries
                       select new ThirdGenModelRegion(entry, reader, metaArea, buildInfo)).ToArray();
        }

        private void LoadSections(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            int count = (int)values.GetInteger("number of sections");
            uint address = values.GetInteger("section table address");
            var layout = buildInfo.GetLayout("model section");
            var entries = ReflexiveReader.ReadReflexive(count, address, reader, layout, metaArea);

            Sections = (from entry in entries
                        select new ThirdGenModelSection(entry, reader, metaArea, buildInfo)).ToArray();
        }
    }
}
