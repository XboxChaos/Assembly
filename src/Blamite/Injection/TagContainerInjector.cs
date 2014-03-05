using System;
using System.Collections.Generic;
using System.IO;
using Blamite.Blam;
using Blamite.Blam.Resources;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Injection
{
	public class TagContainerInjector
	{
		private static int SoundClass = CharConstant.FromString("snd!");
		private readonly ICacheFile _cacheFile;
		private readonly TagContainer _container;

		private readonly Dictionary<DataBlock, uint> _dataBlockAddresses = new Dictionary<DataBlock, uint>();
		private readonly List<StringID> _injectedStrings = new List<StringID>();
		private readonly Dictionary<ResourcePage, int> _pageIndices = new Dictionary<ResourcePage, int>();

		private readonly Dictionary<ExtractedPage, int> _extractedResourcePages = new Dictionary<ExtractedPage, int>();

		private readonly Dictionary<ExtractedResourceInfo, DatumIndex> _resourceIndices =
			new Dictionary<ExtractedResourceInfo, DatumIndex>();

		private readonly Dictionary<ExtractedTag, DatumIndex> _tagIndices = new Dictionary<ExtractedTag, DatumIndex>();
		private ResourceTable _resources;
		private IZoneSetTable _zoneSets;

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

		public ICollection<ExtractedPage> InjectedExtractedResourcePages
		{
			get { return _extractedResourcePages.Keys; }
		}

		public ICollection<ExtractedResourceInfo> InjectedResources
		{
			get { return _resourceIndices.Keys; }
		}

		public ICollection<StringID> InjectedStringIDs
		{
			get { return _injectedStrings.AsReadOnly(); }
		}

		public void SaveChanges(IStream stream)
		{
			if (_resources != null)
			{
				_cacheFile.Resources.SaveResourceTable(_resources, stream);
				_resources = null;
			}
			if (_zoneSets != null && _zoneSets.GlobalZoneSet != null)
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
			ITag existingTag = _cacheFile.Tags.FindTagByName(tag.Name, tag.Class, _cacheFile.FileNames);
			if (existingTag != null)
				return existingTag.Index;

			// If the tag has made it this far but is a sound, make everyone (especially gerit) shut up.
			if (tag.Class == SoundClass)
				return DatumIndex.Null;

			// Look up the tag's datablock to get its size and allocate a tag for it
			DataBlock tagData = _container.FindDataBlock(tag.OriginalAddress);
			if (_resources == null && BlockNeedsResources(tagData))
			{
				// If the tag relies on resources and that info isn't available, throw it out
				LoadResourceTable(stream);
				if (_resources == null)
					return DatumIndex.Null;
			}

			ITag newTag = _cacheFile.Tags.AddTag(tag.Class, tagData.Data.Length, stream);
			_tagIndices[tag] = newTag.Index;
			_cacheFile.FileNames.SetTagName(newTag, tag.Name);

			// Write the data
			WriteDataBlock(tagData, newTag.MetaLocation, stream);

			// Make the tag load
			LoadZoneSets(stream);
			if (_zoneSets != null && _zoneSets.GlobalZoneSet != null)
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
			newAddress = _cacheFile.Allocator.Allocate(block.Data.Length, (uint)block.Alignment, stream);
			SegmentPointer location = SegmentPointer.FromPointer(newAddress, _cacheFile.MetaArea);
			WriteDataBlock(block, location, stream);
			return newAddress;
		}

		public uint InjectDataBlock(uint originalAddress, IStream stream)
		{
			return InjectDataBlock(_container.FindDataBlock(originalAddress), stream);
		}

		public int InjectResourcePage(ResourcePage page, IStream stream)
		{
			if (_resources == null)
				return -1;
			if (page == null)
				throw new ArgumentNullException("page is null");

			// Don't inject the page if it's already been injected
			int newIndex;
			if (_pageIndices.TryGetValue(page, out newIndex))
				return newIndex;

			// Add the page and associate its new index with it
			var extractedRaw = _container.FindExtractedResourcePage(page.Index);
			newIndex = _resources.Pages.Count;
			page.Index = newIndex; // haxhaxhax, oh aaron
			LoadResourceTable(stream);

			// Inject?
			if (extractedRaw != null)
			{
				var rawOffset = InjectExtractedResourcePage(page, extractedRaw, stream);
				page.Offset = rawOffset;
				page.FilePath = null;
			}

			_resources.Pages.Add(page);
			_pageIndices[page] = newIndex;
			return newIndex;
		}

		public int InjectResourcePage(int originalIndex, IStream stream)
		{
			return InjectResourcePage(_container.FindResourcePage(originalIndex), stream);
		}

		public int InjectExtractedResourcePage(ResourcePage resourcePage, ExtractedPage extractedPage, IStream stream)
		{
			if (extractedPage == null)
				throw new ArgumentNullException("extractedPage");

			var injector = new ResourcePageInjector(_cacheFile);
			var rawOffset = injector.InjectPage(stream, resourcePage, extractedPage.ExtractedPageData);
			
			_extractedResourcePages[extractedPage] = extractedPage.ResourcePageIndex;

			return rawOffset;
		}

		public DatumIndex InjectResource(ExtractedResourceInfo resource, IStream stream)
		{
			if (_resources == null)
				return DatumIndex.Null;
			if (resource == null)
				throw new ArgumentNullException("resource is null");

			// Don't inject the resource if it's already been injected
			DatumIndex newIndex;
			if (_resourceIndices.TryGetValue(resource, out newIndex))
				return newIndex;

			// Create a new datum index for it (0x4152 = 'AR') and associate it
			LoadResourceTable(stream);
			newIndex = new DatumIndex(0x4152, (ushort) _resources.Resources.Count);
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
				DatumIndex parentTagIndex = InjectTag(resource.OriginalParentTagIndex, stream);
				newResource.ParentTag = _cacheFile.Tags[parentTagIndex];
			}
			if (resource.Location != null)
			{
				newResource.Location = new ResourcePointer();

				// Primary page pointers
				if (resource.Location.OriginalPrimaryPageIndex >= 0)
				{
					int primaryPageIndex = InjectResourcePage(resource.Location.OriginalPrimaryPageIndex, stream);
					newResource.Location.PrimaryPage = _resources.Pages[primaryPageIndex];
				}
				newResource.Location.PrimaryOffset = resource.Location.PrimaryOffset;
				newResource.Location.PrimaryUnknown = resource.Location.PrimaryUnknown;

				// Secondary page pointers
				if (resource.Location.OriginalSecondaryPageIndex >= 0)
				{
					int secondaryPageIndex = InjectResourcePage(resource.Location.OriginalSecondaryPageIndex, stream);
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
			if (_zoneSets != null && _zoneSets.GlobalZoneSet != null)
				_zoneSets.GlobalZoneSet.ActivateResource(newResource, true);

			return newIndex;
		}

		public DatumIndex InjectResource(DatumIndex originalIndex, IStream stream)
		{
			return InjectResource(_container.FindResource(originalIndex), stream);
		}

		private bool BlockNeedsResources(DataBlock block)
		{
			if (block.ResourceFixups.Count > 0)
				return true;

			foreach (var addrFixup in block.AddressFixups)
			{
				var subBlock = _container.FindDataBlock(addrFixup.OriginalAddress);
				if (subBlock != null && BlockNeedsResources(subBlock))
					return true;
			}
			return false;
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
				FixStringIdReferences(block, bufferWriter);

				// Write the buffer to the file
				stream.SeekTo(location.AsOffset());
				stream.WriteBlock(buffer.GetBuffer(), 0, (int) buffer.Length);
			}

			// Write shader fixups (they can't be done in-memory because they require cache file expansion)
			FixShaderReferences(block, stream, location);
		}

		private void FixBlockReferences(DataBlock block, IWriter buffer, IStream stream)
		{
			foreach (DataBlockAddressFixup fixup in block.AddressFixups)
			{
				uint newAddress = InjectDataBlock(fixup.OriginalAddress, stream);
				buffer.SeekTo(fixup.WriteOffset);
				buffer.WriteUInt32(newAddress);
			}
		}

		private void FixTagReferences(DataBlock block, IWriter buffer, IStream stream)
		{
			foreach (DataBlockTagFixup fixup in block.TagFixups)
			{
				DatumIndex newIndex = InjectTag(fixup.OriginalIndex, stream);
				buffer.SeekTo(fixup.WriteOffset);
				buffer.WriteUInt32(newIndex.Value);
			}
		}

		private void FixResourceReferences(DataBlock block, IWriter buffer, IStream stream)
		{
			foreach (DataBlockResourceFixup fixup in block.ResourceFixups)
			{
				DatumIndex newIndex = InjectResource(fixup.OriginalIndex, stream);
				buffer.SeekTo(fixup.WriteOffset);
				buffer.WriteUInt32(newIndex.Value);
			}
		}

		private void FixStringIdReferences(DataBlock block, IWriter buffer)
		{
			foreach (DataBlockStringIDFixup fixup in block.StringIDFixups)
			{
				// Try to find the string, and if it's not found, inject it
				StringID newSID = _cacheFile.StringIDs.FindStringID(fixup.OriginalString);
				if (newSID == StringID.Null)
				{
					newSID = _cacheFile.StringIDs.AddString(fixup.OriginalString);
					_injectedStrings.Add(newSID);
				}

				buffer.SeekTo(fixup.WriteOffset);
				buffer.WriteUInt32(newSID.Value);
			}
		}

		private void FixShaderReferences(DataBlock block, IStream stream, SegmentPointer baseOffset)
		{
			foreach (DataBlockShaderFixup fixup in block.ShaderFixups)
			{
				stream.SeekTo(baseOffset.AsOffset() + fixup.WriteOffset);
				_cacheFile.ShaderStreamer.ImportShader(fixup.Data, stream);
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