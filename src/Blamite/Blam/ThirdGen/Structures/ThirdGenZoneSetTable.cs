using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Resources;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Structures
{
	public class ThirdGenZoneSetTable : IZoneSetTable
	{
		private readonly MetaAllocator _allocator;
		private readonly EngineDescription _buildInfo;
		private readonly ThirdGenResourceGestalt _gestalt;
		private readonly FileSegmentGroup _metaArea;

		public ThirdGenZoneSetTable(ThirdGenResourceGestalt gestalt, IReader reader, FileSegmentGroup metaArea,
			MetaAllocator allocator, EngineDescription buildInfo)
		{
			_gestalt = gestalt;
			_metaArea = metaArea;
			_allocator = allocator;
			_buildInfo = buildInfo;
			Load(reader);
		}

		/// <summary>
		///     Gets the global zone set. This takes priority over all other zone sets.
		/// </summary>
		public IZoneSet GlobalZoneSet { get; private set; }

		public IZoneSet UnattachedZoneSet { get; private set; }

		public IZoneSet DiscForbiddenZoneSet { get; private set; }

		public IZoneSet DiscAlwaysStreamingZoneSet { get; private set; }

		public IZoneSet[] GeneralZoneSets { get; private set; }

		public IZoneSet[] BSPZoneSets { get; private set; }

		public IZoneSet[] BSPZoneSets2 { get; private set; }

		public IZoneSet[] BSPZoneSets3 { get; private set; }

		public IZoneSet[] CinematicZoneSets { get; private set; }

		public IZoneSet[] CustomZoneSets { get; private set; }

		/// <summary>
		///     Saves changes made to zone sets in the table.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		/// <exception cref="System.NotImplementedException"></exception>
		public void SaveChanges(IStream stream)
		{
			StructureValueCollection tagValues = _gestalt.LoadTag(stream);
			FreeZoneSets(tagValues, stream);

			var cache = new ReflexiveCache<int>();
			SaveZoneSetTable(GlobalZoneSet, tagValues, "number of global zone sets", "global zone set table address", cache, stream);
			SaveZoneSetTable(UnattachedZoneSet, tagValues, "number of unattached zone sets", "unattached zone set table address", cache, stream);
			SaveZoneSetTable(DiscForbiddenZoneSet, tagValues, "number of disc forbidden zone sets", "disc forbidden zone set table address", cache, stream);
			SaveZoneSetTable(DiscAlwaysStreamingZoneSet, tagValues, "number of disc always streaming zone sets", "disc always streaming zone set table address", cache, stream);
			SaveZoneSetTable(GeneralZoneSets, tagValues, "number of general zone sets", "general zone set table address", cache, stream);
			SaveZoneSetTable(BSPZoneSets, tagValues, "number of bsp zone sets", "bsp zone set table address", cache, stream);
			SaveZoneSetTable(BSPZoneSets2, tagValues, "number of bsp 2 zone sets", "bsp 2 zone set table address", cache, stream);
			SaveZoneSetTable(BSPZoneSets3, tagValues, "number of bsp 3 zone sets", "bsp 3 zone set table address", cache, stream);
			SaveZoneSetTable(CinematicZoneSets, tagValues, "number of cinematic zone sets", "cinematic zone set table address", cache, stream);
			SaveZoneSetTable(CustomZoneSets, tagValues, "number of custom zone sets", "custom zone set table address", cache, stream);

			_gestalt.SaveTag(tagValues, stream);
		}

		private void Load(IReader reader)
		{
			StructureValueCollection tagValues = _gestalt.LoadTag(reader);

			// Global, unattached, disc-forbidden, and disc-always-streaming usually only have one entry
			GlobalZoneSet = ReadZoneSetTable(tagValues, "number of global zone sets", "global zone set table address", reader).FirstOrDefault();
			UnattachedZoneSet = ReadZoneSetTable(tagValues, "number of unattached zone sets", "unattached zone set table address", reader).FirstOrDefault();
			DiscForbiddenZoneSet = ReadZoneSetTable(tagValues, "number of disc forbidden zone sets", "disc forbidden zone set table address", reader).FirstOrDefault();
			DiscAlwaysStreamingZoneSet = ReadZoneSetTable(tagValues, "number of disc always streaming zone sets", "disc always streaming zone set table address", reader).FirstOrDefault();

			// Everything else needs to be an array
			GeneralZoneSets = ReadZoneSetTable(tagValues, "number of general zone sets", "general zone set table address", reader).ToArray();
			BSPZoneSets = ReadZoneSetTable(tagValues, "number of bsp zone sets", "bsp zone set table address", reader).ToArray();
			BSPZoneSets2 = ReadZoneSetTable(tagValues, "number of bsp 2 zone sets", "bsp 2 zone set table address", reader).ToArray();
			BSPZoneSets3 = ReadZoneSetTable(tagValues, "number of bsp 3 zone sets", "bsp 3 zone set table address", reader).ToArray();
			CinematicZoneSets = ReadZoneSetTable(tagValues, "number of cinematic zone sets", "cinematic zone set table address", reader).ToArray();
			CustomZoneSets = ReadZoneSetTable(tagValues, "number of custom zone sets", "custom zone set table address", reader).ToArray();
		}

		private IEnumerable<ThirdGenZoneSet> ReadZoneSetTable(StructureValueCollection tagValues, string countName, string addressName, IReader reader)
		{
			if (!tagValues.HasInteger(countName) || !tagValues.HasInteger(addressName))
				return Enumerable.Empty<ThirdGenZoneSet>();

			var count = (int) tagValues.GetInteger(countName);
			uint address = tagValues.GetInteger(addressName);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("zone set definition");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
			return entries.Select(e => new ThirdGenZoneSet(e, reader, _metaArea));
		}

		private void SaveZoneSetTable(IZoneSet set, StructureValueCollection tagValues, string countName, string addressName, ReflexiveCache<int> cache, IStream stream)
		{
			SaveZoneSetTable(new[] {set}, tagValues, countName, addressName, cache, stream);
		}

		private void SaveZoneSetTable(IZoneSet[] sets, StructureValueCollection tagValues, string countName, string addressName, ReflexiveCache<int> cache, IStream stream)
		{
			if (!tagValues.HasInteger(countName) || !tagValues.HasInteger(addressName))
				return;

			var count = (int) tagValues.GetInteger(countName);
			if (count != sets.Length)
				throw new InvalidOperationException("Zone set count does not match");

			uint address = tagValues.GetInteger(addressName);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("zone set definition");
			List<StructureValueCollection> entries =
				sets.Select(set => ((ThirdGenZoneSet) set).Serialize(stream, _allocator, cache)).ToList();
			ReflexiveWriter.WriteReflexive(entries, address, layout, _metaArea, stream);
		}

		private void FreeZoneSets(StructureValueCollection tagValues, IReader reader)
		{
			FreeZoneSetsInTable(tagValues, "number of global zone sets", "global zone set table address", reader);
			FreeZoneSetsInTable(tagValues, "number of unattached zone sets", "unattached zone set table address", reader);
			FreeZoneSetsInTable(tagValues, "number of disc forbidden zone sets", "disc forbidden zone set table address", reader);
			FreeZoneSetsInTable(tagValues, "number of disc always streaming zone sets", "disc always streaming zone set table address", reader);
			FreeZoneSetsInTable(tagValues, "number of general zone sets", "general zone set table address", reader);
			FreeZoneSetsInTable(tagValues, "number of bsp zone sets", "bsp zone set table address", reader);
			FreeZoneSetsInTable(tagValues, "number of bsp 2 zone sets", "bsp 2 zone set table address", reader);
			FreeZoneSetsInTable(tagValues, "number of bsp 3 zone sets", "bsp 3 zone set table address", reader);
			FreeZoneSetsInTable(tagValues, "number of cinematic zone sets", "cinematic zone set table address", reader);
			FreeZoneSetsInTable(tagValues, "number of custom zone sets", "custom zone set table address", reader);
		}

		private void FreeZoneSetsInTable(StructureValueCollection tagValues, string countName, string addressName, IReader reader)
		{
			if (!tagValues.HasInteger(countName) || !tagValues.HasInteger(addressName))
				return;

			var count = (int) tagValues.GetInteger(countName);
			uint address = tagValues.GetInteger(addressName);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("zone set definition");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
			foreach (StructureValueCollection entry in entries)
				ThirdGenZoneSet.Free(entry, _allocator);
		}
	}
}