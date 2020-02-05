using System;
using System.Collections;
using Blamite.Blam.Resources;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Extensions;

namespace Blamite.Blam.ThirdGen.Structures
{
	/// <summary>
	///     Implementation of <see cref="IZoneSet" /> for third-generation cache files.
	/// </summary>
	public class ThirdGenZoneSet : IZoneSet
	{
		private readonly FileSegmentGroup _metaArea;
		private BitArray _activeResources;
		private BitArray _activeTags;
		private BitArray _unknownResources;
		private BitArray _unknownResources2;
		private BitArray _unknownTags;

		public ThirdGenZoneSet(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, IPointerExpander expander)
		{
			_metaArea = metaArea;
			Load(values, reader, expander);
		}

		/// <summary>
		///     Gets the stringID pointing to the zone set's name.
		/// </summary>
		public StringID Name { get; private set; }

		/// <summary>
		///     Activates or deactivates a resource in the zone set.
		/// </summary>
		/// <param name="index">The datum index of the resource to activate or deactivate.</param>
		/// <param name="activate"><c>true</c> if the resource should be made active, <c>false</c> otherwise.</param>
		public void ActivateResource(DatumIndex index, bool activate)
		{
			_activeResources.Length = Math.Max(_activeResources.Length, index.Index + 1);
			_activeResources[index.Index] = activate;
		}

		/// <summary>
		///     Activates or deactivates a resource in the zone set.
		/// </summary>
		/// <param name="resource">The resource to activate or deactivate.</param>
		/// <param name="activate"><c>true</c> if the resource should be made active, <c>false</c> otherwise.</param>
		public void ActivateResource(Resource resource, bool activate)
		{
			if (resource != null)
				ActivateResource(resource.Index, activate);
		}

		/// <summary>
		///     Determines whether or not a resource is marked as active.
		/// </summary>
		/// <param name="index">The datum index of the resource to check.</param>
		/// <returns><c>true</c> if the resource is active, <c>false</c> otherwise.</returns>
		public bool IsResourceActive(DatumIndex index)
		{
			if (index.Index >= _activeResources.Length)
				return false;
			return _activeResources[index.Index];
		}

		/// <summary>
		///     Determines whether or not a resource is marked as active.
		/// </summary>
		/// <param name="index">The resource to check.</param>
		/// <returns><c>true</c> if the resource is active, <c>false</c> otherwise.</returns>
		public bool IsResourceActive(Resource resource)
		{
			if (resource == null)
				return false;
			return IsResourceActive(resource.Index);
		}

		/// <summary>
		///     Activates or deactivates a tag in the zone set.
		/// </summary>
		/// <param name="index">The datum index of the tag to activate or deactivate.</param>
		/// <param name="activate"><c>true</c> if the tag should be made active, <c>false</c> otherwise.</param>
		public void ActivateTag(DatumIndex index, bool activate)
		{
			_activeTags.Length = Math.Max(_activeTags.Length, index.Index + 1);
			_activeTags[index.Index] = activate;
		}

		/// <summary>
		///     Activates or deactivates a tag in the zone set.
		/// </summary>
		/// <param name="tag">The tag to activate or deactivate.</param>
		/// <param name="activate"><c>true</c> if the tag should be made active, <c>false</c> otherwise.</param>
		public void ActivateTag(ITag tag, bool activate)
		{
			if (tag != null)
				ActivateTag(tag.Index, activate);
		}

		/// <summary>
		///     Determines whether or not a tag is marked as active.
		/// </summary>
		/// <param name="index">The datum index of the tag to check.</param>
		/// <returns><c>true</c> if the tag is active, <c>false</c> otherwise.</returns>
		public bool IsTagActive(DatumIndex index)
		{
			if (index.Index >= _activeTags.Length)
				return false;
			return _activeTags[index.Index];
		}

		/// <summary>
		///     Determines whether or not a tag is marked as active.
		/// </summary>
		/// <param name="tag">The tag to check.</param>
		/// <returns><c>true</c> if the tag is active, <c>false</c> otherwise.</returns>
		public bool IsTagActive(ITag tag)
		{
			if (tag == null)
				return false;
			return IsTagActive(tag.Index);
		}

