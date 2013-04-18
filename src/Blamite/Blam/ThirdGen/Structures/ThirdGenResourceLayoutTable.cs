using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Blamite.Blam.Resources;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Resources
{
    public class ThirdGenResourceLayoutTable
    {
        private ITag _tag;
        private FileSegmentGroup _metaArea;
        private MetaAllocator _allocator;
        private BuildInformation _buildInfo;

        public ThirdGenResourceLayoutTable(ITag playTag, FileSegmentGroup metaArea, MetaAllocator allocator, BuildInformation buildInfo)
        {
            _tag = playTag;
            _metaArea = metaArea;
            _allocator = allocator;
            _buildInfo = buildInfo;
        }

        public IEnumerable<ResourcePage> LoadPages(IReader reader)
        {
            var values = LoadTag(reader);
            var files = LoadExternalFiles(values, reader);

            int count = (int)values.GetInteger("number of raw pages");
            uint address = values.GetInteger("raw page table address");
            var layout = _buildInfo.GetLayout("raw page table entry");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
            return entries.Select((e, i) => LoadPage(e, i, files));
        }

        public void SavePages(ICollection<ResourcePage> pages, IStream stream)
        {
            var values = LoadTag(stream);
            var files = LoadExternalFiles(values, stream);
            
            var layout = _buildInfo.GetLayout("raw page table entry");
            int oldCount = (int)values.GetInteger("number of raw pages");
            uint oldAddress = values.GetInteger("raw page table address");
            var entries = pages.Select(p => SerializePage(p, files));
            uint newAddress = ReflexiveWriter.WriteReflexive(entries, oldCount, oldAddress, pages.Count, layout, _metaArea, _allocator, stream);

            // Update values
            values.SetInteger("number of raw pages", (uint)pages.Count);
            values.SetInteger("raw page table address", newAddress);
            SaveTag(values, stream);
        }

        public IEnumerable<ResourcePointer> LoadPointers(IReader reader, IList<ResourcePage> pages)
        {
            var values = LoadTag(reader);
            int count = (int)values.GetInteger("number of raw segments");
            uint address = values.GetInteger("raw segment table address");
            var layout = _buildInfo.GetLayout("raw segment table entry");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
            return entries.Select(e => LoadPointer(e, pages));
        }

        public void SavePointers(ICollection<ResourcePointer> pointers, IStream stream)
        {
            var values = LoadTag(stream);

            var layout = _buildInfo.GetLayout("raw segment table entry");
            int oldCount = (int)values.GetInteger("number of raw segments");
            uint oldAddress = values.GetInteger("raw segment table address");
            var entries = pointers.Select(p => SerializePointer(p));
            uint newAddress = ReflexiveWriter.WriteReflexive(entries, oldCount, oldAddress, pointers.Count, layout, _metaArea, _allocator, stream);

            // Update values
            values.SetInteger("number of raw segments", (uint)pointers.Count);
            values.SetInteger("raw segment table address", newAddress);
            SaveTag(values, stream);
        }

        private StructureValueCollection LoadTag(IReader reader)
        {
            reader.SeekTo(_tag.MetaLocation.AsOffset());
            return StructureReader.ReadStructure(reader, _buildInfo.GetLayout("resource layout table"));
        }

        private void SaveTag(StructureValueCollection values, IWriter writer)
        {
            writer.SeekTo(_tag.MetaLocation.AsOffset());
            StructureWriter.WriteStructure(values, _buildInfo.GetLayout("resource layout table"), writer);
        }

        private ThirdGenCacheFileReference[] LoadExternalFiles(StructureValueCollection values, IReader reader)
        {
            int count = (int)values.GetInteger("number of external cache files");
            uint address = values.GetInteger("external cache file table address");
            var layout = _buildInfo.GetLayout("external cache file table entry");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
            return entries.Select(e => new ThirdGenCacheFileReference(e)).ToArray();
        }

        private ResourcePage LoadPage(StructureValueCollection values, int index, ThirdGenCacheFileReference[] externalFiles)
        {
            ResourcePage result = new ResourcePage();
            result.Index = index;
            result.Salt = (ushort)values.GetInteger("salt");
            result.Flags = (byte)values.GetInteger("flags");
            result.CompressionMethod = ((int)values.GetInteger("compression codec index") != -1) ? ResourcePageCompression.Deflate : ResourcePageCompression.None; // FIXME: hax/laziness
            int externalFile = (int)values.GetInteger("shared cache file index");
            result.FilePath = (externalFile != -1) ? externalFiles[externalFile].Path : null;
            result.Unknown1 = (int)values.GetIntegerOrDefault("unknown 1", 0);
            result.Offset = (int)values.GetInteger("compressed block offset");
            result.CompressedSize = (int)values.GetInteger("compressed block size");
            result.UncompressedSize = (int)values.GetInteger("uncompressed block size");
            result.Checksum = values.GetInteger("checksum");
            result.Hash1 = values.GetRaw("hash 1");
            result.Hash2 = values.GetRaw("hash 2");
            result.Hash3 = values.GetRaw("hash 3");
            result.Unknown2 = (int)values.GetIntegerOrDefault("unknown 2", 0);
            result.Unknown3 = (int)values.GetIntegerOrDefault("unknown 3", 0);
            return result;
        }

        private StructureValueCollection SerializePage(ResourcePage page, ThirdGenCacheFileReference[] externalFiles)
        {
            StructureValueCollection result = new StructureValueCollection();
            result.SetInteger("salt", (uint)page.Salt);
            result.SetInteger("flags", (uint)page.Flags);
            result.SetInteger("compression codec index", (page.CompressionMethod != ResourcePageCompression.None) ? 0 : 0xFFFFFFFF);
            result.SetInteger("shared cache file index", (page.FilePath != null) ? (uint)FindExternalFile(externalFiles, page.FilePath) : 0xFFFFFFFF);
            result.SetInteger("unknown 1", (uint)page.Unknown1);
            result.SetInteger("compressed block offset", (uint)page.Offset);
            result.SetInteger("compressed block size", (uint)page.CompressedSize);
            result.SetInteger("uncompressed block size", (uint)page.UncompressedSize);
            result.SetInteger("checksum", page.Checksum);
            result.SetRaw("hash 1", page.Hash1);
            result.SetRaw("hash 2", page.Hash2);
            result.SetRaw("hash 3", page.Hash3);
            result.SetInteger("unknown 2", (uint)page.Unknown2);
            result.SetInteger("unknown 3", (uint)page.Unknown3);
            return result;
        }

        private int FindExternalFile(ThirdGenCacheFileReference[] externalFiles, string path)
        {
            for (int i = 0; i < externalFiles.Length; i++)
            {
                if (externalFiles[i].Path.Equals(path, StringComparison.InvariantCultureIgnoreCase))
                    return i;
            }
            throw new InvalidOperationException("Invalid shared map path \"" + path + "\"");
        }

        private ResourcePointer LoadPointer(StructureValueCollection values, IList<ResourcePage> pages)
        {
            ResourcePointer result = new ResourcePointer();
            int primaryPage = (int)values.GetInteger("primary page index");
            result.PrimaryPage = (primaryPage != -1) ? pages[primaryPage] : null;
            result.PrimaryOffset = (int)values.GetInteger("primary offset");
            result.PrimaryUnknown = (int)values.GetInteger("primary unknown");

            int secondaryPage = (int)values.GetInteger("secondary page index");
            result.SecondaryPage = (secondaryPage != -1) ? pages[secondaryPage] : null;
            result.SecondaryOffset = (int)values.GetInteger("secondary offset");
            result.SecondaryUnknown = (int)values.GetInteger("secondary unknown");

            return result;
        }

        private StructureValueCollection SerializePointer(ResourcePointer pointer)
        {
            StructureValueCollection result = new StructureValueCollection();
            result.SetInteger("primary page index", (pointer.PrimaryPage != null) ? (uint)pointer.PrimaryPage.Index : 0xFFFFFFFF);
            result.SetInteger("primary offset", (uint)pointer.PrimaryOffset);
            result.SetInteger("primary unknown", (uint)pointer.PrimaryUnknown);
            result.SetInteger("secondary page index", (pointer.SecondaryPage != null) ? (uint)pointer.SecondaryPage.Index : 0xFFFFFFFF);
            result.SetInteger("secondary offset", (uint)pointer.SecondaryOffset);
            result.SetInteger("secondary unknown", (uint)pointer.SecondaryUnknown);
            return result;
        }
    }
}
