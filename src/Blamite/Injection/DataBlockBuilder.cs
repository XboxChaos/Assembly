using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam;
using Blamite.Flexibility;
using Blamite.IO;
using Blamite.Plugins;

namespace Blamite.Injection
{
    /// <summary>
    /// A plugin processor which uses a plugin to recursively extract DataBlocks from a tag.
    /// </summary>
    public class DataBlockBuilder : IPluginVisitor
    {
        private IReader _reader;
        private SegmentPointer _baseLocation;
        private FileSegmentGroup _metaArea;
        private StructureLayout _tagRefLayout;
        private StructureLayout _reflexiveLayout;
        private StructureLayout _dataRefLayout;
        private Stack<DataBlock> _blockStack = new Stack<DataBlock>();
        private Stack<SegmentPointer> _locationStack = new Stack<SegmentPointer>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataBlockBuilder" /> class.
        /// </summary>
        /// <param name="reader">The stream to read from.</param>
        /// <param name="tagLocation">The location of the tag to load data blocks for.</param>
        /// <param name="metaArea">The meta area of the cache file.</param>
        /// <param name="buildInfo">The build info for the cache file.</param>
        public DataBlockBuilder(IReader reader, SegmentPointer tagLocation, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            _reader = reader;
            _baseLocation = tagLocation;
            _metaArea = metaArea;
            _tagRefLayout = buildInfo.GetLayout("tag reference");
            _reflexiveLayout = buildInfo.GetLayout("reflexive");
            _dataRefLayout = buildInfo.GetLayout("data reference");

            DataBlocks = new List<DataBlock>();
            ReferencedTags = new HashSet<DatumIndex>();
            ReferencedResources = new HashSet<DatumIndex>();
        }

        /// <summary>
        /// Gets a list of data blocks that were created.
        /// </summary>
        public List<DataBlock> DataBlocks { get; private set; }

        /// <summary>
        /// Gets a set of tags referenced by the scanned tag.
        /// </summary>
        public HashSet<DatumIndex> ReferencedTags { get; private set; }

        /// <summary>
        /// Gets a set of resources referenced by the scanned tag.
        /// </summary>
        public HashSet<DatumIndex> ReferencedResources { get; private set; }

        public bool EnterPlugin(int baseSize)
        {
            // Read the tag data in based off the base size
            _reader.SeekTo(_baseLocation.AsOffset());
            byte[] data = _reader.ReadBlock(baseSize);

            // Create a block for it and push it onto the block stack
            var block = new DataBlock(_baseLocation.AsPointer(), data);
            DataBlocks.Add(block);
            _blockStack.Push(block);

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
            string lowerName = name.ToLower();
            if (lowerName.Contains("asset salt")
                || lowerName.Contains("resource salt")
                || lowerName.Contains("asset datum salt")
                || lowerName.Contains("resource datum salt"))
            {
                ReadResourceFixup(offset);
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
            string lowerName = name.ToLower();
            if (lowerName.Contains("asset index")
                || lowerName.Contains("resource index")
                || lowerName.Contains("asset datum")
                || lowerName.Contains("resource datum"))
            {
                ReadResourceFixup(offset);
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
        }

        public void VisitTagReference(string name, uint offset, bool visible, bool withClass, bool showJumpTo, uint pluginLine)
        {
            _reader.SeekTo(_baseLocation.AsOffset() + offset);

            DatumIndex index;
            int fixupOffset;
            if (withClass)
            {
                // Class info - do a flexible structure read to get the index
                var values = StructureReader.ReadStructure(_reader, _tagRefLayout);
                index = new DatumIndex(values.GetInteger("datum index"));
                fixupOffset = (int)offset + _tagRefLayout.GetFieldOffset("datum index");
            }
            else
            {
                // No tag class - the datum index is at the offset
                index = new DatumIndex(_reader.ReadUInt32());
                fixupOffset = (int)offset;
            }

            if (index != DatumIndex.Null)
            {
                // Add the tagref fixup to the block
                var fixup = new DataBlockTagFixup(index, fixupOffset);
                _blockStack.Peek().TagFixups.Add(fixup);
                ReferencedTags.Add(index);
            }
        }

        public void VisitDataReference(string name, uint offset, string format, bool visible, uint pluginLine)
        {
            // Read the size and pointer
            _reader.SeekTo(_baseLocation.AsOffset() + offset);
            var values = StructureReader.ReadStructure(_reader, _dataRefLayout);
            int size = (int)values.GetInteger("size");
            uint pointer = (uint)values.GetInteger("pointer");

            if (size > 0 && _metaArea.ContainsPointer(pointer))
            {
                // Read the block and create a fixup for it
                var block = ReadDataBlock(pointer, size);
                var fixup = new DataBlockAddressFixup(pointer, (int)offset + _dataRefLayout.GetFieldOffset("pointer"));
                _blockStack.Peek().AddressFixups.Add(fixup);
            }
        }

        public void VisitRawData(string name, uint offset, bool visible, int size, uint pluginLine)
        {
        }

        public void VisitRange(string name, uint offset, bool visible, string type, double min, double max, double smallChange, double largeChange, uint pluginLine)
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

        public bool EnterReflexive(string name, uint offset, bool visible, uint entrySize, uint pluginLine)
        {
            // Read the count and pointer
            _reader.SeekTo(_baseLocation.AsOffset() + offset);
            var values = StructureReader.ReadStructure(_reader, _reflexiveLayout);
            int count = (int)values.GetInteger("entry count");
            uint pointer = values.GetInteger("pointer");

            if (count > 0 && _metaArea.ContainsPointer(pointer))
            {
                int size = (int)(count * entrySize);
                var block = ReadDataBlock(pointer, size);

                // Now create a fixup for the block
                var fixup = new DataBlockAddressFixup(pointer, (int)offset + _reflexiveLayout.GetFieldOffset("pointer"));
                _blockStack.Peek().AddressFixups.Add(fixup);

                // Push it onto the block stack and recurse into it
                _blockStack.Push(block);
                _locationStack.Push(_baseLocation);
                _baseLocation = SegmentPointer.FromPointer(pointer, _metaArea);
                return true;
            }

            // Null reflexive - don't recurse
            return false;
        }

        public void LeaveReflexive()
        {
            // Pop the block stack and pop our location
            _blockStack.Pop();
            _baseLocation = _locationStack.Pop();
        }

        private void ReadResourceFixup(uint offset)
        {
            _reader.SeekTo(_baseLocation.AsOffset() + offset);
            DatumIndex index = new DatumIndex(_reader.ReadUInt32());
            if (index.IsValid)
            {
                var fixup = new DataBlockResourceFixup(index, (int)offset);
                _blockStack.Peek().ResourceFixups.Add(fixup);
                ReferencedResources.Add(index);
            }
        }

        private DataBlock ReadDataBlock(uint pointer, int size)
        {
            _reader.SeekTo(_metaArea.PointerToOffset(pointer));
            byte[] data = _reader.ReadBlock(size);

            var block = new DataBlock(pointer, data);
            DataBlocks.Add(block);
            return block;
        }
    }
}
