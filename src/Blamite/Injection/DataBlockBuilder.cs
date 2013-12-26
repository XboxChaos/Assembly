using System;
using System.Collections.Generic;
using Blamite.Blam;
using Blamite.Blam.Shaders;
using Blamite.Flexibility;
using Blamite.IO;
using Blamite.Plugins;

namespace Blamite.Injection
{
	/// <summary>
	///     A plugin processor which uses a plugin to recursively extract DataBlocks from a tag.
	/// </summary>
	public class DataBlockBuilder : IPluginVisitor
	{
		private readonly Stack<List<DataBlock>> _blockStack = new Stack<List<DataBlock>>();
		private readonly ICacheFile _cacheFile;
		private readonly StructureLayout _dataRefLayout;
		private readonly IReader _reader;
		private readonly StructureLayout _reflexiveLayout;
		private readonly SegmentPointer _tagLocation;
		private readonly StructureLayout _tagRefLayout;
		private List<DataBlock> _reflexiveBlocks;

		/// <summary>
		///     Initializes a new instance of the <see cref="DataBlockBuilder" /> class.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="tagLocation">The location of the tag to load data blocks for.</param>
		/// <param name="cacheFile">The cache file.</param>
		/// <param name="buildInfo">The build info for the cache file.</param>
		public DataBlockBuilder(IReader reader, SegmentPointer tagLocation, ICacheFile cacheFile, EngineDescription buildInfo)
		{
			_reader = reader;
			_tagLocation = tagLocation;
			_cacheFile = cacheFile;
			_tagRefLayout = buildInfo.Layouts.GetLayout("tag reference");
			_reflexiveLayout = buildInfo.Layouts.GetLayout("reflexive");
			_dataRefLayout = buildInfo.Layouts.GetLayout("data reference");

			DataBlocks = new List<DataBlock>();
			ReferencedTags = new HashSet<DatumIndex>();
			ReferencedResources = new HashSet<DatumIndex>();
		}

		/// <summary>
		///     Gets a list of data blocks that were created.
		/// </summary>
		public List<DataBlock> DataBlocks { get; private set; }

		/// <summary>
		///     Gets a set of tags referenced by the scanned tag.
		/// </summary>
		public HashSet<DatumIndex> ReferencedTags { get; private set; }

		/// <summary>
		///     Gets a set of resources referenced by the scanned tag.
		/// </summary>
		public HashSet<DatumIndex> ReferencedResources { get; private set; }

		public bool EnterPlugin(int baseSize)
		{
			// Read the tag data in based off the base size
			_reader.SeekTo(_tagLocation.AsOffset());
			var data = _reader.ReadBlock(baseSize);

			// Create a block for it and push it onto the block stack
			var block = new DataBlock(_tagLocation.AsPointer(), 1, 4, data);
			DataBlocks.Add(block);

			var blockList = new List<DataBlock> {block};
			_blockStack.Push(blockList);

			return true;
		}

		public void LeavePlugin()
		{
			_blockStack.Pop();
		}

		public bool EnterRevisions()
		{
			return false;
		}

		public void VisitRevision(PluginRevision revision)
		{
		}

		public void LeaveRevisions()
		{
		}

		public void VisitComment(string title, string text, uint pluginLine)
		{
		}

