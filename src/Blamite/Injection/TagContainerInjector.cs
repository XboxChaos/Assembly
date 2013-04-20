using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Blamite.Blam;
using Blamite.Blam.Resources;
using Blamite.IO;

namespace Blamite.Injection
{
    public class TagContainerInjector
    {
        private ICacheFile _cacheFile;
        private TagContainer _container;
        private ResourceTable _resources;
        private IZoneSetTable _zoneSets;

        private Dictionary<DataBlock, uint> _dataBlockAddresses = new Dictionary<DataBlock, uint>();
        private Dictionary<ExtractedTag, DatumIndex> _tagIndices = new Dictionary<ExtractedTag, DatumIndex>();
        private Dictionary<ResourcePage, int> _pageIndices = new Dictionary<ResourcePage, int>();
        private Dictionary<ExtractedResourceInfo, DatumIndex> _resourceIndices = new Dictionary<ExtractedResourceInfo, DatumIndex>();

        public TagContainerInjector(ICacheFile cacheFile, TagContainer container)
        {
            _cacheFile = cacheFile;
            _container = container;
        }

        public ICollection<DataBlock> InjectedBlocks
        {
            get { return _dataBlockAddresses.Keys; }
        }

        public ICollection<ExtractedTag> InjectedTags
        {
            get { return _tagIndices.Keys; }
        }

        public ICollection<ResourcePage> InjectedPages
        {
            get { return _pageIndices.Keys; }
        }

        public ICollection<ExtractedResourceInfo> InjectedResources
        {
            get { return _resourceIndices.Keys; }
        }

        public void SaveChanges(IStream stream)
        {
            if (_resources != null)
            {
                _cacheFile.Resources.SaveResourceTable(_resources, stream);
                _resources = null;
            }
            if (_zoneSets != null)
            {
                _zoneSets.SaveChanges(stream);
                _zoneSets = null;
            }
            _cacheFile.SaveChanges(stream);
        }

        public DatumIndex InjectTag(ExtractedTag tag, IStream stream)
        {
            if (tag == null)
                throw new ArgumentNullException("tag is null");

            // Don't inject the tag if it's already been injected
            DatumIndex newIndex;
            if (_tagIndices.TryGetValue(tag, out newIndex))
                return newIndex;

            // Make sure there isn't already a tag with the given name
            var existingTag = _cacheFile.Tags.FindTagByName(tag.Name, tag.Class, _cacheFile.FileNames);
            if (existingTag != null)
                return existingTag.Index;

            // Look up the tag's datablock to get its size and allocate a tag for it
            var tagData = _container.FindDataBlock(tag.OriginalAddress);
            var newTag = _cacheFile.Tags.AddTag(tag.Class, tagData.Data.Length, stream);
            _tagIndices[tag] = newTag.Index;

            // Write the data
            WriteDataBlock(tagData, newTag.MetaLocation, stream);

            // Make the tag load
            LoadZoneSets(stream);
            _zoneSets.GlobalZoneSet.ActivateTag(newTag, true);

            return newTag.Index;
        }

        public DatumIndex InjectTag(DatumIndex originalIndex, IStream stream)
        {
            return InjectTag(_container.FindTag(originalIndex), stream);
        }

        public uint InjectDataBlock(DataBlock block, IStream stream)
        {
            if (block == null)
                throw new ArgumentNullException("block is null");

            // Don't inject the block if it's already been injected
            uint newAddress;
            if (_dataBlockAddresses.TryGetValue(block, out newAddress))
                return newAddress;

            // Allocate space for it and write it to the file
            newAddress = _cacheFile.Allocator.Allocate(block.Data.Length, stream);
            var location = SegmentPointer.FromPointer(newAddress, _cacheFile.MetaArea);
            WriteDataBlock(block, location, stream);
            return newAddress;
        }

        public uint InjectDataBlock(uint originalAddress, IStream stream)
        {
            return InjectDataBlock(_container.FindDataBlock(originalAddress), stream);
        }

        public int InjectResourcePage(ResourcePage page, IReader reader)
        {
            if (page == null)
                throw new ArgumentNullException("page is null");

            // Don't inject the page if it's already been injected
            int newIndex;
            if (_pageIndices.TryGetValue(page, out newIndex))
                return newIndex;

            // Add the page and associate its new index with it
            newIndex = _resources.Pages.Count;
            page.Index = newIndex; // haxhaxhax
            LoadResourceTable(reader);
            _resources.Pages.Add(page);
            _pageIndices[page] = newIndex;
            return newIndex;
        }

        public int InjectResourcePage(int originalIndex, IReader reader)
        {
            return InjectResourcePage(_container.FindResourcePage(originalIndex), reader);
        }

