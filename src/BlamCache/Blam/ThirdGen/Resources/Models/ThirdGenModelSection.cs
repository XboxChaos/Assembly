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
    public class ThirdGenModelSection : IModelSection
    {
        public ThirdGenModelSection(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            Load(values, reader, metaArea, buildInfo);
        }

        public IModelSubmesh[] Submeshes { get; private set; }

        public IModelVertexGroup[] VertexGroups { get; private set; }

        public int VertexFormat { get; private set; }

        private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            VertexFormat = (int)values.GetNumber("vertex format");

            LoadSubmeshes(values, reader, metaArea, buildInfo);
            LoadVertexGroups(values, reader, metaArea, buildInfo, Submeshes);
        }

        private void LoadSubmeshes(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            int count = (int)values.GetNumber("number of submeshes");
            uint address = values.GetNumber("submesh table address");
            var layout = buildInfo.GetLayout("model submesh");
            var entries = ReflexiveReader.ReadReflexive(count, address, reader, layout, metaArea);

            Submeshes = (from entry in entries
                         select new ThirdGenModelSubmesh(entry)).ToArray();
        }

        private void LoadVertexGroups(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo, IModelSubmesh[] submeshes)
        {
            int count = (int)values.GetNumber("number of vertex groups");
            uint address = values.GetNumber("vertex group table address");
            var layout = buildInfo.GetLayout("model vertex group");
            var entries = ReflexiveReader.ReadReflexive(count, address, reader, layout, metaArea);

            VertexGroups = (from entry in entries
                            select new ThirdGenModelVertexGroup(entry, submeshes)).ToArray();
        }
    }
}