		public void VisitUInt8(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitInt8(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitUInt16(string name, uint offset, bool visible, uint pluginLine)
		{
			// haxhaxhaxhax
			// TODO: Fix this if/when cross-tag references are added to plugins
			var lowerName = name.ToLower();
			if (lowerName.Contains("asset salt")
			    || lowerName.Contains("resource salt")
			    || lowerName.Contains("asset datum salt")
			    || lowerName.Contains("resource datum salt"))
			{
				ReadReferences(offset, ReadResourceFixup);
			}
		}

		public void VisitInt16(string name, uint offset, bool visible, uint pluginLine)
		{
			VisitUInt16(name, offset, visible, pluginLine);
		}

		public void VisitUInt32(string name, uint offset, bool visible, uint pluginLine)
		{
			// haxhaxhaxhax
			// TODO: Fix this if/when cross-tag references are added to plugins
			var lowerName = name.ToLower();
			if (lowerName.Contains("asset index")
			    || lowerName.Contains("resource index")
			    || lowerName.Contains("asset datum")
			    || lowerName.Contains("resource datum"))
			{
				ReadReferences(offset, ReadResourceFixup);
			}
		}

		public void VisitInt32(string name, uint offset, bool visible, uint pluginLine)
		{
			VisitUInt32(name, offset, visible, pluginLine);
		}

		public void VisitFloat32(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitUndefined(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitVector3(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitDegree(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitStringID(string name, uint offset, bool visible, uint pluginLine)
		{
			ReadReferences(offset, ReadStringId);
		}

		public void VisitTagReference(string name, uint offset, bool visible, bool withClass, bool showJumpTo, uint pluginLine)
		{
			ReadReferences(offset, (b, o) => ReadTagReference(b, o, withClass));
		}

		public void VisitDataReference(string name, uint offset, string format, bool visible, int align, uint pluginLine)
		{
			ReadReferences(offset, (b, o) => ReadDataReference(b, o, align));
		}

		public void VisitRawData(string name, uint offset, bool visible, int size, uint pluginLine)
		{
		}

		public void VisitRange(string name, uint offset, bool visible, string type, double min, double max, double smallChange,
			double largeChange, uint pluginLine)
		{
		}

		public void VisitAscii(string name, uint offset, bool visible, int size, uint pluginLine)
		{
		}

		public void VisitUtf16(string name, uint offset, bool visible, int size, uint pluginLine)
		{
		}

		public void VisitColorInt(string name, uint offset, bool visible, string format, uint pluginLine)
		{
		}

		public void VisitColorF(string name, uint offset, bool visible, string format, uint pluginLine)
		{
		}

		public bool EnterBitfield8(string name, uint offset, bool visible, uint pluginLine)
		{
			return false;
		}

		public bool EnterBitfield16(string name, uint offset, bool visible, uint pluginLine)
		{
			return false;
		}

		public bool EnterBitfield32(string name, uint offset, bool visible, uint pluginLine)
		{
			return false;
		}

		public void VisitBit(string name, int index)
		{
		}

		public void LeaveBitfield()
		{
		}

		public bool EnterEnum8(string name, uint offset, bool visible, uint pluginLine)
		{
			return false;
		}

		public bool EnterEnum16(string name, uint offset, bool visible, uint pluginLine)
		{
			return false;
		}

		public bool EnterEnum32(string name, uint offset, bool visible, uint pluginLine)
		{
			return false;
		}

		public void VisitOption(string name, int value)
		{
		}

		public void LeaveEnum()
		{
		}

		public bool EnterReflexive(string name, uint offset, bool visible, uint entrySize, int align, uint pluginLine)
		{
			_reflexiveBlocks = new List<DataBlock>();
			ReadReferences(offset, (b, o) => ReadReflexive(b, o, entrySize, align));
			if (_reflexiveBlocks.Count <= 0) return false;
			_blockStack.Push(_reflexiveBlocks);
			return true;
		}

		public void LeaveReflexive()
		{
			// Pop the block stack
			_blockStack.Pop();
		}

		public void VisitShader(string name, uint offset, bool visible, ShaderType type, uint pluginLine)
		{
			ReadReferences(offset, (b, o) => ReadShader(b, o, type));
		}

		private void ReadReferences(uint offset, Action<DataBlock, uint> processor)
		{
			var blocks = _blockStack.Peek();
			foreach (var block in blocks)
			{
				var currentOffset = offset;
				for (var i = 0; i < block.EntryCount; i++)
				{
					processor(block, currentOffset);
					currentOffset += (uint) block.EntrySize;
				}
			}
		}

		private void ReadStringId(DataBlock block, uint offset)
		{
			SeekToOffset(block, offset);
			var sid = new StringID(_reader.ReadUInt32());
			if (sid == StringID.Null) return;

			var str = _cacheFile.StringIDs.GetString(sid);
			if (str == null) return;

			var fixup = new DataBlockStringIDFixup(str, (int) offset);
			block.StringIDFixups.Add(fixup);
		}

		private void ReadResourceFixup(DataBlock block, uint offset)
		{
			SeekToOffset(block, offset);
			var index = new DatumIndex(_reader.ReadUInt32());
			if (!index.IsValid) return;

			var fixup = new DataBlockResourceFixup(index, (int) offset);
			block.ResourceFixups.Add(fixup);
			ReferencedResources.Add(index);
		}

		private DataBlock ReadDataBlock(uint pointer, int entrySize, int entryCount, int align)
		{
			_reader.SeekTo(_cacheFile.MetaArea.PointerToOffset(pointer));
			var data = _reader.ReadBlock(entrySize*entryCount);

			var block = new DataBlock(pointer, entryCount, align, data);
			DataBlocks.Add(block);
			return block;
		}

		private void ReadTagReference(DataBlock block, uint offset, bool withClass)
		{
			SeekToOffset(block, offset);

			DatumIndex index;
			int fixupOffset;
			bool valid;
			if (withClass)
			{
				// Class info - do a flexible structure read to get the index
				var values = StructureReader.ReadStructure(_reader, _tagRefLayout);
				var classMagic = (int) values.GetInteger("class magic");
				index = new DatumIndex(values.GetInteger("datum index"));
				fixupOffset = (int) offset + _tagRefLayout.GetFieldOffset("datum index");
				valid = _cacheFile.Tags.IsValidIndex(index, classMagic);
			}
			else
			{
				// No tag class - the datum index is at the offset
				index = new DatumIndex(_reader.ReadUInt32());
				fixupOffset = (int) offset;
				valid = _cacheFile.Tags.IsValidIndex(index);
			}

			if (!valid) return;

			// Add the tagref fixup to the block
			var fixup = new DataBlockTagFixup(index, fixupOffset);
			block.TagFixups.Add(fixup);
			ReferencedTags.Add(index);
		}

		private void ReadDataReference(DataBlock block, uint offset, int align)
		{
			// Read the size and pointer
			SeekToOffset(block, offset);
			var values = StructureReader.ReadStructure(_reader, _dataRefLayout);
			var size = (int) values.GetInteger("size");
			var pointer = values.GetInteger("pointer");

			if (size <= 0 || !_cacheFile.MetaArea.ContainsBlockPointer(pointer, size)) return;

			// Read the block and create a fixup for it
			ReadDataBlock(pointer, size, 1, align);
			var fixup = new DataBlockAddressFixup(pointer, (int) offset + _dataRefLayout.GetFieldOffset("pointer"));
			block.AddressFixups.Add(fixup);
		}

		private void ReadReflexive(DataBlock block, uint offset, uint entrySize, int align)
		{
			// Read the count and pointer
			SeekToOffset(block, offset);
			var values = StructureReader.ReadStructure(_reader, _reflexiveLayout);
			var count = (int) values.GetInteger("entry count");
			var pointer = values.GetInteger("pointer");

			if (count <= 0 || !_cacheFile.MetaArea.ContainsBlockPointer(pointer, (int) (count*entrySize))) return;

			var newBlock = ReadDataBlock(pointer, (int)entrySize, count, align);

			// Now create a fixup for the block
			var fixup = new DataBlockAddressFixup(pointer, (int) offset + _reflexiveLayout.GetFieldOffset("pointer"));
			block.AddressFixups.Add(fixup);

			// Add it to _reflexiveBlocks so it'll be recursed into
			_reflexiveBlocks.Add(newBlock);
		}

		private void ReadShader(DataBlock block, uint offset, ShaderType type)
		{
			SeekToOffset(block, offset);
			var data = _cacheFile.ShaderStreamer.ExportShader(_reader, type);
			var fixup = new DataBlockShaderFixup((int)offset, data);
			block.ShaderFixups.Add(fixup);
		}

		private void SeekToOffset(DataBlock block, uint offset)
		{
			var baseOffset = _cacheFile.MetaArea.PointerToOffset(block.OriginalAddress);
			_reader.SeekTo(baseOffset + offset);
		}
	}
}