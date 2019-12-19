using System.Collections.Generic;
using System.Linq;
using Blamite.Blam;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Util;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public class MetaReader : IMetaFieldVisitor
	{
		public enum LoadType
		{
			File,
			Memory
		}

		private readonly ICacheFile _cache;
		private readonly StructureLayout _dataRefLayout;
		private readonly FieldChangeSet _ignoredFields;
		private readonly StructureLayout _tagBlockLayout;
		private readonly IStreamManager _streamManager;
		private readonly StructureLayout _tagRefLayout;
		private readonly LoadType _type;
		private IReader _reader;

		public MetaReader(IStreamManager streamManager, long baseOffset, ICacheFile cache, EngineDescription buildInfo,
			LoadType type, FieldChangeSet ignore)
		{
			_streamManager = streamManager;
			BaseOffset = baseOffset;
			_cache = cache;
			_ignoredFields = ignore;
			_type = type;

			// Load layouts
			_tagBlockLayout = buildInfo.Layouts.GetLayout("tag block");
			_tagRefLayout = buildInfo.Layouts.GetLayout("tag reference");
			_dataRefLayout = buildInfo.Layouts.GetLayout("data reference");
		}

		public long BaseOffset { get; set; }

		public void VisitBitfield(BitfieldData field)
		{
			SeekToOffset(field.Offset);
			switch (field.Type)
			{
				case BitfieldType.Bitfield8:
					field.Value = _reader.ReadByte();
					break;
				case BitfieldType.Bitfield16:
					field.Value = _reader.ReadUInt16();
					break;
				case BitfieldType.Bitfield32:
					field.Value = _reader.ReadUInt32();
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
					field.Value = _reader.ReadSByte();
					break;
				case EnumType.Enum16:
					field.Value = _reader.ReadInt16();
					break;
				case EnumType.Enum32:
					field.Value = _reader.ReadInt32();
					break;
			}

			// Search for the corresponding option and select it
			EnumValue selected = null;
			foreach (EnumValue option in field.Values)
			{
				// Typecast the field value and the option value based upon the enum type
				switch (field.Type)
				{
					case EnumType.Enum8:
						if ((sbyte) option.Value == (sbyte) field.Value)
							selected = option;
						break;
					case EnumType.Enum16:
						if ((short) option.Value == (short) field.Value)
							selected = option;
						break;
					case EnumType.Enum32:
						if (option.Value == field.Value)
							selected = option;
						break;
				}
				if (selected != null)
					break;
			}
			if (selected == null)
			{
				// Nothing matched, so just add an option for it
				selected = new EnumValue(field.Value.ToString(), field.Value);
				field.Values.Add(selected);
			}
			field.SelectedValue = selected;
		}

		public void VisitUint8(Uint8Data field)
		{
			SeekToOffset(field.Offset);
			field.Value = _reader.ReadByte();
		}

		public void VisitInt8(Int8Data field)
		{
			SeekToOffset(field.Offset);
			field.Value = _reader.ReadSByte();
		}

		public void VisitUint16(Uint16Data field)
		{
			SeekToOffset(field.Offset);
			field.Value = _reader.ReadUInt16();
		}

		public void VisitInt16(Int16Data field)
		{
			SeekToOffset(field.Offset);
			field.Value = _reader.ReadInt16();
		}

		public void VisitUint32(Uint32Data field)
		{
			SeekToOffset(field.Offset);
			field.Value = _reader.ReadUInt32();
		}

		public void VisitInt32(Int32Data field)
		{
			SeekToOffset(field.Offset);
			field.Value = _reader.ReadInt32();
		}

		public void VisitColourInt(ColourData field)
		{
			SeekToOffset(field.Offset);

			string colorValue = "#";
			foreach (char formatChar in field.Format)
				colorValue += (_reader.ReadByte().ToString("X2"));
			field.Value = colorValue;
		}

		public void VisitColourFloat(ColourData field)
		{
			SeekToOffset(field.Offset);

			string colorValue = "#";
			foreach (char formatChar in field.Format)
				colorValue += ((int) (_reader.ReadFloat()*255)).ToString("X2");
			field.Value = colorValue;
		}

		public void VisitString(StringData field)
		{
			SeekToOffset(field.Offset);
			switch (field.Type)
			{
				case StringType.ASCII:
					field.Value = _reader.ReadAscii(field.Size);
					break;

				case StringType.UTF16:
					field.Value = _reader.ReadUTF16(field.Size);
					break;
			}
		}

		public void VisitStringID(StringIDData field)
		{
			SeekToOffset(field.Offset);
			field.Value = _cache.StringIDs.GetString(new StringID(_reader.ReadUInt32()));
		}

		public void VisitRawData(RawData field)
		{
			SeekToOffset(field.Offset);
			field.DataAddress = field.FieldAddress;
			field.Value = FunctionHelpers.BytesToHexString(_reader.ReadBlock(field.Length));
		}

		public void VisitDataRef(DataRef field)
		{
			SeekToOffset(field.Offset);
			StructureValueCollection values = StructureReader.ReadStructure(_reader, _dataRefLayout);

			var length = (int) values.GetInteger("size");
			uint pointer = (uint)values.GetInteger("pointer");

			long expanded = _cache.PointerExpander.Expand(pointer);

			if (length > 0 && _cache.MetaArea.ContainsBlockPointer(expanded, length))
			{
				field.DataAddress = expanded;
				field.Length = length;

				// Go to position
				if (_type == LoadType.Memory)
					_reader.SeekTo(expanded);
				else
					_reader.SeekTo(_cache.MetaArea.PointerToOffset(expanded));

				switch (field.Format)
				{
					default:
						byte[] data = _reader.ReadBlock(field.Length);
						field.Value = FunctionHelpers.BytesToHexString(data);
						break;
					case "unicode":
						field.Value = _reader.ReadUTF16(field.Length);
						break;
					case "asciiz":
						field.Value = _reader.ReadAscii(field.Length);
						break;
				}
			}
			else
			{
				field.DataAddress = 0;
				field.Length = 0;
				field.Value = "";
			}
		}

		public void VisitTagRef(TagRefData field)
		{
			SeekToOffset(field.Offset);

			TagClass tagClass = null;
			DatumIndex index;
			if (field.WithClass)
			{
				// Read the datum index based upon the layout
				StructureValueCollection values = StructureReader.ReadStructure(_reader, _tagRefLayout);
				index = new DatumIndex(values.GetInteger("datum index"));

				// Check the class, in case the datum index is null
				var magic = values.GetInteger("class magic");
				var str = CharConstant.ToString((int)magic);
				tagClass = field.Tags.Classes.FirstOrDefault(c => c.TagClassMagic == str);
			}
			else
			{
				// Just read the datum index at the current position
				index = DatumIndex.ReadFrom(_reader);
			}

			TagEntry tag = null;
			if (index.IsValid && index.Index < field.Tags.Entries.Count)
			{
				tag = field.Tags.Entries[index.Index];
				if (tag == null || tag.RawTag == null || tag.RawTag.Index != index)
					tag = null;
			}

			if (tag != null)
			{
				field.Class = field.Tags.Classes.FirstOrDefault(c => c.RawClass == tag.RawTag.Class);
				field.Value = tag;
			}
			else
			{
				field.Class = tagClass;
				field.Value = null;
			}
		}

		public void VisitFloat32(Float32Data field)
		{
			SeekToOffset(field.Offset);
			field.Value = _reader.ReadFloat();
		}

		public void VisitPoint2(Vector2Data field)
		{
			SeekToOffset(field.Offset);
			field.A = _reader.ReadFloat();
			field.B = _reader.ReadFloat();
		}

		public void VisitPoint3(Vector3Data field)
		{
			SeekToOffset(field.Offset);
			field.A = _reader.ReadFloat();
			field.B = _reader.ReadFloat();
			field.C = _reader.ReadFloat();
		}

		public void VisitVector2(Vector2Data field)
		{
			SeekToOffset(field.Offset);
			field.A = _reader.ReadFloat();
			field.B = _reader.ReadFloat();
		}

		public void VisitVector3(Vector3Data field)
		{
			SeekToOffset(field.Offset);
			field.A = _reader.ReadFloat();
			field.B = _reader.ReadFloat();
			field.C = _reader.ReadFloat();
		}

		public void VisitVector4(Vector4Data field)
		{
			SeekToOffset(field.Offset);
			field.A = _reader.ReadFloat();
			field.B = _reader.ReadFloat();
			field.C = _reader.ReadFloat();
			field.D = _reader.ReadFloat();
		}

		public void VisitDegree(DegreeData field)
		{
			SeekToOffset(field.Offset);
			field.Radian = _reader.ReadFloat();
		}

		public void VisitDegree2(Degree2Data field)
		{
			SeekToOffset(field.Offset);
			field.RadianA = _reader.ReadFloat();
			field.RadianB = _reader.ReadFloat();
		}

		public void VisitDegree3(Degree3Data field)
		{
			SeekToOffset(field.Offset);
			field.RadianA = _reader.ReadFloat();
			field.RadianB = _reader.ReadFloat();
			field.RadianC = _reader.ReadFloat();
		}

		public void VisitPlane2(Vector3Data field)
		{
			SeekToOffset(field.Offset);
			field.A = _reader.ReadFloat();
			field.B = _reader.ReadFloat();
			field.C = _reader.ReadFloat();
		}

		public void VisitPlane3(Vector4Data field)
		{
			SeekToOffset(field.Offset);
			field.A = _reader.ReadFloat();
			field.B = _reader.ReadFloat();
			field.C = _reader.ReadFloat();
			field.D = _reader.ReadFloat();
		}

		public void VisitRect16(RectangleData field)
		{
			SeekToOffset(field.Offset);
			field.A = _reader.ReadInt16();
			field.B = _reader.ReadInt16();
			field.C = _reader.ReadInt16();
			field.D = _reader.ReadInt16();
		}

		public void VisitReflexive(ReflexiveData field)
		{
			SeekToOffset(field.Offset);
			StructureValueCollection values = StructureReader.ReadStructure(_reader, _tagBlockLayout);
			var length = (int) values.GetInteger("entry count");
			uint pointer = (uint)values.GetInteger("pointer");

			long expanded = _cache.PointerExpander.Expand(pointer);

			// Make sure the pointer looks valid
			if (length < 0 || !_cache.MetaArea.ContainsBlockPointer(expanded, (int) (length*field.EntrySize)))
			{
				length = 0;
				pointer = 0;
				expanded = 0;
			}

			if (expanded != field.FirstEntryAddress)
				field.FirstEntryAddress = expanded;

			field.Length = length;
			
		}

		public void VisitShaderRef(ShaderRef field)
		{
			SeekToOffset(field.Offset);
			if (_cache.ShaderStreamer != null)
				field.Shader = _cache.ShaderStreamer.ReadShader(_reader, field.Type);
		}

		public void VisitRangeUint16(RangeUint16Data field)
		{
			SeekToOffset(field.Offset);
			field.A = _reader.ReadUInt16();
			field.B = _reader.ReadUInt16();
		}

		public void VisitRangeFloat32(RangeFloat32Data field)
		{
			SeekToOffset(field.Offset);
			field.A = _reader.ReadFloat();
			field.B = _reader.ReadFloat();
		}

		public void VisitRangeDegree(RangeDegreeData field)
		{
			SeekToOffset(field.Offset);
			field.RadianA = _reader.ReadFloat();
			field.RadianB = _reader.ReadFloat();
		}

		public void VisitReflexiveEntry(WrappedReflexiveEntry field)
		{
		}

		private void ReadField(MetaField field)
		{
			// Update the field's memory address
			var valueField = field as ValueField;
			if (valueField != null)
			{
				valueField.FieldAddress = BaseOffset + valueField.Offset;
				if (_type == LoadType.File)
					valueField.FieldAddress = _cache.MetaArea.OffsetToPointer((int)valueField.FieldAddress);
			}

			// Read its contents if it hasn't changed (or if change detection is disabled)
			if (_ignoredFields == null || !_ignoredFields.HasChanged(field))
				field.Accept(this);

			// If it's a reflexive, read its children
			var reflexive = field as ReflexiveData;
			if (reflexive != null)
				ReadReflexiveChildren(reflexive);
		}

		public void ReadFields(IList<MetaField> fields)
		{
			bool opened = OpenReader();
			if (_reader == null)
				return;

			try
			{
// ReSharper disable ForCanBeConvertedToForeach
				for (int i = 0; i < fields.Count; i++)
					ReadField(fields[i]);
// ReSharper restore ForCanBeConvertedToForeach
			}
			finally
			{
				if (opened)
					CloseReader();
			}
		}

		public void ReadReflexiveChildren(ReflexiveData reflexive)
		{
			if (!reflexive.HasChildren || reflexive.CurrentIndex < 0)
				return;

			bool opened = OpenReader();
			if (_reader == null)
				return;

			try
			{
				// Calculate the base offset to read from
				long oldBaseOffset = BaseOffset;
				long dataOffset = reflexive.FirstEntryAddress;
				if (_type == LoadType.File)
					dataOffset = (uint) _cache.MetaArea.PointerToOffset(dataOffset);
				BaseOffset = (dataOffset + reflexive.CurrentIndex*reflexive.EntrySize);

				ReflexivePage page = reflexive.Pages[reflexive.CurrentIndex];
				for (int i = 0; i < page.Fields.Length; i++)
				{
					ReadField(page.Fields[i] ?? reflexive.Template[i]);
				}

				BaseOffset = oldBaseOffset;
			}
			finally
			{
				if (opened)
					CloseReader();
			}
		}

		/// <summary>
		///     Opens the file for reading and sets _reader to the stream. Must be done before any I/O operations are performed.
		/// </summary>
		/// <returns>false if the file was already open.</returns>
		private bool OpenReader()
		{
			if (_reader == null)
			{
				_reader = _streamManager.OpenRead();
				return true;
			}
			return false;
		}

		private void CloseReader()
		{
			if (_reader != null)
			{
				_reader.Dispose();
				_reader = null;
			}
		}

		private void SeekToOffset(uint offset)
		{
			_reader.SeekTo(BaseOffset + offset);
		}
	}
}