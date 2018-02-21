﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Blamite.Blam;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Util;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	internal class MetaWriter : IMetaFieldVisitor
	{
		public enum SaveType
		{
			File,
			Memory
		}

		private readonly ICacheFile _cache;
		private readonly FieldChangeSet _changes;
		private readonly StructureLayout _dataRefLayout;
		private readonly StructureLayout _reflexiveLayout;
		private readonly Trie _stringIdTrie;
		private readonly StructureLayout _tagRefLayout;
		private readonly SaveType _type;
		private readonly IWriter _writer;
		private uint _baseOffset;
		private uint _headerOffset;


		private bool _pokeTemplateFields = true;

		/// <summary>
		///     Save meta to the Blam Cache File
		/// </summary>
		public MetaWriter(IWriter writer, uint headerOffset, uint baseOffset, ICacheFile cache, EngineDescription buildInfo, SaveType type,
			FieldChangeSet changes, Trie stringIdTrie)
 		{
			_writer = writer;
			_headerOffset = headerOffset;
			_baseOffset = baseOffset;
			_cache = cache;
			_type = type;
			_changes = changes;
			_stringIdTrie = stringIdTrie;

			// Load layouts
			_reflexiveLayout = buildInfo.Layouts.GetLayout("tag block");
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

			bool eldorado = _cache.Engine == EngineType.FourthGeneration;
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
						byte red = !eldorado ? byte.Parse(field.Value.Replace("#", "").Remove(0, 2).Remove(2), NumberStyles.HexNumber)
							: byte.Parse(field.Value.Replace("#", "").Remove(0, 6), NumberStyles.HexNumber);
						_writer.WriteByte(red);
						break;
					case 'g':
						byte green = !eldorado ? byte.Parse(field.Value.Replace("#", "").Remove(0, 4).Remove(2), NumberStyles.HexNumber)
							: byte.Parse(field.Value.Replace("#", "").Remove(0, 4).Remove(2), NumberStyles.HexNumber);
						_writer.WriteByte(green);
						break;
					case 'b':
						byte blue = !eldorado ? byte.Parse(field.Value.Replace("#", "").Remove(0, 6), NumberStyles.HexNumber)
							: byte.Parse(field.Value.Replace("#", "").Remove(0, 2).Remove(2), NumberStyles.HexNumber);
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

		public void VisitReflexive(ReflexiveData field)
		{
			var values = new StructureValueCollection();
			values.SetInteger("entry count", (uint) field.Length);
			values.SetInteger("pointer", field.FirstEntryAddress);

			SeekToOffset(field.Offset);
			StructureWriter.WriteStructure(values, _reflexiveLayout, _writer);
		}

		public void VisitReflexiveEntry(WrappedReflexiveEntry field)
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

			SeekToOffset(field.Offset);
			StructureWriter.WriteStructure(values, _dataRefLayout, _writer);

			if (field.DataAddress == 0xFFFFFFFF || field.DataAddress <= 0) return;

			// Go to the data location
			uint offset = field.DataAddress;
			uint dataOffset = offset;

			switch (_type)
			{
				case SaveType.Memory:
					{
						if (_cache.GetType() != typeof(Blamite.Blam.FourthGen.FourthGenCacheFile))
							values.SetInteger("pointer", offset);
						break;
					}
				case SaveType.File:
					{
						if (_cache.GetType() == typeof(Blamite.Blam.FourthGen.FourthGenCacheFile))
							offset = offset - _headerOffset + 0x40000000;
						values.SetInteger("pointer", offset);
						dataOffset = (uint)_cache.MetaArea.PointerToOffset(dataOffset);
						break;
					}
			}
			_writer.SeekTo(dataOffset);

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
					//hax
					if (field.Value.RawTag == null)
					{
						values.SetInteger("class magic", (uint)field.Class.RawClass.Magic);
						values.SetInteger("datum index", 0xFFFFFFFF);
					}
					else
					{
						values.SetInteger("class magic", (uint)field.Value.RawTag.Class.Magic);
						values.SetInteger("datum index", field.Value.RawTag.Index.Value);
					}
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
				_writer.WriteUInt32(field.Value == null ? 0xFFFFFFFF : field.Value.RawTag.Index.Value);
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

		public void WriteFields(IList<MetaField> fields)
		{
			foreach (MetaField t in fields)
				WriteField(t);
		}

		private void WriteField(MetaField field)
		{
			if (_changes == null || _changes.HasChanged(field))
				field.Accept(this);

			var reflexive = field as ReflexiveData;
			if (reflexive != null)
				WriteReflexiveChildren(reflexive);
		}

		public void WriteReflexiveChildren(ReflexiveData field)
		{
			if (field.CurrentIndex < 0 || !field.HasChildren)
				return;

			// Get the base address and convert it to an offset if we're writing to the file
			uint newBaseOffset = field.FirstEntryAddress;
			if (_cache.GetType() == typeof(Blamite.Blam.FourthGen.FourthGenCacheFile))
				newBaseOffset = _headerOffset + (newBaseOffset & 0xFFFFFFF);

			if (_type == SaveType.File)
				newBaseOffset = (uint) _cache.MetaArea.PointerToOffset(newBaseOffset);

			// Save the old base offset and set the base offset to the reflexive's base
			uint oldBaseOffset = _baseOffset;
			_baseOffset = newBaseOffset;

			// Write each page
			int _oldIndex = field.CurrentIndex;
			bool _oldPokeTemplates = _pokeTemplateFields;
			for (int i = 0; i < field.Length; i++)
			{
				// If we're saving everything, then change the active page so the values get loaded from the file
				if (_changes == null && field.CurrentIndex != i)
					field.CurrentIndex = i;

				// If we're not saving everything, then we can only poke template fields in reflexives
				// if the current indices all line up
				if (i != _oldIndex)
					_pokeTemplateFields = false;

				// Get each field in the page and write it
				ReflexivePage page = field.Pages[i];
				for (int j = 0; j < page.Fields.Length; j++)
				{
					MetaField pageField = page.Fields[j];
					// The field in the page takes precedence over the field in the reflexive's template
					if (pageField == null && (_changes == null || _pokeTemplateFields))
						pageField = field.Template[j]; // Get it from the template
					if (pageField != null)
						WriteField(pageField);
				}

				// Advance to the next chunk
				_baseOffset += field.EntrySize;
				_pokeTemplateFields = _oldPokeTemplates;
			}
			if (!Equals(field.CurrentIndex, _oldIndex))
				field.CurrentIndex = _oldIndex;

			// Restore the old base offset
			_baseOffset = oldBaseOffset;
		}

		private void SeekToOffset(uint offset)
		{
			_writer.SeekTo(_baseOffset + offset);
		}
	}
}