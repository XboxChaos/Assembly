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
		private IPointerExpander _expander;

		public ThirdGenResourceGestalt(IReader reader, ITag zoneTag, FileSegmentGroup metaArea, MetaAllocator allocator,
			StringIDSource stringIDs, EngineDescription buildInfo, IPointerExpander expander)
		{
			_tag = zoneTag;
			_metaArea = metaArea;
			_allocator = allocator;
			_buildInfo = buildInfo;
			_expander = expander;

			Load(reader, stringIDs);
		}

		public IEnumerable<Resource> LoadResources(IReader reader, TagTable tags, IList<ResourcePointer> pointers)
		{
			StructureValueCollection values = LoadTag(reader);
			byte[] infoBuffer = LoadResourceInfoBuffer(values, reader);

			StructureValueCollection[] entries = ReadTagBlock(values, reader, "number of resources", "resource table address", "resource table element");
			return entries.Select((e, i) => LoadResource(e, i, tags, pointers, infoBuffer, reader));
		}

		public IList<ResourcePointer> SaveResources(ICollection<Resource> resources, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("resource table element");

			FreeResources(values, stream);
			FreeInfoBuffer(values);

			// Serialize each resource element
			// This can't be lazily evaluated because allocations might cause the stream to expand
			int infoOffset = 0;
			int altInfoOffset = 0;
			var infocache = new TagBlockCache<int>();

			var pointers = new List<ResourcePointer>();
			var entries = new List<StructureValueCollection>();
			var fixupCache = new TagBlockCache<ResourceFixup>();
			var defFixupCache = new TagBlockCache<ResourceDefinitionFixup>();

			List<byte[]> paddedInfos = new List<byte[]>();

			foreach (Resource resource in resources)
			{

				StructureValueCollection entry = SerializeResource(resource, (resource.Location != null) ? pointers.Count : -1, stream);
				entries.Add(entry);

				// Save fixups
				SaveResourceFixups(resource.ResourceFixups, entry, stream, fixupCache);
				SaveDefinitionFixups(resource.DefinitionFixups, entry, stream, defFixupCache);

				uint bits = (uint)entry.GetInteger("resource bits");
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

				if (layout.HasField("number of resource info offsets"))
				{
					var oldCount = (int)entry.GetIntegerOrDefault("number of resource info offsets", 0);
					uint oldAddress = (uint)entry.GetIntegerOrDefault("resource info offsets table address", 0);

					long expand = _expander.Expand(oldAddress);

					StructureLayout infolayout = _buildInfo.Layouts.GetLayout("resource info offset element");

					if (size > 0)
					{
						long newBlockAddress;

						// Write a new block
						IEnumerable<StructureValueCollection> infoentries = offsets.Select(f => SerializeInfos(f));
						newBlockAddress = TagBlockWriter.WriteTagBlock(infoentries, oldCount, expand, offsets.Count, infolayout, _metaArea,
							_allocator, stream);
						infocache.Add(newBlockAddress, offsets);

						uint cont = _expander.Contract(newBlockAddress);

						entry.SetInteger("number of resource info offsets", (uint)offsets.Count);
						entry.SetInteger("resource info offsets table address", cont);
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

			// Write the block and update the tag values
			long newAddress = TagBlockWriter.WriteTagBlock(entries, layout, _metaArea, _allocator, stream);

			uint contr = _expander.Contract(newAddress);

			values.SetInteger("number of resources", (uint) entries.Count);
			values.SetInteger("resource table address", contr);

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
			uint address = (uint)values.GetInteger("resource type table address");

			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("resource type element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
			_resourceTypes = entries.Select(e => new ThirdGenResourceType(e, stringIDs)).ToArray();
		}

		private byte[] LoadResourceInfoBuffer(StructureValueCollection values, IReader reader)
		{
			var size = (int) values.GetInteger("resource info buffer size");
			uint address = (uint)values.GetInteger("resource info buffer address");

			long expand = _expander.Expand(address);

			if (size <= 0 || address == 0)
				return new byte[0];

			uint offset = _metaArea.PointerToOffset(expand);
			reader.SeekTo(offset);
			return reader.ReadBlock(size);
		}

		private void SaveResourceInfoBuffer(byte[] buffer, StructureValueCollection values, IStream stream)
		{
			long newAddress = 0;
			if (buffer.Length > 0)
			{
				newAddress = _allocator.Allocate((uint)buffer.Length, 0x10, stream);
				stream.SeekTo(_metaArea.PointerToOffset(newAddress));
				stream.WriteBlock(buffer);
			}

			// Update values

			uint cont = _expander.Contract(newAddress);

			values.SetInteger("resource info buffer size", (uint) buffer.Length);
			values.SetInteger("resource info buffer address", cont);
		}

		public IEnumerable<ResourcePredictionD> LoadPredictions(IReader reader, TagTable tags, List<Resource> resources)
		{
			StructureValueCollection values = LoadTag(reader);

			if (!values.HasInteger("number of prediction d2s") || !values.HasInteger("prediction d2 table address"))
				return null;

			int subcount = 2;
			StructureLayout templayout = _buildInfo.Layouts.GetLayout("raw segment table element");
			if (templayout.HasField("tertiary page index"))
				subcount = 3;

			var result = new List<ResourcePredictionD>();

			StructureValueCollection[] d2entries = ReadTagBlock(values, reader, "number of prediction d2s", "prediction d2 table address", "prediction d2 element");
			StructureValueCollection[] dentries = ReadTagBlock(values, reader, "number of prediction ds", "prediction d table address", "prediction d element");
			StructureValueCollection[] centries = ReadTagBlock(values, reader, "number of prediction cs", "prediction c table address", "prediction c element");
			StructureValueCollection[] bentries = ReadTagBlock(values, reader, "number of prediction bs", "prediction b table address", "prediction b element");
			StructureValueCollection[] aentries = ReadTagBlock(values, reader, "number of prediction as", "prediction a table address", "prediction a element");

			for (int i = 0; i < d2entries.Length; i++)
			{
				ResourcePredictionD pd = new ResourcePredictionD();
				pd.Index = i;
				var tag = new DatumIndex(d2entries[i].GetInteger("tag datum"));
				pd.Tag = tag.IsValid ? tags[tag] : null;
				pd.Unknown1 = (int)d2entries[i].GetInteger("unknown 1");
				pd.Unknown2 = (int)d2entries[i].GetInteger("unknown 2");

				var dccount = (int)dentries[i].GetInteger("c count");
				var dcindex = (int)dentries[i].GetInteger("c index");

				var dacount = (int)dentries[i].GetInteger("a count");
				var daindex = (int)dentries[i].GetInteger("a index");

				for (int c = dcindex; c < dcindex + dccount; c++)
				{
					ResourcePredictionC pc = new ResourcePredictionC();
					pc.Index = c;
					var cbindex = (int)centries[c].GetInteger("b index");
					pc.OverallIndex = (short)centries[c].GetInteger("overall index");

					ResourcePredictionB pb = new ResourcePredictionB();
					pb.Index = cbindex;
					var bacount = (int)bentries[cbindex].GetInteger("a count");
					var baindex = (int)bentries[cbindex].GetInteger("a index");
					pb.OverallIndex = (short)bentries[cbindex].GetInteger("overall index");

					for (int a = baindex; a < baindex + bacount; a++)
					{
						ResourcePredictionA pa = new ResourcePredictionA();
						pa.Index = a;
						pa.Value = new DatumIndex(aentries[a].GetInteger("value"));

						int resolvedresource = pa.Value.Index / subcount;
						int subresource = pa.Value.Index - resolvedresource * subcount;

						if (resolvedresource >= resources.Count)
							continue;
						var res = resources[resolvedresource];

						pa.Resource = res.Index;
						pa.SubResource = subresource;

						pb.AEntries.Add(pa);
					}

					pc.BEntry = pb;
					pd.CEntries.Add(pc);
				}

				for (int a = daindex; a < daindex + dacount; a++)
				{
					ResourcePredictionA pa = new ResourcePredictionA();
					pa.Index = a;
					pa.Value = new DatumIndex(aentries[a].GetInteger("value"));

					int resolvedresource = pa.Value.Index / subcount;
					int subresource = pa.Value.Index - resolvedresource * subcount;

					if (resolvedresource >= resources.Count)
						continue;
					var res = resources[resolvedresource];

					pa.Resource = res.Index;
					pa.SubResource = subresource;

					pd.AEntries.Add(pa);
				}
				result.Add(pd);
			}
			return result;
		}

		private StructureValueCollection SerializePredictionD(ResourcePredictionD prediction, int cStart, int aStart, IStream stream)
		{
			var result = new StructureValueCollection();
			result.SetInteger("tag datum", prediction.Tag.Index.Value);
			result.SetInteger("unknown 1", (uint)prediction.Unknown1);
			result.SetInteger("unknown 2", (uint)prediction.Unknown2);

			result.SetInteger("c index", (uint)cStart);
			result.SetInteger("c count", (uint)prediction.CEntries.Count);

			result.SetInteger("a index", (uint)aStart);
			result.SetInteger("a count", (uint)prediction.AEntries.Count);

			return result;
		}

		private StructureValueCollection SerializePredictionC(ResourcePredictionC prediction, int bStart, int overall, IStream stream)
		{
			var result = new StructureValueCollection();
			if (prediction.OverallIndex == -1)
				result.SetInteger("overall index", (uint)overall);
			else
				result.SetInteger("overall index", (uint)prediction.OverallIndex);

			result.SetInteger("b index", (uint)bStart);

			return result;
		}

		private StructureValueCollection SerializePredictionB(ResourcePredictionB prediction, int aStart, int overall, IStream stream)
		{
			var result = new StructureValueCollection();
			if (prediction.OverallIndex == -1)
				result.SetInteger("overall index", (uint)overall);
			else
				result.SetInteger("overall index", (uint)prediction.OverallIndex);

			result.SetInteger("a index", (uint)aStart);
			result.SetInteger("a count", (uint)prediction.AEntries.Count);

			return result;
		}

		private StructureValueCollection SerializePredictionA(ResourcePredictionA prediction, IStream stream)
		{
			var result = new StructureValueCollection();
			result.SetInteger("value", prediction.Value.Value);

			return result;
		}

		public void SavePredictions(ICollection<ResourcePredictionD> predictions, IStream stream)
		{
			StructureValueCollection values = LoadTag(stream);

			FreePredictions(values);

			var dentries = new List<StructureValueCollection>();
			var centries = new List<StructureValueCollection>();
			var bentries = new List<StructureValueCollection>();
			var aentries = new List<StructureValueCollection>();

			var writtenc = new Dictionary<long, int>();//hash, index
			var writtenb = new Dictionary<long, int>();

			int firstnullc = -1;

			DatumIndex currenttag = DatumIndex.Null;

			foreach (ResourcePredictionD pred in predictions)
			{
				long dchash = pred.GetCHash();

				int dcstart = centries.Count();
				int dastart = aentries.Count;

				if (pred.CEntries.Count > 0)
				{
					int exist;
					bool found = writtenc.TryGetValue(dchash, out exist);

					if (!found)
					{
						foreach (ResourcePredictionC pc in pred.CEntries)
						{
							long cbhash = pc.GetBHash();

							int cbstart = bentries.Count;

							int bexist;
							bool bfound = writtenb.TryGetValue(cbhash, out bexist);

							if (!bfound)
							{
								int bkstart = aentries.Count();

								foreach (ResourcePredictionA pa in pc.BEntry.AEntries)
									aentries.Add(SerializePredictionA(pa, stream));

								writtenb[cbhash] = cbstart;

								bentries.Add(SerializePredictionB(pc.BEntry, bkstart, cbstart, stream));
							}
							else
								cbstart = bexist;

							writtenc[dchash] = dcstart;

							centries.Add(SerializePredictionC(pc, cbstart, cbstart, stream));
						}
					}
					else
						dcstart = exist;
				}
				else
				{
					if (firstnullc == -1)
						firstnullc = dcstart;
					else
						dcstart = firstnullc;	
				}

				if (pred.AEntries.Count > 0)
				{
					foreach (ResourcePredictionA pa in pred.AEntries)
						aentries.Add(SerializePredictionA(pa, stream));
				}
				else
					dastart = -1;

				dentries.Add(SerializePredictionD(pred, dcstart, dastart, stream));
			}

			// a
			StructureLayout alayout = _buildInfo.Layouts.GetLayout("prediction a element");
			long newa = TagBlockWriter.WriteTagBlock(aentries, alayout, _metaArea, _allocator, stream);

			uint conta = _expander.Contract(newa);

			values.SetInteger("number of prediction as", (uint)aentries.Count);
			values.SetInteger("prediction a table address", conta);

			// b
			StructureLayout blayout = _buildInfo.Layouts.GetLayout("prediction b element");
			long newb = TagBlockWriter.WriteTagBlock(bentries, blayout, _metaArea, _allocator, stream);

			uint contb = _expander.Contract(newb);

			values.SetInteger("number of prediction bs", (uint)bentries.Count);
			values.SetInteger("prediction b table address", contb);

			// cc
			StructureLayout clayout = _buildInfo.Layouts.GetLayout("prediction c element");
			long newc = TagBlockWriter.WriteTagBlock(centries, clayout, _metaArea, _allocator, stream);

			uint contc = _expander.Contract(newc);

			values.SetInteger("number of prediction cs", (uint)centries.Count);
			values.SetInteger("prediction c table address", contc);

			// d
			StructureLayout dlayout = _buildInfo.Layouts.GetLayout("prediction d element");
			long newd = TagBlockWriter.WriteTagBlock(dentries, dlayout, _metaArea, _allocator, stream);

			uint contd = _expander.Contract(newd);

			values.SetInteger("number of prediction ds", (uint)dentries.Count);
			values.SetInteger("prediction d table address", contd);

			// d2
			StructureLayout d2layout = _buildInfo.Layouts.GetLayout("prediction d2 element");
			long newd2 = TagBlockWriter.WriteTagBlock(dentries, d2layout, _metaArea, _allocator, stream);

			uint contd2 = _expander.Contract(newd2);

			values.SetInteger("number of prediction d2s", (uint)dentries.Count);
			values.SetInteger("prediction d2 table address", contd2);

			SaveTag(values, stream);
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
			result.Flags = (uint)values.GetInteger("flags");

			var infoSize = (int) values.GetInteger("resource info size");

			if (infoSize > 0)
			{
				var infoOffset = 0;

				if (values.HasInteger("number of resource info offsets"))//for h4
				{
					var infocount = (int)values.GetInteger("number of resource info offsets");
					uint address = (uint)values.GetInteger("resource info offsets table address");

					long expand = _expander.Expand(address);

					StructureLayout layout = _buildInfo.Layouts.GetLayout("resource info offset element");
					StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, infocount, expand, layout, _metaArea);

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
				result.SetInteger("parent tag group magic", (uint) resource.ParentTag.Group.Magic);
				result.SetInteger("parent tag datum index", resource.ParentTag.Index.Value);
			}
			else
			{
				result.SetInteger("parent tag group magic", 0xFFFFFFFF);
				result.SetInteger("parent tag datum index", 0xFFFFFFFF);
			}
			result.SetInteger("datum index salt", resource.Index.Salt);
			result.SetInteger("resource type index", (uint) FindResourceType(resource.Type));
			result.SetInteger("flags", resource.Flags);
			result.SetInteger("resource info size", (resource.Info != null) ? (uint)resource.Info.Length : 0);

			result.SetInteger("resource bits", (ushort) resource.ResourceBits);
			result.SetInteger("segment index", (uint) pointerIndex);
			result.SetInteger("base definition address", (uint) resource.BaseDefinitionAddress);
			return result;
		}

		private IEnumerable<ResourceFixup> LoadResourceFixups(StructureValueCollection values, IReader reader)
		{
			var count = (int) values.GetInteger("number of resource fixups");
			uint address = (uint)values.GetInteger("resource fixup table address");

			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("resource fixup element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
			return entries.Select(e => new ResourceFixup
			{
				Offset = (int) e.GetInteger("offset"),
				Address = (uint)e.GetInteger("address")
			});
		}

		private void SaveResourceFixups(IList<ResourceFixup> fixups, StructureValueCollection values, IStream stream,
			TagBlockCache<ResourceFixup> cache)
		{
			var oldCount = (int) values.GetIntegerOrDefault("number of resource fixups", 0);
			uint oldAddress = (uint)values.GetIntegerOrDefault("resource fixup table address", 0);

			long oldExpand = _expander.Expand(oldAddress);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("resource fixup element");

			long newAddress;
			if (!cache.TryGetAddress(fixups, out newAddress))
			{
				// Write a new block
				IEnumerable<StructureValueCollection> entries = fixups.Select(f => SerializeResourceFixup(f));
				newAddress = TagBlockWriter.WriteTagBlock(entries, oldCount, oldExpand, fixups.Count, layout, _metaArea,
					_allocator, stream);
				cache.Add(newAddress, fixups);
			}
			else if (oldAddress != 0 && oldCount > 0)
			{
				// Block was cached - just free it
				_allocator.Free(oldExpand, (uint)(oldCount*layout.Size));
			}

			uint cont = _expander.Contract(newAddress);

			values.SetInteger("number of resource fixups", (uint) fixups.Count);
			values.SetInteger("resource fixup table address", cont);
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
			uint address = (uint)values.GetInteger("definition fixup table address");

			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("definition fixup element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
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
			IStream stream, TagBlockCache<ResourceDefinitionFixup> cache)
		{
			var oldCount = (int) values.GetIntegerOrDefault("number of definition fixups", 0);
			uint oldAddress = (uint)values.GetIntegerOrDefault("definition fixup table address", 0);

			long oldExpand = _expander.Expand(oldAddress);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("definition fixup element");

			long newAddress;
			if (!cache.TryGetAddress(fixups, out newAddress))
			{
				// Write a new block
				IEnumerable<StructureValueCollection> entries = fixups.Select(f => SerializeDefinitionFixup(f));
				newAddress = TagBlockWriter.WriteTagBlock(entries, oldCount, oldExpand, fixups.Count, layout, _metaArea,
					_allocator, stream);
				cache.Add(newAddress, fixups);
			}
			else if (oldAddress != 0 && oldCount > 0)
			{
				// Block was cached - just free it
				_allocator.Free(oldExpand, (uint)(oldCount*layout.Size));
			}

			uint cont = _expander.Contract(newAddress);

			values.SetInteger("number of definition fixups", (uint) fixups.Count);
			values.SetInteger("definition fixup table address", cont);
		}

		private void FreeResources(StructureValueCollection values, IReader reader)
		{
			var count = (int) values.GetInteger("number of resources");
			uint address = (uint)values.GetInteger("resource table address");

			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("resource table element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
			foreach (StructureValueCollection entry in entries)
				FreeResource(entry);

			uint size = (uint)(count*layout.Size);
			if (expand >= 0 && size > 0)
				_allocator.Free(expand, size);
		}

		private void FreeResource(StructureValueCollection values)
		{
			FreeTagBlock(values, "number of resource fixups", "resource fixup table address", "resource fixup element");
			FreeTagBlock(values, "number of definition fixups", "definition fixup table address", "definition fixup element");
			if (values.HasInteger("number of resource info offsets"))
				FreeTagBlock(values, "number of resource info offsets", "resource info offsets table address", "resource info offset element");
		}

		private void FreeInfoBuffer(StructureValueCollection values)
		{
			var buffsize = (uint)values.GetInteger("resource info buffer size");
			uint buffaddr = (uint)values.GetInteger("resource info buffer address");

			long expand = _expander.Expand(buffaddr);

			if (buffaddr >= 0 && buffsize > 0)
				_allocator.Free(expand, buffsize);
		}

		private void FreePredictions(StructureValueCollection values)
		{
			FreeTagBlock(values, "number of prediction as", "prediction a table address", "prediction a element");
			FreeTagBlock(values, "number of prediction bs", "prediction b table address", "prediction b element");
			FreeTagBlock(values, "number of prediction cs", "prediction c table address", "prediction c element");
			FreeTagBlock(values, "number of prediction ds", "prediction d table address", "prediction d element");
			FreeTagBlock(values, "number of prediction d2s", "prediction d2 table address", "prediction d2 element");
		}

		private void FreeTagBlock(StructureValueCollection values, string countName, string addressName, string layoutName)
		{
			var count = (int)values.GetInteger(countName);
			uint address = (uint)values.GetInteger(addressName);

			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout(layoutName);
			uint size = (uint)(count * layout.Size);
			if (expand >= 0 && size > 0)
				_allocator.Free(expand, size);
		}

		private StructureValueCollection[] ReadTagBlock(StructureValueCollection values, IReader reader, string countName, string addressName, string layoutName)
		{
			var count = (int)values.GetInteger(countName);
			uint address = (uint)values.GetInteger(addressName);

			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout(layoutName);
			return TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
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