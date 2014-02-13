using System;
using System.Collections.Generic;
using System.Globalization;
using Blamite.Blam;
using Blamite.Flexibility;
using Blamite.IO;
using Blamite.Util;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	public class TagDataWriter : ITagDataFieldVisitor
	{
		public enum SaveType
		{
			File,
			Memory
		}

		private readonly ICacheFile _cache;
		private readonly FieldChangeSet _changes;
		private readonly StructureLayout _dataRefLayout;
		private readonly StructureLayout _tagBlockLayout;
		private readonly Trie _stringIdTrie;
		private readonly StructureLayout _tagRefLayout;
		private readonly SaveType _type;
		private readonly IWriter _writer;
		private uint _baseOffset;

		private bool _pokeTemplateFields = true;

		/// <summary>
		///     Save meta to the Blam Cache File
		/// </summary>
		public TagDataWriter(IWriter writer, uint baseOffset, ICacheFile cache, EngineDescription buildInfo, SaveType type,
			FieldChangeSet changes, Trie stringIdTrie)
		{
			_writer = writer;
			_baseOffset = baseOffset;
			_cache = cache;
			_type = type;
			_changes = changes;
			_stringIdTrie = stringIdTrie;

			// Load layouts
			_tagBlockLayout = buildInfo.Layouts.GetLayout("reflexive");
			_tagRefLayout = buildInfo.Layouts.GetLayout("tag reference");
			_dataRefLayout = buildInfo.Layouts.GetLayout("data reference");
		}

		public void VisitBitfield(BitfieldData field)
		{
			SeekToOffset(field.Offset);
			switch (field.Type)
			{
				case BitfieldType.Bitfield8:
					_writer.WriteByte((byte) field.Value);
					break;

				case BitfieldType.Bitfield16:
					_writer.WriteUInt16((ushort) field.Value);
					break;

				case BitfieldType.Bitfield32:
					_writer.WriteUInt32(field.Value);
					break;
			}
		}

		public void VisitComment(CommentData field)
		{
		}

		public void VisitEnum(EnumData field)
		{
			SeekToOffset(field.Offset);
			switch (field.Type)
			{
				case EnumType.Enum8:
					_writer.WriteSByte((sbyte) field.Value);
					break;

				case EnumType.Enum16:
					_writer.WriteInt16((short) field.Value);
					break;

				case EnumType.Enum32:
					_writer.WriteInt32(field.Value);
					break;
			}
		}

		public void VisitUint8(Uint8Data field)
		{
			SeekToOffset(field.Offset);
			_writer.WriteByte(field.Value);
		}

		public void VisitInt8(Int8Data field)
		{
			SeekToOffset(field.Offset);
			_writer.WriteSByte(field.Value);
		}

		public void VisitUint16(Uint16Data field)
		{
			SeekToOffset(field.Offset);
			_writer.WriteUInt16(field.Value);
		}

		public void VisitInt16(Int16Data field)
		{
			SeekToOffset(field.Offset);
			_writer.WriteInt16(field.Value);
		}

		public void VisitUint32(Uint32Data field)
		{
			SeekToOffset(field.Offset);
			_writer.WriteUInt32(field.Value);
		}

		public void VisitInt32(Int32Data field)
		{
			SeekToOffset(field.Offset);
			_writer.WriteInt32(field.Value);
		}

		public void VisitFloat32(Float32Data field)
		{
			SeekToOffset(field.Offset);
			_writer.WriteFloat(field.Value);
		}

		public void VisitColourInt(ColourData field)
		{
			SeekToOffset(field.Offset);

			if (field.Value.Length == 7)
				field.Value = field.Value.Insert(1, "FF");

			foreach (char formatChar in field.Format)
			{
				switch (formatChar)
				{
					case 'a':
						byte alpha = byte.Parse(field.Value.Replace("#", "").Remove(2), NumberStyles.HexNumber);
						_writer.WriteByte(alpha);
						break;
					case 'r':
						byte red = byte.Parse(field.Value.Replace("#", "").Remove(0, 2).Remove(2), NumberStyles.HexNumber);
						_writer.WriteByte(red);
						break;
					case 'g':
						byte green = byte.Parse(field.Value.Replace("#", "").Remove(0, 4).Remove(2), NumberStyles.HexNumber);
						_writer.WriteByte(green);
						break;
					case 'b':
						byte blue = byte.Parse(field.Value.Replace("#", "").Remove(0, 6), NumberStyles.HexNumber);
						_writer.WriteByte(blue);
						break;
				}
			}
		}

		public void VisitColourFloat(ColourData field)
		{
			SeekToOffset(field.Offset);

			if (field.Value.Length == 7)
				field.Value = field.Value.Insert(1, "FF");

			foreach (char formatChar in field.Format)
			{
				switch (formatChar)
				{
					case 'a':
						float alpha = Convert.ToSingle(int.Parse(field.Value.Replace("#", "").Remove(2), NumberStyles.HexNumber))/255;
						_writer.WriteFloat(alpha);
						break;
					case 'r':
						float red =
							Convert.ToSingle(int.Parse(field.Value.Replace("#", "").Remove(0, 2).Remove(2), NumberStyles.HexNumber))/255;
						_writer.WriteFloat(red);
						break;
					case 'g':
						float green =
							Convert.ToSingle(int.Parse(field.Value.Replace("#", "").Remove(0, 4).Remove(2), NumberStyles.HexNumber))/255;
						_writer.WriteFloat(green);
						break;
					case 'b':
						float blue = Convert.ToSingle(int.Parse(field.Value.Replace("#", "").Remove(0, 6), NumberStyles.HexNumber))/255;
						_writer.WriteFloat(blue);
						break;
				}
			}
		}

		public void VisitReflexive(TagBlockData field)
		{
			var values = new StructureValueCollection();
			values.SetInteger("entry count", (uint) field.Length);
			values.SetInteger("pointer", field.FirstEntryAddress);

			SeekToOffset(field.Offset);
			StructureWriter.WriteStructure(values, _tagBlockLayout, _writer);
		}

		public void VisitReflexiveEntry(WrappedTagBlockEntry field)
		{
		}

		public void VisitString(StringData field)
		{
			SeekToOffset(field.Offset);
			switch (field.Type)
			{
				case StringType.ASCII:
					_writer.WriteAscii(field.Value);
					break;

				case StringType.UTF16:
					_writer.WriteUTF16(field.Value);
					break;
			}
		}

		public void VisitStringID(StringIDData field)
		{
			SeekToOffset(field.Offset);
			if (_stringIdTrie.Contains(field.Value))
			{
				StringID sid = _cache.StringIDs.FindStringID(field.Value);
				_writer.WriteUInt32(sid.Value);
			}
			else if (_type == SaveType.File)
			{
				StringID sid = _cache.StringIDs.AddString(field.Value);
				_stringIdTrie.Add(field.Value);
				_writer.WriteUInt32(sid.Value);
			}
			else
			{
				_writer.WriteUInt32(StringID.Null.Value);
			}
		}

		public void VisitRawData(RawData field)
		{
			SeekToOffset(field.Offset);
			_writer.WriteBlock(FunctionHelpers.HexStringToBytes(field.Value), 0, field.Length);
		}

		public void VisitDataRef(DataRef field)
		{
			var values = new StructureValueCollection();
			values.SetInteger("size", (uint) field.Length);
			values.SetInteger("pointer", field.DataAddress);

			SeekToOffset(field.Offset);
			StructureWriter.WriteStructure(values, _dataRefLayout, _writer);

			if (field.DataAddress == 0xFFFFFFFF || field.DataAddress <= 0) return;

			// Go to the data location
			uint offset = field.DataAddress;
			if (_type == SaveType.File)
				offset = (uint) _cache.MetaArea.PointerToOffset(offset);
			_writer.SeekTo(offset);

			// Write its data
			switch (field.Format)
			{
				default:
					_writer.WriteBlock(FunctionHelpers.HexStringToBytes(field.Value), 0, field.Length);
					break;
				case "unicode":
					_writer.WriteUTF16(field.Value);
					break;
				case "asciiz":
					_writer.WriteAscii(field.Value);
					break;
			}
		}

		public void VisitTagRef(TagRefData field)
		{
			SeekToOffset(field.Offset);

			if (field.WithClass)
			{
				var values = new StructureValueCollection();
				if (field.Value != null)
				{
					values.SetInteger("class magic", (uint)field.Value.Tag.Class.Magic);
					values.SetInteger("datum index", field.Value.Tag.Index.Value);
				}
				else
				{
					values.SetInteger("class magic", 0xFFFFFFFF);
					values.SetInteger("datum index", 0xFFFFFFFF);
				}
				StructureWriter.WriteStructure(values, _tagRefLayout, _writer);
			}
			else
			{
				_writer.WriteUInt32(field.Value == null ? 0xFFFFFFFF : field.Value.Tag.Index.Value);
			}
		}

		public void VisitVector(VectorData field)
		{
			SeekToOffset(field.Offset);
			_writer.WriteFloat(field.X);
			_writer.WriteFloat(field.Y);
			_writer.WriteFloat(field.Z);
		}

		public void VisitDegree(DegreeData field)
		{
			SeekToOffset(field.Offset);
			_writer.WriteFloat(field.Radian);
		}

		public void VisitShaderRef(ShaderRef field)
		{
			// Don't do anything
		}

		public void WriteFields(IList<TagDataField> fields)
		{
			foreach (var t in fields)
				WriteField(t);
		}

		private void WriteField(TagDataField field)
		{
			if (_changes == null || _changes.HasChanged(field))
				field.Accept(this);

			var tagBlock = field as TagBlockData;
			if (tagBlock != null)
				WriteTagBlockChildren(tagBlock);
		}

		public void WriteTagBlockChildren(TagBlockData field)
		{
			if (field.CurrentIndex < 0 || !field.HasChildren)
				return;

			// Get the base address and convert it to an offset if we're writing to the file
			var newBaseOffset = field.FirstEntryAddress;
			if (_type == SaveType.File)
				newBaseOffset = (uint) _cache.MetaArea.PointerToOffset(newBaseOffset);

			// Save the old base offset and set the base offset to the reflexive's base
			var oldBaseOffset = _baseOffset;
			_baseOffset = newBaseOffset;

			// Write each page
			var oldIndex = field.CurrentIndex;
			var oldPokeTemplates = _pokeTemplateFields;
			for (var i = 0; i < field.Length; i++)
			{
				// If we're saving everything, then change the active page so the values get loaded from the file
				if (_changes == null && field.CurrentIndex != i)
					field.CurrentIndex = i;

				// If we're not saving everything, then we can only poke template fields in tag blocks
				// if the current indices all line up
				if (i != oldIndex)
					_pokeTemplateFields = false;

				// Get each field in the page and write it
				var page = field.Pages[i];
				for (var j = 0; j < page.Fields.Length; j++)
				{
					var pageField = page.Fields[j];
					// The field in the page takes precedence over the field in the reflexive's template
					if (pageField == null && (_changes == null || _pokeTemplateFields))
						pageField = field.Template[j]; // Get it from the template
					if (pageField != null)
						WriteField(pageField);
				}

				// Advance to the next chunk
				_baseOffset += field.EntrySize;
				_pokeTemplateFields = oldPokeTemplates;
			}
			if (!Equals(field.CurrentIndex, oldIndex))
				field.CurrentIndex = oldIndex;

			// Restore the old base offset
			_baseOffset = oldBaseOffset;
		}

		private void SeekToOffset(uint offset)
		{
			_writer.SeekTo(_baseOffset + offset);
		}
	}
}