        public DatumIndex InjectResource(ExtractedResourceInfo resource, IStream stream)
        {
            if (resource == null)
                throw new ArgumentNullException("resource is null");

            // Don't inject the resource if it's already been injected
            DatumIndex newIndex;
            if (_resourceIndices.TryGetValue(resource, out newIndex))
                return newIndex;

            // Create a new datum index for it (0x4152 = 'AR') and associate it
            LoadResourceTable(stream);
            newIndex = new DatumIndex(0x4152, (ushort)_resources.Resources.Count);
            _resourceIndices[resource] = newIndex;

            // Create a native resource for it
            var newResource = new Resource();
            _resources.Resources.Add(newResource);
            newResource.Index = newIndex;
            newResource.Flags = resource.Flags;
            newResource.Type = resource.Type;
            newResource.Info = resource.Info;
            if (resource.OriginalParentTagIndex.IsValid)
            {
                var parentTagIndex = InjectTag(resource.OriginalParentTagIndex, stream);
                newResource.ParentTag = _cacheFile.Tags[parentTagIndex];
            }
            if (resource.Location != null)
            {
                newResource.Location = new ResourcePointer();

                // Primary page pointers
                if (resource.Location.OriginalPrimaryPageIndex >= 0)
                {
                    var primaryPageIndex = InjectResourcePage(resource.Location.OriginalPrimaryPageIndex, stream);
                    newResource.Location.PrimaryPage = _resources.Pages[primaryPageIndex];
                }
                newResource.Location.PrimaryOffset = resource.Location.PrimaryOffset;
                newResource.Location.PrimaryUnknown = resource.Location.PrimaryUnknown;

                // Secondary page pointers
                if (resource.Location.OriginalSecondaryPageIndex >= 0)
                {
                    var secondaryPageIndex = InjectResourcePage(resource.Location.OriginalSecondaryPageIndex, stream);
                    newResource.Location.SecondaryPage = _resources.Pages[secondaryPageIndex];
                }
                newResource.Location.SecondaryOffset = resource.Location.SecondaryOffset;
                newResource.Location.SecondaryUnknown = resource.Location.SecondaryUnknown;
            }

            newResource.ResourceFixups.AddRange(resource.ResourceFixups);
            newResource.DefinitionFixups.AddRange(resource.DefinitionFixups);

            newResource.Unknown1 = resource.Unknown1;
            newResource.Unknown2 = resource.Unknown2;
            newResource.Unknown3 = resource.Unknown3;

            // Make it load
            LoadZoneSets(stream);
            _zoneSets.GlobalZoneSet.ActivateResource(newResource, true);

            return newIndex;
        }

        public DatumIndex InjectResource(DatumIndex originalIndex, IStream stream)
        {
            return InjectResource(_container.FindResource(originalIndex), stream);
        }

        private void WriteDataBlock(DataBlock block, SegmentPointer location, IStream stream)
        {
            // Don't write anything if the block has already been written
            if (_dataBlockAddresses.ContainsKey(block))
                return;

            // Associate the location with the block
            _dataBlockAddresses[block] = location.AsPointer();

            // Create a MemoryStream and write the block data to it (so fixups can be performed before writing it to the file)
            using (var buffer = new MemoryStream(block.Data.Length))
            {
                var bufferWriter = new EndianWriter(buffer, stream.Endianness);
                bufferWriter.WriteBlock(block.Data);

                // Apply fixups
                FixBlockReferences(block, bufferWriter, stream);
                FixTagReferences(block, bufferWriter, stream);
                FixResourceReferences(block, bufferWriter, stream);

                // Write the buffer to the file
                stream.SeekTo(location.AsOffset());
                stream.WriteBlock(buffer.GetBuffer(), 0, (int)buffer.Length);
            }
        }

        private void FixBlockReferences(DataBlock block, IWriter buffer, IStream stream)
        {
            foreach (var fixup in block.AddressFixups)
            {
                uint newAddress = InjectDataBlock(fixup.OriginalAddress, stream);
                buffer.SeekTo(fixup.WriteOffset);
                buffer.WriteUInt32(newAddress);
            }
        }

        private void FixTagReferences(DataBlock block, IWriter buffer, IStream stream)
        {
            foreach (var fixup in block.TagFixups)
            {
                var newIndex = InjectTag(fixup.OriginalIndex, stream);
                buffer.SeekTo(fixup.WriteOffset);
                buffer.WriteUInt32(newIndex.Value);
            }
        }

        private void FixResourceReferences(DataBlock block, IWriter buffer, IStream stream)
        {
            foreach (var fixup in block.ResourceFixups)
            {
                var newIndex = InjectResource(fixup.OriginalIndex, stream);
                buffer.SeekTo(fixup.WriteOffset);
                buffer.WriteUInt32(newIndex.Value);
            }
        }

        private void FixStringIDReferences(DataBlock block, IWriter buffer)
        {
            foreach (var fixup in block.StringIDFixups)
            {
                // Try to find the string, and if it's not found, just skip over it
                // TODO: Actually inject it if it isn't found
                StringID newSID = _cacheFile.StringIDs.FindStringID(fixup.OriginalString);
                if (newSID != StringID.Null)
                {
                    buffer.SeekTo(fixup.WriteOffset);
                    buffer.WriteUInt32(newSID.Value);
                }
            }
        }

        private void LoadResourceTable(IReader reader)
        {
            if (_resources == null)
                _resources = _cacheFile.Resources.LoadResourceTable(reader);
        }

        private void LoadZoneSets(IReader reader)
        {
            if (_zoneSets == null)
                _zoneSets = _cacheFile.Resources.LoadZoneSets(reader);
        }
    }
}
