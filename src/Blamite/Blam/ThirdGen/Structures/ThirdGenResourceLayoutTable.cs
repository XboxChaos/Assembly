using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Resources;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Structures
{
	public class ThirdGenResourceLayoutTable
	{
		private readonly MetaAllocator _allocator;
		private readonly EngineDescription _buildInfo;
		private readonly FileSegmentGroup _metaArea;
		private readonly ITag _tag;
		private readonly bool _zone;
		private readonly IPointerExpander _expander;

		public ThirdGenResourceLayoutTable(ITag playTag, FileSegmentGroup metaArea, MetaAllocator allocator,
			EngineDescription buildInfo, IPointerExpander expander)
		{
			_tag = playTag;
			_metaArea = metaArea;
			_allocator = allocator;
			_buildInfo = buildInfo;
			_expander = expander;

			_zone = _tag.Group.Magic != 1886151033;//"play"
		}

		public IEnumerable<ResourcePage> LoadPages(IReader reader)
		{
			StructureValueCollection values = LoadTag(reader);
			ThirdGenCacheFileReference[] files = LoadExternalFiles(values, reader);

			var count = (int) values.GetInteger("number of raw pages");
			uint address = (uint)values.GetInteger("raw page table address");

			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("raw page table element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
			return entries.Select((e, i) => LoadPage(e, i, files));
		}

		public void SavePages(ICollection<ResourcePage> pages, ICollection<ResourcePointer> pointers, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);
			ThirdGenCacheFileReference[] files = LoadExternalFiles(values, stream);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("raw page table element");
			var oldCount = (int) values.GetInteger("number of raw pages");
			uint oldAddress = (uint)values.GetInteger("raw page table address");

			long expand = _expander.Expand(oldAddress);

			// recount asset count values for every page for predictions
			foreach (ResourcePage p in pages)
				p.AssetCount = pointers.Count(x => x.PrimaryPage == p || x.SecondaryPage == p || x.TertiaryPage == p);

			IEnumerable<StructureValueCollection> entries = pages.Select(p => SerializePage(p, files));
			long newAddress = TagBlockWriter.WriteTagBlock(entries, oldCount, expand, pages.Count, layout, _metaArea,
				_allocator, stream);

			// Update values
			uint cont = _expander.Contract(newAddress);

			values.SetInteger("number of raw pages", (uint) pages.Count);
			values.SetInteger("raw page table address", cont);
			SaveTag(values, stream);
		}

		public IEnumerable<ResourcePointer> LoadPointers(IReader reader, IList<ResourcePage> pages, IList<ResourceSize> sizes)
		{
			StructureValueCollection values = LoadTag(reader);
			var count = (int) values.GetInteger("number of raw segments");
			uint address = (uint)values.GetInteger("raw segment table address");

			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("raw segment table element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
			return entries.Select(e => LoadPointer(e, pages, sizes));
		}

		public void SavePointers(ICollection<ResourcePointer> pointers, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("raw segment table element");
			var oldCount = (int) values.GetInteger("number of raw segments");
			uint oldAddress = (uint)values.GetInteger("raw segment table address");

			long expand = _expander.Expand(oldAddress);

			IEnumerable<StructureValueCollection> entries = pointers.Select(p => SerializePointer(p));
			long newAddress = TagBlockWriter.WriteTagBlock(entries, oldCount, expand, pointers.Count, layout, _metaArea,
				_allocator, stream);

			// Update values
			uint cont = _expander.Contract(newAddress);

			values.SetInteger("number of raw segments", (uint) pointers.Count);
			values.SetInteger("raw segment table address", cont);
			SaveTag(values, stream);
		}

		public List<ResourceSize> LoadSizes(IReader reader)
		{
			StructureValueCollection values = LoadTag(reader);
			var count = (int)values.GetInteger("number of raw sizes");
			uint address = (uint)values.GetInteger("raw size table address");

			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("raw size table element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
			return entries.Select((e, i) => LoadSize(e, i, reader)).ToList();
		}

		public void SaveSizes(ICollection<ResourceSize> sizes, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);

			var entries = new List<StructureValueCollection>();
			var partCache = new TagBlockCache<ResourceSizePart>();

			foreach (ResourceSize size in sizes)
			{
				StructureValueCollection entry = SerializeSize(size);
				entries.Add(entry);
				SaveSizeParts(size.Parts, entry, stream, partCache);
			}


			StructureLayout layout = _buildInfo.Layouts.GetLayout("raw size table element");
			var oldCount = (int)values.GetInteger("number of raw sizes");
			uint oldAddress = (uint)values.GetInteger("raw size table address");

			long expand = _expander.Expand(oldAddress);

			long newAddress = TagBlockWriter.WriteTagBlock(entries, oldCount, expand, sizes.Count, layout, _metaArea,
				_allocator, stream);

			// Update values
			uint cont = _expander.Contract(newAddress);

			values.SetInteger("number of raw sizes", (uint)sizes.Count);
			values.SetInteger("raw size table address", cont);
			SaveTag(values, stream);
		}

		private StructureValueCollection LoadTag(IReader reader)
		{
			reader.SeekTo(_tag.MetaLocation.AsOffset());
			if (!_zone)
				return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("resource layout table"));
			else
				return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("resource layout table alt"));
		}

		private void SaveTag(StructureValueCollection values, IWriter writer)
		{
			writer.SeekTo(_tag.MetaLocation.AsOffset());
			if (!_zone)
				StructureWriter.WriteStructure(values, _buildInfo.Layouts.GetLayout("resource layout table"), writer);
			else
				StructureWriter.WriteStructure(values, _buildInfo.Layouts.GetLayout("resource layout table alt"), writer);
		}

		private ThirdGenCacheFileReference[] LoadExternalFiles(StructureValueCollection values, IReader reader)
		{
			var count = (int) values.GetInteger("number of external cache files");
			uint address = (uint)values.GetInteger("external cache file table address");

			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("external cache file table element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
			return entries.Select(e => new ThirdGenCacheFileReference(e)).ToArray();
		}

		private ResourcePage LoadPage(StructureValueCollection values, int index, ThirdGenCacheFileReference[] externalFiles)
		{
			var result = new ResourcePage();
			result.Index = index;
			result.Salt = (ushort) values.GetInteger("salt");
			result.Flags = (byte) values.GetInteger("flags");
			result.CompressionMethod = ((int) values.GetInteger("compression codec index") != -1)
				? ResourcePageCompression.Deflate
				: ResourcePageCompression.None; // FIXME: hax/laziness
			var externalFile = (int) values.GetInteger("shared cache file index");
			result.FilePath = (externalFile != -1) ? externalFiles[externalFile].Path : null;
			result.Unknown1 = (int) values.GetIntegerOrDefault("unknown 1", 0);
			result.Offset = (int) values.GetInteger("compressed block offset");
			result.CompressedSize = (int) values.GetInteger("compressed block size");
			result.UncompressedSize = (int) values.GetInteger("uncompressed block size");
			result.Checksum = (uint)values.GetInteger("checksum");
			result.Hash1 = values.GetRaw("hash 1");
			result.Hash2 = values.GetRaw("hash 2");
			result.Hash3 = values.GetRaw("hash 3");
			result.AssetCount = (int) values.GetIntegerOrDefault("asset count", 0);
			result.Unknown2 = (int) values.GetIntegerOrDefault("unknown 2", 0);
			return result;
		}

		private StructureValueCollection SerializePage(ResourcePage page, ThirdGenCacheFileReference[] externalFiles)
		{
			var result = new StructureValueCollection();
			result.SetInteger("salt", page.Salt);
			result.SetInteger("flags", page.Flags);
			result.SetInteger("compression codec index",
				(page.CompressionMethod != ResourcePageCompression.None) ? 0 : 0xFFFFFFFF);
			result.SetInteger("shared cache file index",
				(page.FilePath != null) ? (uint) FindExternalFile(externalFiles, page.FilePath) : 0xFFFFFFFF);
			result.SetInteger("unknown 1", (uint) page.Unknown1);
			result.SetInteger("compressed block offset", (uint) page.Offset);
			result.SetInteger("compressed block size", (uint) page.CompressedSize);
			result.SetInteger("uncompressed block size", (uint) page.UncompressedSize);
			result.SetInteger("checksum", page.Checksum);
			result.SetRaw("hash 1", page.Hash1);
			result.SetRaw("hash 2", page.Hash2);
			result.SetRaw("hash 3", page.Hash3);
			result.SetInteger("asset count", (uint) page.AssetCount);
			result.SetInteger("unknown 2", (uint) page.Unknown2);
			return result;
		}

		private int FindExternalFile(ThirdGenCacheFileReference[] externalFiles, string path)
		{
			for (int i = 0; i < externalFiles.Length; i++)
			{
				if (externalFiles[i].Path.ToLowerInvariant() == path.ToLowerInvariant())
					return i;
			}
			throw new InvalidOperationException("Invalid shared map path \"" + path + "\"");
		}

		private ResourcePointer LoadPointer(StructureValueCollection values, IList<ResourcePage> pages, IList<ResourceSize> sizes)
		{
			var result = new ResourcePointer();
			var primaryPage = (int) values.GetInteger("primary page index");
			result.PrimaryPage = (primaryPage != -1) ? pages[primaryPage] : null;
			result.PrimaryOffset = (int) values.GetInteger("primary offset");
			var primarySize = (int) values.GetInteger("primary size index");
			result.PrimarySize = (primarySize != -1) ? sizes[primarySize] : null;

			var secondaryPage = (int) values.GetInteger("secondary page index");
			result.SecondaryPage = (secondaryPage != -1) ? pages[secondaryPage] : null;
			result.SecondaryOffset = (int) values.GetInteger("secondary offset");
			var secondarySize = (int) values.GetInteger("secondary size index");
			result.SecondarySize = (secondarySize != -1) ? sizes[secondarySize] : null;


			if (values.HasInteger("tertiary page index"))
			{
				var tertiaryPage = (int)values.GetInteger("tertiary page index");
				result.TertiaryPage = (tertiaryPage != -1) ? pages[tertiaryPage] : null;
				result.TertiaryOffset = (int)values.GetInteger("tertiary offset");
				var tertiarySize = (int) values.GetInteger("tertiary size index");
				result.TertiarySize = (tertiarySize != -1) ? sizes[tertiarySize] : null;
			}


			return result;
		}

		private StructureValueCollection SerializePointer(ResourcePointer pointer)
		{
			var result = new StructureValueCollection();
			result.SetInteger("primary page index", (pointer.PrimaryPage != null) ? (uint) pointer.PrimaryPage.Index : 0xFFFFFFFF);
			result.SetInteger("primary offset", (uint) pointer.PrimaryOffset);
			result.SetInteger("primary size index", (pointer.PrimarySize != null) ? (uint)pointer.PrimarySize.Index : 0xFFFFFFFF);
			result.SetInteger("secondary page index",
				(pointer.SecondaryPage != null) ? (uint) pointer.SecondaryPage.Index : 0xFFFFFFFF);
			result.SetInteger("secondary offset", (uint) pointer.SecondaryOffset);
			result.SetInteger("secondary size index", (pointer.SecondarySize != null) ? (uint)pointer.SecondarySize.Index : 0xFFFFFFFF);

			if (_buildInfo.HeaderSize == 0x1E000)
			{
				result.SetInteger("tertiary page index", (pointer.TertiaryPage != null) ? (uint)pointer.TertiaryPage.Index : 0xFFFFFFFF);
				result.SetInteger("tertiary offset", (uint)pointer.TertiaryOffset);
				result.SetInteger("tertiary size index", (pointer.TertiarySize != null) ? (uint)pointer.TertiarySize.Index : 0xFFFFFFFF);
			}

			return result;
		}

		private ResourceSize LoadSize(StructureValueCollection values, int index, IReader reader)
		{
			var result = new ResourceSize();

			result.Index = index;

			result.Size = (int)values.GetInteger("overall size");

			result.Parts.AddRange(LoadSizeParts(values, reader));

			return result;
		}

		private StructureValueCollection SerializeSize(ResourceSize size)
		{
			var result = new StructureValueCollection();

			result.SetInteger("overall size", (uint)size.Size);

			return result;
		}

		private IEnumerable<ResourceSizePart> LoadSizeParts(StructureValueCollection values, IReader reader)
		{
			var count = (int)values.GetInteger("number of size parts");
			uint address = (uint)values.GetInteger("size part table address");

			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("size part table element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
			return entries.Select(e => new ResourceSizePart
			{
				Offset = (int)e.GetInteger("offset"),
				Size = (int)e.GetInteger("size")
			});
		}

		private void SaveSizeParts(IList<ResourceSizePart> parts, StructureValueCollection values, IStream stream,
			TagBlockCache<ResourceSizePart> cache)
		{
			var oldCount = (int)values.GetIntegerOrDefault("number of size parts", 0);
			uint oldAddress = (uint)values.GetIntegerOrDefault("size part table address", 0);

			long expand = _expander.Expand(oldAddress);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("size part table element");

			long newAddress;
			if (!cache.TryGetAddress(parts, out newAddress))
			{
				// Write a new block
				IEnumerable<StructureValueCollection> entries = parts.Select(p => SerializeSizePart(p));
				newAddress = TagBlockWriter.WriteTagBlock(entries, oldCount, expand, parts.Count, layout, _metaArea,
					_allocator, stream);
				cache.Add(newAddress, parts);
			}
			else if (oldAddress != 0 && oldCount > 0)
			{
				// Block was cached - just free it
				_allocator.Free(expand, (uint)(oldCount * layout.Size));
			}

			uint cont = _expander.Contract(newAddress);

			values.SetInteger("number of size parts", (uint)parts.Count);
			values.SetInteger("size part table address", cont);
		}

		private StructureValueCollection SerializeSizePart(ResourceSizePart part)
		{
			var result = new StructureValueCollection();
			result.SetInteger("offset", (uint)part.Offset);
			result.SetInteger("size", (uint)part.Size);
			return result;
		}
	}
}
