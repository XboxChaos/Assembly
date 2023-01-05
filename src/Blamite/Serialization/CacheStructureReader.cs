using Blamite.Blam;
using Blamite.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Serialization
{
    public class CacheStructureReader : StructureReader, ICacheStructureLayoutVisitor
    {
		private readonly ICacheFile _cache;
		private readonly EngineDescription _buildInfo;

        public CacheStructureReader(IReader reader, ICacheFile cache, EngineDescription buildInfo) : base(reader)
        {
			_cache = cache;
			_buildInfo = buildInfo;
        }

		public void VisitTagBlockField(string name, int offset, StructureLayout layout)
		{
			SeekReader(offset);
			var tagBlockLayout = _buildInfo.Layouts.GetLayout("tag block");
			var blockHeader = ReadStructure(_reader, _cache, _buildInfo, tagBlockLayout);
			uint pointer = (uint)blockHeader.GetInteger("pointer");
			int count = (int)blockHeader.GetInteger("entry count");
			long expandedPointer = _cache.PointerExpander.Expand(pointer);

			if (count == 0 || !_cache.MetaArea.ContainsPointer(expandedPointer))
			{
				_collection.SetTagBlock(name, new StructureValueCollection[0]);
			}
			else
			{
				var elements = new StructureValueCollection[count];
				uint blockOffset = _cache.MetaArea.PointerToOffset(expandedPointer);
				_reader.SeekTo(blockOffset);
				for (int i = 0; i < count; i++)
				{
					elements[i] = ReadStructure(_reader, _cache, _buildInfo, layout);
				}
				_collection.SetTagBlock(name, elements);
			}
		}

		public void VisitStringIDField(string name, int offset)
        {
			SeekReader(offset);
			uint value = _reader.ReadUInt32();
			string id = _cache.StringIDs.GetString(new StringID(value));
			_collection.SetStringID(name, id);
			_offset += 4;
        }

		public void VisitTagReferenceField(string name, int offset, bool withGroup)
		{
			SeekReader(offset);
			DatumIndex index;
			if(withGroup)
            {
				var tagRefLayout = _buildInfo.Layouts.GetLayout("tag reference");
				var tagRefValues = ReadStructure(_reader, _cache, _buildInfo, tagRefLayout);
				index = new DatumIndex(tagRefValues.GetInteger("datum index"));
            }
			else
            {
				index = new DatumIndex(_reader.ReadUInt32());
				_offset += 4;
			}
			
			if(index.IsValid)
            {
				ITag tag = _cache.Tags[index];
				_collection.SetTagReference(name, tag);
            }
			else
            {
				_collection.SetTagReference(name, null);
            }
		}

		/// <summary>
		///     Reads a structure from a stream by following a predefined structure layout.
		/// </summary>
		/// <param name="reader">The IReader to read the structure from.</param>
		/// <param name="cache">The cache file to read from.</param>
		/// <param name="buildInfo"></param>
		/// <param name="layout">The structure layout to follow.</param>
		/// <returns>A collection of the values that were read.</returns>
		/// <seealso cref="StructureLayout" />
		public static StructureValueCollection ReadStructure(IReader reader, ICacheFile cache, EngineDescription buildInfo, StructureLayout layout)
		{
			var structReader = new CacheStructureReader(reader, cache, buildInfo);
			layout.Accept(structReader);
			if (layout.Size > 0)
				structReader.SeekReader(layout.Size);

			return structReader._collection;
		}
	}
}
