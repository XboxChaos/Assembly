using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.Blam;
using Blamite.Blam.Shaders;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Plugins;
using Blamite.Blam.Localization;

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
		private readonly StructureLayout _tagBlockLayout;
		private readonly ITag _tag;
		private readonly StructureLayout _tagRefLayout;
		private CachedLanguagePackLoader _languageCache;
		private List<DataBlock> _reflexiveBlocks;

		/// <summary>
		///     Initializes a new instance of the <see cref="DataBlockBuilder" /> class.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="tag">The tag to load data blocks for.</param>
		/// <param name="cacheFile">The cache file.</param>
		/// <param name="buildInfo">The build info for the cache file.</param>
		public DataBlockBuilder(IReader reader, ITag tag, ICacheFile cacheFile, EngineDescription buildInfo)
		{
			_reader = reader;
			_tag = tag;
			_cacheFile = cacheFile;
			_languageCache = new CachedLanguagePackLoader(_cacheFile.Languages);
			_tagRefLayout = buildInfo.Layouts.GetLayout("tag reference");
			_tagBlockLayout = buildInfo.Layouts.GetLayout("tag block");
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
			_reader.SeekTo(_tag.MetaLocation.AsOffset());
			var data = _reader.ReadBlock(baseSize);

			// Create a block for it and push it onto the block stack
			uint cont = _cacheFile.PointerExpander.Contract(_tag.MetaLocation.AsPointer());

			var block = new DataBlock(cont, 1, 4, false, data);
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
			if (lowerName.Contains("global cache shader index"))
			{
				ReadReferences(offset, GlobalShaderFixup);

			}
			if (lowerName.Contains("polyart asset address"))
			{
				//ReadReferences(offset, PolyartFixup);
				ReadReferences(offset, (b, o) => ReadPolyart(b, o));
			}
			if (lowerName.Contains("compiled scenario index"))
			{
				switch (Util.CharConstant.ToString(_tag.Class.Magic))
				{
					case "effe":
						ReadReferences(offset, (b, o) => ReadCompiledEffect(b, o, EffectStorageType.Effect));
						break;
					case "beam":
						ReadReferences(offset, (b, o) => ReadCompiledEffect(b, o, EffectStorageType.Beam));
						break;
					case "cntl":
						ReadReferences(offset, (b, o) => ReadCompiledEffect(b, o, EffectStorageType.Contrail));
						break;
					case "ltvl":
						ReadReferences(offset, (b, o) => ReadCompiledEffect(b, o, EffectStorageType.LightVolume));
						break;
					default:
						return;
				}
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

		public void VisitPoint2(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitPoint3(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitVector2(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitVector3(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitVector4(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitDegree(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitDegree2(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitDegree3(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitPlane2(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitPlane3(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitRect16(string name, uint offset, bool visible, uint pluginLine)
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

		public void VisitAscii(string name, uint offset, bool visible, int size, uint pluginLine)
		{
		}

		public void VisitUtf16(string name, uint offset, bool visible, int size, uint pluginLine)
		{
		}

		public void VisitColorInt(string name, uint offset, bool visible, bool alpha, uint pluginLine)
		{
		}

		public void VisitColorF(string name, uint offset, bool visible, bool alpha, uint pluginLine)
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

		public bool EnterBitfield64(string name, uint offset, bool visible, uint pluginLine)
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

		public bool EnterReflexive(string name, uint offset, bool visible, uint entrySize, int align, bool sort, uint pluginLine)
		{
			_reflexiveBlocks = new List<DataBlock>();
			ReadReferences(offset, (b, o) => ReadReflexive(b, o, entrySize, align, sort));
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

		public void VisitRangeUInt16(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitRangeFloat32(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		public void VisitRangeDegree(string name, uint offset, bool visible, uint pluginLine)
		{
		}

		private UnicListFixupString CreateFixupString(LocalizedString str)
		{
			if (_cacheFile.StringIDs != null)
			{
				var stringId = _cacheFile.StringIDs.GetString(str.Key);
				if (stringId != null)
					return new UnicListFixupString(stringId, str.Value);
			}
			return new UnicListFixupString("", str.Value);
		}

		public void VisitUnicList(string name, uint offset, bool visible, int languages, uint pluginLine)
		{
			for (var i = 0; i < languages; i++)
			{
				var strings = LoadStringList(i, _tag);
				if (strings == null)
					continue;
				var fixupStrings = strings.Strings.Select(CreateFixupString).ToArray();
				var fixup = new DataBlockUnicListFixup(i, (int)(offset + i * 4), fixupStrings);
				_blockStack.Peek()[0].UnicListFixups.Add(fixup); // These will never be in tag blocks and I don't want to deal with it
			}
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

		private DataBlock ReadDataBlock(long pointer, int entrySize, int entryCount, int align, bool sort)
		{
			_reader.SeekTo(_cacheFile.MetaArea.PointerToOffset(pointer));
			var data = _reader.ReadBlock(entrySize*entryCount);

			uint cont = _cacheFile.PointerExpander.Contract(pointer);

			var block = new DataBlock(cont, entryCount, align, sort, data);
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
				valid = _cacheFile.Tags.IsValidIndex(index);
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
			var pointer = (uint)values.GetInteger("pointer");

			long expand = _cacheFile.PointerExpander.Expand(pointer);

			if (size <= 0 || !_cacheFile.MetaArea.ContainsBlockPointer(expand, size)) return;

			// Read the block and create a fixup for it
			ReadDataBlock(expand, size, 1, align, false);
			var fixup = new DataBlockAddressFixup(pointer, (int) offset + _dataRefLayout.GetFieldOffset("pointer"));
			block.AddressFixups.Add(fixup);
		}

		private void ReadReflexive(DataBlock block, uint offset, uint entrySize, int align, bool sort)
		{
			// Read the count and pointer
			SeekToOffset(block, offset);
			var values = StructureReader.ReadStructure(_reader, _tagBlockLayout);
			var count = (int) values.GetInteger("entry count");
			var pointer = (uint)values.GetInteger("pointer");

			long expand = _cacheFile.PointerExpander.Expand(pointer);

			if (count <= 0 || !_cacheFile.MetaArea.ContainsBlockPointer(expand, (int) (count*entrySize))) return;

			var newBlock = ReadDataBlock(expand, (int)entrySize, count, align, sort);

			// Now create a fixup for the block
			var fixup = new DataBlockAddressFixup(pointer, (int) offset + _tagBlockLayout.GetFieldOffset("pointer"));
			block.AddressFixups.Add(fixup);

			// Add it to _reflexiveBlocks so it'll be recursed into
			_reflexiveBlocks.Add(newBlock);
		}

		private void ReadShader(DataBlock block, uint offset, ShaderType type)
		{
			SeekToOffset(block, offset);

			//Don't bother if the pointer is null.
			if (_reader.ReadInt32() == 0)
				return;
			_reader.Skip(-4);

			var data = _cacheFile.ShaderStreamer.ExportShader(_reader, type);
			var fixup = new DataBlockShaderFixup((int)offset, data);
			block.ShaderFixups.Add(fixup);
		}

		private void GlobalShaderFixup(DataBlock block, uint offset)
		{
			SeekToOffset(block, offset);
			var index = _reader.ReadInt32();
			var shaderAddr = _reader.ReadUInt32();

			//check if we need to grab the shader
			if (shaderAddr == 0)
			{
				if (index != -1)
				{
					//make the index -1 in the datablock
					for (var i = 0; i < 4; i++)
						block.Data[offset + i] = 0xFF;

					//Grab the shader from gpix
					_reader.SeekTo(_cacheFile.Tags.FindTagByClass("gpix").MetaLocation.AsOffset() + 0x14);
					var gpixBase = _reader.ReadUInt32() - _cacheFile.MetaArea.PointerMask;
					_reader.SeekTo(gpixBase + (0x58 * index) + 0x54);

					//Make a shader fixup using the gpix shader
					var data = _cacheFile.ShaderStreamer.ExportShader(_reader, ShaderType.Pixel);
					var fixup = new DataBlockShaderFixup((int)offset + 4, data);
					block.ShaderFixups.Add(fixup);
				}
			}
			else
				return;
		}

		private void ReadPolyart(DataBlock block, uint offset)
		{
			// Read the address
			SeekToOffset(block, offset);
			var address = _reader.ReadUInt32();

			long expand = _cacheFile.PointerExpander.Expand(address);

			if (!_cacheFile.MetaArea.ContainsBlockPointer(expand, 24)) return;

			var newBlock = ReadDataBlock(expand, 24, 1, 16, false);
			ReadDataReference(newBlock, 0, 16);

			//get the type from the table
			uint loc = block.OriginalAddress;
			long expLoc = _cacheFile.PointerExpander.Expand(loc) + offset;
			uint cont = _cacheFile.PointerExpander.Contract(expLoc);

			ITagInterop a = _cacheFile.TagInteropTable.Where(p => p.Pointer == cont).FirstOrDefault();
			if (a == null) return;
			int type = a.Type;

			// Now create a fixup
			var fixup = new DataBlockModelDataFixup(type, address, (int)offset);
			block.ModelDataFixups.Add(fixup);
		}

		private void ReadCompiledEffect(DataBlock block, uint offset, EffectStorageType type)
		{
			if (_cacheFile.CompiledEffects == null)
				return;

			// Read the index
			SeekToOffset(block, offset);
			int index = _reader.ReadInt32();

			//get the data
			byte[] data = _cacheFile.CompiledEffects.GetData(type, index);

			// Now create a fixup
			var fixup = new DataBlockEffectFixup((int)type, index, (int)offset, data);
			block.EffectFixups.Add(fixup);
		}

		private LocalizedStringList LoadStringList(int languageIndex, ITag tag)
		{
			var pack = _languageCache.LoadLanguage((GameLanguage)languageIndex, _reader);
			if (pack == null)
				return null;
			return pack.FindStringListByTag(tag);
		}

		private void SeekToOffset(DataBlock block, uint offset)
		{
			long expand = _cacheFile.PointerExpander.Expand(block.OriginalAddress);

			var baseOffset = _cacheFile.MetaArea.PointerToOffset(expand);
			_reader.SeekTo(baseOffset + offset);
		}
	}
}