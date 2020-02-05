using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Util;
using System.Collections.Generic;
using System.Linq;

namespace Blamite.Blam.ThirdGen.Structures
{
	public class ThirdGenSimulationDefinitionTable : ISimulationDefinitionTable
	{
		private List<ITag> _table;
		private bool _changed = false;

		private ITag _scenario;
		private TagTable _tags;
		private FileSegmentGroup _metaArea;
		private MetaAllocator _allocator;
		private EngineDescription _buildInfo;
		private IPointerExpander _expander;

		public ThirdGenSimulationDefinitionTable(ITag scenario, TagTable tags, IReader reader, FileSegmentGroup metaArea, MetaAllocator allocator, EngineDescription buildInfo, IPointerExpander expander)
		{
			_scenario = scenario;
			_tags = tags;
			_metaArea = metaArea;
			_allocator = allocator;
			_buildInfo = buildInfo;
			_expander = expander;

			Load(reader);
		}

		public void Add(ITag tag)
		{
			// Insert the tag in order by datum index
			// NOTE: Casting the datum index to a signed value is necessary because that's what the XEX does
			var index = ListSearching.BinarySearch(_table, (int)tag.Index.Value, (t) => (int)t.Index.Value);
			if (index >= 0)
				return;
			index = ~index;
			_table.Insert(index, tag);
			_changed = true;
		}

		public void Remove(ITag tag)
		{
			if (_table.Remove(tag))
				_changed = true;
		}

		public bool Contains(ITag tag)
		{
			return _table.Contains(tag);
		}

		public int Count
		{
			get { return _table.Count; }
		}

		public IEnumerator<ITag> GetEnumerator()
		{
			return _table.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((System.Collections.IEnumerable)_table).GetEnumerator();
		}

		public void SaveChanges(IStream stream)
		{
			if (!_changed)
				return;

			var scenarioLayout = _buildInfo.Layouts.GetLayout("scnr");
			stream.SeekTo(_scenario.MetaLocation.AsOffset());
			var scenarioData = StructureReader.ReadStructure(stream, scenarioLayout);
			var oldCount = (int)scenarioData.GetInteger("simulation definition table count");
			var oldAddress = (uint)scenarioData.GetInteger("simulation definition table address");

			long expand = _expander.Expand(oldAddress);

			var entryLayout = _buildInfo.Layouts.GetLayout("simulation definition table entry");

			var newTable = _table.Select(SerializeTag);
			var newAddr = ReflexiveWriter.WriteReflexive(newTable, oldCount, expand, _table.Count, entryLayout, _metaArea, _allocator, stream);

			uint cont = _expander.Contract(newAddr);

			scenarioData.SetInteger("simulation definition table count", (uint)_table.Count);
			scenarioData.SetInteger("simulation definition table address", cont);
			stream.SeekTo(_scenario.MetaLocation.AsOffset());
			StructureWriter.WriteStructure(scenarioData, scenarioLayout, stream);
			_changed = false;
		}

		private static StructureValueCollection SerializeTag(ITag tag)
		{
			var result = new StructureValueCollection();
			result.SetInteger("datum index", tag.Index.Value);
			return result;
		}

		private void Load(IReader reader)
		{
			reader.SeekTo(_scenario.MetaLocation.AsOffset());
			var scenarioLayout = _buildInfo.Layouts.GetLayout("scnr");
			var scenarioData = StructureReader.ReadStructure(reader, scenarioLayout);

			var count = (int)scenarioData.GetInteger("simulation definition table count");
			var address = (uint)scenarioData.GetInteger("simulation definition table address");

			long expand = _expander.Expand(address);

			var entryLayout = _buildInfo.Layouts.GetLayout("simulation definition table entry");
			_table = ReflexiveReader.ReadReflexive(reader, count, expand, entryLayout, _metaArea).Select((e) => _tags[new DatumIndex(e.GetInteger("datum index"))]).ToList();
		}
	}
}
