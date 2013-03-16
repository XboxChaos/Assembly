using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Resources
{
    public class ThirdGenResourceLayoutTable
    {
        public ThirdGenResourceLayoutTable(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            Load(values, reader, metaArea, buildInfo);
        }

        public ThirdGenCacheFileReference[] FileReferences { get; private set; }
        public ThirdGenResourcePage[] Pages { get; private set; }
        public ThirdGenResourceSegment[] Segments { get; private set; }

        private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            LoadFileReferences(values, reader, metaArea, buildInfo);
            LoadPageInfo(values, reader, metaArea, buildInfo, FileReferences);
            LoadSegments(values, reader, metaArea, buildInfo, Pages);
        }

        private void LoadFileReferences(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            int count = (int)values.GetInteger("number of external cache files");
            uint address = values.GetInteger("external cache file table address");
            var layout = buildInfo.GetLayout("external cache file table entry");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

            FileReferences = (from entry in entries
                              select new ThirdGenCacheFileReference(entry)).ToArray();
        }

        private void LoadPageInfo(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo, ThirdGenCacheFileReference[] fileReferences)
        {
            int count = (int)values.GetInteger("number of raw pages");
            uint address = values.GetInteger("raw page table address");
            var layout = buildInfo.GetLayout("raw page table entry");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

            Pages = (from entry in entries
                        select new ThirdGenResourcePage(entry, fileReferences)).ToArray();
        }

        private void LoadSegments(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo, ThirdGenResourcePage[] pages)
        {
            int count = (int)values.GetInteger("number of raw segments");
            uint address = values.GetInteger("raw segment table address");
            var layout = buildInfo.GetLayout("raw segment table entry");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

            Segments = (from entry in entries
                        select new ThirdGenResourceSegment(entry, pages)).ToArray();
        }
    }
}
