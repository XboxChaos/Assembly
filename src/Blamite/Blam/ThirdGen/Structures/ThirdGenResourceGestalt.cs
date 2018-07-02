using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Resources;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Structures
{
	public class ThirdGenResourceType
	{
		public ThirdGenResourceType(StructureValueCollection values, StringIDSource stringIDs)
		{
			Load(values, stringIDs);
		}

		/// <summary>
		///     Gets the GUID of the resource type.
		/// </summary>
		public byte[] Guid { get; private set; }

		/// <summary>
		///     Gets the name of the resource type.
		/// </summary>
		public string Name { get; private set; }

		private void Load(StructureValueCollection values, StringIDSource stringIDs)
		{
			Guid = values.GetRaw("guid");
			Name = stringIDs.GetString(new StringID(values.GetInteger("name stringid")));
		}
	}

	public class ThirdGenResourceGestalt
	{
		private readonly MetaAllocator _allocator;
		private readonly EngineDescription _buildInfo;
		private readonly FileSegmentGroup _metaArea;
		private readonly ITag _tag;
		private ThirdGenResourceType[] _resourceTypes;

		public ThirdGenResourceGestalt(IReader reader, ITag zoneTag, FileSegmentGroup metaArea, MetaAllocator allocator,
			StringIDSource stringIDs, EngineDescription buildInfo)
		{
			_tag = zoneTag;
			_metaArea = metaArea;
			_allocator = allocator;
			_buildInfo = buildInfo;

			Load(reader, stringIDs);
		}

		public IEnumerable<Resource> LoadResources(IReader reader, TagTable tags, IList<ResourcePointer> pointers)
		{
			StructureValueCollection values = LoadTag(reader);
			byte[] infoBuffer = LoadResourceInfoBuffer(values, reader);

			var count = (int)values.GetInteger("number of resources");
			uint address = values.GetInteger("resource table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("resource table entry");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
			return entries.Select((e, i) => LoadResource(e, i, tags, pointers, infoBuffer, reader));
		}

		public IList<ResourcePointer> SaveResources(ICollection<Resource> resources, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);

			// Free everything
			FreeResources(values, stream);

			// Serialize each resource entry
			// This can't be lazily evaluated because allocations might cause the stream to expand
			int infoOffset = 0;
			int altInfoOffset = 0;
			var infocache = new ReflexiveCache<int>();

			var pointers = new List<ResourcePointer>();
			var entries = new List<StructureValueCollection>();
			var fixupCache = new ReflexiveCache<ResourceFixup>();
			var defFixupCache = new ReflexiveCache<ResourceDefinitionFixup>();

			List<byte[]> paddedInfos = new List<byte[]>();

			foreach (Resource resource in resources)
			{

				StructureValueCollection entry = SerializeResource(resource, (resource.Location != null) ? pointers.Count : -1, stream);
				entries.Add(entry);

				// Save fixups
				SaveResourceFixups(resource.ResourceFixups, entry, stream, fixupCache);
				SaveDefinitionFixups(resource.DefinitionFixups, entry, stream, defFixupCache);

				uint bits = entry.GetInteger("resource bits");
				int size = (int)entry.GetInteger("resource info size");

				List<int> offsets = new List<int>();

				if (size > 0)
				{
					int dirtyoffset = infoOffset;

					infoOffset = AlignInfoBlockOffset(resource, infoOffset);

					offsets.Add(infoOffset);

					int padding =  infoOffset - dirtyoffset;

					byte[] temp = new byte[padding + size];

					Buffer.BlockCopy(resource.Info, 0, temp, padding, size);

					paddedInfos.Add(temp);

					infoOffset += size;
				}
				else
					offsets.Add(0);

				if ((bits & 2) > 0)
				{
					int tempalt = AlignInfoBlockOffset(resource, altInfoOffset);
					offsets.Add(tempalt);

					altInfoOffset += size;
				}
				else
					offsets.Add(size > 0 ? -1 : 0);

				if ((bits & 4) > 0)
				{
					int tempalt = AlignInfoBlockOffset(resource, altInfoOffset);
					offsets.Add(tempalt);

					altInfoOffset += size;
				}
				else
					offsets.Add(size > 0 ? -1 : 0);

				if (_buildInfo.HeaderSize == 0x1E000)//h4
				{
					var oldCount = (int)entry.GetIntegerOrDefault("number of resource info offsets", 0);
					uint oldAddress = entry.GetIntegerOrDefault("resource info offsets table address", 0);
					StructureLayout infolayout = _buildInfo.Layouts.GetLayout("resource info offset entry");

					if (size > 0)
					{
						uint newBlockAddress;

						// Write a new reflexive
						IEnumerable<StructureValueCollection> infoentries = offsets.Select(f => SerializeInfos(f));
						newBlockAddress = ReflexiveWriter.WriteReflexive(infoentries, oldCount, oldAddress, offsets.Count, infolayout, _metaArea,
							_allocator, stream);
						infocache.Add(newBlockAddress, offsets);

						entry.SetInteger("number of resource info offsets", (uint)offsets.Count);
						entry.SetInteger("resource info offsets table address", newBlockAddress);
					}
					else
					{
						entry.SetInteger("number of resource info offsets", 0);
						entry.SetInteger("resource info offsets table address", 0);
					}
				}
				else
				{
					if (size > 0)
					{
						entry.SetInteger("resource info offset", (uint)offsets[0]);
						entry.SetInteger("alt resource info offset", (uint)offsets[1]);
					}
					else
					{
						entry.SetInteger("resource info offset", 0);
						entry.SetInteger("alt resource info offset", 0);
					}

				}
					
				// Update pointer info
				if (resource.Location != null)
					pointers.Add(resource.Location);
			}

			// Write the reflexive and update the tag values
			StructureLayout layout = _buildInfo.Layouts.GetLayout("resource table entry");
			uint newAddress = ReflexiveWriter.WriteReflexive(entries, layout, _metaArea, _allocator, stream);
			values.SetInteger("number of resources", (uint) entries.Count);
			values.SetInteger("resource table address", newAddress);

			// Save the info buffer
			SaveResourceInfoBuffer(paddedInfos.SelectMany(a => a).ToArray(), values, stream);

			SaveTag(values, stream);
			return pointers;
		}

		public StructureValueCollection LoadTag(IReader reader)
		{
			reader.SeekTo(_tag.MetaLocation.AsOffset());
			return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("resource gestalt"));
		}

		public void SaveTag(StructureValueCollection values, IWriter writer)
		{
			writer.SeekTo(_tag.MetaLocation.AsOffset());
			StructureWriter.WriteStructure(values, _buildInfo.Layouts.GetLayout("resource gestalt"), writer);
		}

		private void Load(IReader reader, StringIDSource stringIDs)
		{
			StructureValueCollection values = LoadTag(reader);
			LoadResourceTypes(values, reader, stringIDs);
		}

		private void LoadResourceTypes(StructureValueCollection values, IReader reader, StringIDSource stringIDs)
		{
			if (!values.HasInteger("number of resource types") || !values.HasInteger("resource type table address"))
				return;

			var count = (int) values.GetInteger("number of resource types");
			uint address = values.GetInteger("resource type table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("resource type entry");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
			_resourceTypes = entries.Select(e => new ThirdGenResourceType(e, stringIDs)).ToArray();
		}

		private byte[] LoadResourceInfoBuffer(StructureValueCollection values, IReader reader)
		{
			var size = (int) values.GetInteger("resource info buffer size");
			uint address = values.GetInteger("resource info buffer address");
			if (size <= 0 || address == 0)
				return new byte[0];

			int offset = _metaArea.PointerToOffset(address);
			reader.SeekTo(offset);
			return reader.ReadBlock(size);
		}

		private void SaveResourceInfoBuffer(byte[] buffer, StructureValueCollection values, IStream stream)
		{
			// Free the old info buffer
			var oldSize = (int) values.GetInteger("resource info buffer size");
			uint oldAddress = values.GetInteger("resource info buffer address");
			if (oldAddress >= 0 && oldSize > 0)
				_allocator.Free(oldAddress, oldSize);

			// Write a new one
			uint newAddress = 0;
			if (buffer.Length > 0)
			{
				newAddress = _allocator.Allocate(buffer.Length, 0x10, stream);
				stream.SeekTo(_metaArea.PointerToOffset(newAddress));
				stream.WriteBlock(buffer);
			}

			// Update values
			values.SetInteger("resource info buffer size", (uint) buffer.Length);
			values.SetInteger("resource info buffer address", newAddress);
		}

		private Resource LoadResource(StructureValueCollection values, int index, TagTable tags,
			IList<ResourcePointer> pointers, byte[] infoBuffer, IReader reader)
		{
			var result = new Resource();

			var parentTag = new DatumIndex(values.GetInteger("parent tag datum index"));
			result.ParentTag = parentTag.IsValid ? tags[parentTag] : null;
			var salt = (ushort) values.GetInteger("datum index salt");
			result.Index = new DatumIndex(salt, (ushort) index);
			var typeIndex = (int) values.GetInteger("resource type index");
			if (typeIndex >= 0 && typeIndex < _resourceTypes.Length)
				result.Type = _resourceTypes[typeIndex].Name;
			result.Flags = values.GetInteger("flags");

			//var infoOffset = (int) values.GetInteger("resource info offset");
			var infoSize = (int) values.GetInteger("resource info size");

			if (infoSize > 0)
			{
				var infoOffset = 0;

				if (values.HasInteger("number of resource info offsets"))//for h4
				{
					var infocount = (int)values.GetInteger("number of resource info offsets");
					uint address = values.GetInteger("resource info offsets table address");
					StructureLayout layout = _buildInfo.Layouts.GetLayout("resource info offset entry");
					StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, infocount, address, layout, _metaArea);

					if (infocount > 0)
						infoOffset = (int)entries[0].GetInteger("offset");
				}
				else
					infoOffset = (int)values.GetInteger("resource info offset");

				// Copy the section of the info buffer that the resource is pointing to
				result.Info = new byte[infoSize];
				Buffer.BlockCopy(infoBuffer, infoOffset, result.Info, 0, infoSize);
			}

			result.ResourceBits = (ushort) values.GetInteger("resource bits");
			var segmentIndex = (int) values.GetInteger("segment index");
			result.Location = (segmentIndex >= 0) ? pointers[segmentIndex] : null;
			result.BaseDefinitionAddress = (int) values.GetInteger("base definition address");

			result.ResourceFixups.AddRange(LoadResourceFixups(values, reader));
			result.DefinitionFixups.AddRange(LoadDefinitionFixups(values, reader));

			return result;
		}

		private StructureValueCollection SerializeResource(Resource resource, int pointerIndex, IStream stream)
		{
			var result = new StructureValueCollection();
			if (resource.ParentTag != null)
			{
				result.SetInteger("parent tag class magic", (uint) resource.ParentTag.Class.Magic);
				result.SetInteger("parent tag datum index", resource.ParentTag.Index.Value);
			}
			else
			{
				result.SetInteger("parent tag class magic", 0xFFFFFFFF);
				result.SetInteger("parent tag datum index", 0xFFFFFFFF);
			}
			result.SetInteger("datum index salt", resource.Index.Salt);
			result.SetInteger("resource type index", (uint) FindResourceType(resource.Type));
			result.SetInteger("flags", resource.Flags);
			//result.SetInteger("resource info offset", (uint)infoOffset);
			result.SetInteger("resource info size", (resource.Info != null) ? (uint)resource.Info.Length : 0);

			result.SetInteger("resource bits", (ushort) resource.ResourceBits);
			result.SetInteger("segment index", (uint) pointerIndex);
			result.SetInteger("base definition address", (uint) resource.BaseDefinitionAddress);
			return result;
		}

		private IEnumerable<ResourceFixup> LoadResourceFixups(StructureValueCollection values, IReader reader)
		{
			var count = (int) values.GetInteger("number of resource fixups");
			uint address = values.GetInteger("resource fixup table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("resource fixup entry");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
			return entries.Select(e => new ResourceFixup
			{
				Offset = (int) e.GetInteger("offset"),
				Address = e.GetInteger("address")
			});
		}

		private void SaveResourceFixups(IList<ResourceFixup> fixups, StructureValueCollection values, IStream stream,
			ReflexiveCache<ResourceFixup> cache)
		{
			var oldCount = (int) values.GetIntegerOrDefault("number of resource fixups", 0);
			uint oldAddress = values.GetIntegerOrDefault("resource fixup table address", 0);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("resource fixup entry");

			uint newAddress;
			if (!cache.TryGetAddress(fixups, out newAddress))
			{
				// Write a new reflexive
				IEnumerable<StructureValueCollection> entries = fixups.Select(f => SerializeResourceFixup(f));
				newAddress = ReflexiveWriter.WriteReflexive(entries, oldCount, oldAddress, fixups.Count, layout, _metaArea,
					_allocator, stream);
				cache.Add(newAddress, fixups);
			}
			else if (oldAddress != 0 && oldCount > 0)
			{
				// Reflexive was cached - just free it
				_allocator.Free(oldAddress, oldCount*layout.Size);
			}

			values.SetInteger("number of resource fixups", (uint) fixups.Count);
			values.SetInteger("resource fixup table address", newAddress);
		}

		private StructureValueCollection SerializeResourceFixup(ResourceFixup fixup)
		{
			var result = new StructureValueCollection();
			result.SetInteger("offset", (uint) fixup.Offset);
			result.SetInteger("address", fixup.Address);
			return result;
		}

		private IEnumerable<ResourceDefinitionFixup> LoadDefinitionFixups(StructureValueCollection values, IReader reader)
		{
			var count = (int) values.GetInteger("number of definition fixups");
			uint address = values.GetInteger("definition fixup table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("definition fixup entry");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
			return entries.Select(e => new ResourceDefinitionFixup
			{
				Offset = (int) e.GetInteger("offset"),
				Type = (int) e.GetInteger("type index")
			});
		}

		private StructureValueCollection SerializeDefinitionFixup(ResourceDefinitionFixup fixup)
		{
			var result = new StructureValueCollection();
			result.SetInteger("offset", (uint) fixup.Offset);
			result.SetInteger("type index", (uint) fixup.Type);
			return result;
		}

		private StructureValueCollection SerializeInfos(int info)
		{
			var result = new StructureValueCollection();
			result.SetInteger("offset", (uint)info);
			return result;
		}

		private void SaveDefinitionFixups(IList<ResourceDefinitionFixup> fixups, StructureValueCollection values,
			IStream stream, ReflexiveCache<ResourceDefinitionFixup> cache)
		{
			var oldCount = (int) values.GetIntegerOrDefault("number of definition fixups", 0);
			uint oldAddress = values.GetIntegerOrDefault("definition fixup table address", 0);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("definition fixup entry");

			uint newAddress;
			if (!cache.TryGetAddress(fixups, out newAddress))
			{
				// Write a new reflexive
				IEnumerable<StructureValueCollection> entries = fixups.Select(f => SerializeDefinitionFixup(f));
				newAddress = ReflexiveWriter.WriteReflexive(entries, oldCount, oldAddress, fixups.Count, layout, _metaArea,
					_allocator, stream);
				cache.Add(newAddress, fixups);
			}
			else if (oldAddress != 0 && oldCount > 0)
			{
				// Reflexive was cached - just free it
				_allocator.Free(oldAddress, oldCount*layout.Size);
			}

			values.SetInteger("number of definition fixups", (uint) fixups.Count);
			values.SetInteger("definition fixup table address", newAddress);
		}

		private void FreeResources(StructureValueCollection values, IReader reader)
		{
			var count = (int) values.GetInteger("number of resources");
			uint address = values.GetInteger("resource table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("resource table entry");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
			foreach (StructureValueCollection entry in entries)
				FreeResource(entry);

			int size = count*layout.Size;
			if (address >= 0 && size > 0)
				_allocator.Free(address, size);
		}

		private void FreeResource(StructureValueCollection values)
		{
			FreeResourceFixups(values);
			FreeDefinitionFixups(values);
		}

		private void FreeResourceFixups(StructureValueCollection values)
		{
			var count = (int) values.GetInteger("number of resource fixups");
			uint address = values.GetInteger("resource fixup table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("resource fixup entry");
			int size = count*layout.Size;
			if (address >= 0 && size > 0)
				_allocator.Free(address, size);
		}

		private void FreeDefinitionFixups(StructureValueCollection values)
		{
			var count = (int) values.GetInteger("number of definition fixups");
			uint address = values.GetInteger("definition fixup table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("definition fixup entry");
			int size = count*layout.Size;
			if (address >= 0 && size > 0)
				_allocator.Free(address, size);
		}

		private int AlignInfoBlockOffset(Resource resource, int offset)
		{
			if ((resource.Flags & 4) != 0) // hax
				return (offset + 0xF) & ~0xF;
			return offset;
		}

		private int FindResourceType(string name)
		{
			if (string.IsNullOrEmpty(name))
				return -1;

			for (int i = 0; i < _resourceTypes.Length; i++)
			{
				if (_resourceTypes[i].Name == name)
					return i;
			}
			throw new InvalidOperationException("Invalid resource type \"" + name + "\"");
		}
	}
}