		public StructureValueCollection Serialize(IStream stream, MetaAllocator allocator, ReflexiveCache<int> cache, IPointerExpander expander)
		{
			var result = new StructureValueCollection();
			SaveBitArray(_activeResources, "number of raw pool bitfields", "raw pool bitfield table address", allocator, stream, cache, result, expander);
			SaveBitArray(_unknownResources, "number of raw pool 2 bitfields", "raw pool 2 bitfield table address", allocator, stream, cache, result, expander);
			SaveBitArray(_unknownResources2, "number of raw pool 3 bitfields", "raw pool 3 bitfield table address", allocator, stream, cache, result, expander);
			SaveBitArray(_activeTags, "number of tag bitfields", "tag bitfield table address", allocator, stream, cache, result, expander);
			SaveBitArray(_unknownTags, "number of tag 2 bitfields", "tag 2 bitfield table address", allocator, stream, cache, result, expander);
			return result;
		}

		public static void Free(StructureValueCollection values, MetaAllocator allocator, IPointerExpander expander)
		{
			FreeBitArray(values, "number of raw pool bitfields", "raw pool bitfield table address", allocator, expander);
			FreeBitArray(values, "number of raw pool 2 bitfields", "raw pool 2 bitfield table address", allocator, expander);
			FreeBitArray(values, "number of raw pool 3 bitfields", "raw pool 3 bitfield table address", allocator, expander);
			FreeBitArray(values, "number of tag bitfields", "tag bitfield table address", allocator, expander);
			FreeBitArray(values, "number of tag 2 bitfields", "tag 2 bitfield table address", allocator, expander);
		}

		private void Load(StructureValueCollection values, IReader reader, IPointerExpander expander)
		{
			Name = new StringID(values.GetInteger("name stringid"));
			_activeResources = LoadBitArray(values, "number of raw pool bitfields", "raw pool bitfield table address", reader, expander);
			_unknownResources = LoadBitArray(values, "number of raw pool 2 bitfields", "raw pool 2 bitfield table address", reader, expander);
			_unknownResources2 = LoadBitArray(values, "number of raw pool 3 bitfields", "raw pool 3 bitfield table address", reader, expander);
			_activeTags = LoadBitArray(values, "number of tag bitfields", "tag bitfield table address", reader, expander);
			_unknownTags = LoadBitArray(values, "number of tag 2 bitfields", "tag 2 bitfield table address", reader, expander);
		}

		private BitArray LoadBitArray(StructureValueCollection values, string countName, string addressName, IReader reader, IPointerExpander expander)
		{
			if (!values.HasInteger(countName) || !values.HasInteger(addressName))
				return new BitArray(0);

			var count = (int) values.GetInteger(countName);
			uint address = (uint)values.GetInteger(addressName);
			if (count <= 0 || address == 0)
				return new BitArray(0);

			long expand = expander.Expand(address);

			var ints = new int[count];
			reader.SeekTo(_metaArea.PointerToOffset(expand));
			for (int i = 0; i < count; i++)
				ints[i] = reader.ReadInt32();

			return new BitArray(ints);
		}

		private void SaveBitArray(BitArray bits, string countName, string addressName, MetaAllocator allocator, IStream stream, ReflexiveCache<int> cache, StructureValueCollection values, IPointerExpander expander)
		{
			if (bits.Length == 0)
			{
				values.SetInteger(countName, 0);
				values.SetInteger(addressName, 0);
				return;
			}

			var ints = bits.ToIntArray();

			// If the address isn't cached, then allocate space and write a new array
			long newAddress;
			if (!cache.TryGetAddress(ints, out newAddress))
			{
				newAddress = allocator.Allocate(ints.Length*4, stream);
				stream.SeekTo(_metaArea.PointerToOffset(newAddress));
				foreach (int i in ints)
					stream.WriteInt32(i);

				cache.Add(newAddress, ints);
			}

			uint cont = expander.Contract(newAddress);

			values.SetInteger(countName, (uint)ints.Length);
			values.SetInteger(addressName, cont);
		}

		private static void FreeBitArray(StructureValueCollection values, string countName, string addressName, MetaAllocator allocator, IPointerExpander expander)
		{
			if (!values.HasInteger(countName) || !values.HasInteger(addressName))
				return;

			var oldCount = (int)values.GetInteger(countName);
			uint oldAddress = (uint)values.GetInteger(addressName);

			long expand = expander.Expand(oldAddress);

			if (oldCount > 0 && oldAddress > 0)
				allocator.Free(expand, oldCount*4);
		}
	}
}
