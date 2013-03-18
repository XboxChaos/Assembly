using System;
using System.Globalization;
using Blamite.IO;
using System.Collections.Generic;
using Blamite.Blam;
using Blamite.Util;
using Blamite.Flexibility;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    class MetaWriter : IMetaFieldVisitor
    {
        private IWriter _writer;
        private uint _baseOffset;
        private ICacheFile _cache;
        private SaveType _type;
        private FieldChangeSet _changes;
        private StructureLayout _reflexiveLayout;
        private StructureLayout _tagRefLayout;
        private StructureLayout _dataRefLayout;

        private bool _pokeTemplateFields = true;

        public enum SaveType { File, Memory }

        /// <summary>
        /// Save meta to the Blam Cache File
        /// </summary>
        public MetaWriter(IWriter writer, uint baseOffset, ICacheFile cache, BuildInformation buildInfo, SaveType type, FieldChangeSet changes)
        {
            _writer = writer;
            _baseOffset = baseOffset;
            _cache = cache;
            _type = type;
            _changes = changes;

            // Load layouts
            _reflexiveLayout = buildInfo.GetLayout("reflexive");
            _tagRefLayout = buildInfo.GetLayout("tag reference");
            _dataRefLayout = buildInfo.GetLayout("data reference");
        }

        public void WriteFields(IList<MetaField> fields)
        {
	        foreach (var t in fields)
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

        public void VisitBitfield(BitfieldData field)
        {
            SeekToOffset(field.Offset);
            switch (field.Type)
            {
                case BitfieldType.Bitfield8:
                    _writer.WriteByte((byte)field.Value);
                    break;

                case BitfieldType.Bitfield16:
                    _writer.WriteUInt16((ushort)field.Value);
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
                    _writer.WriteSByte((sbyte)field.Value);
                    break;

                case EnumType.Enum16:
                    _writer.WriteInt16((short)field.Value);
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

			foreach (var formatChar in field.Format.ToCharArray())
			{
				switch (formatChar)
				{
					case 'a':
						var alpha = byte.Parse(field.Value.Replace("#", "").Remove(2), NumberStyles.HexNumber);
						_writer.WriteByte(alpha);
						break;
					case 'r':
						var red = byte.Parse(field.Value.Replace("#", "").Remove(0, 2).Remove(4), NumberStyles.HexNumber);
						_writer.WriteByte(red);
						break;
					case 'g':
						var green = byte.Parse(field.Value.Replace("#", "").Remove(0, 4).Remove(6), NumberStyles.HexNumber);
						_writer.WriteByte(green);
						break;
					case 'b':
						var blue = byte.Parse(field.Value.Replace("#", "").Remove(0, 6), NumberStyles.HexNumber);
						_writer.WriteByte(blue);
						break;
				}
			}
		}
		public void VisitColourFloat(ColourData field)
		{
			SeekToOffset(field.Offset);

			foreach(var formatChar in field.Format.ToCharArray())
			{
				switch(formatChar)
				{
					case 'a':
						var alpha = Convert.ToSingle(int.Parse(field.Value.Replace("#", "").Remove(2), NumberStyles.HexNumber)) / 255;
						_writer.WriteFloat(alpha);
						break;
					case 'r':
						var red = Convert.ToSingle(int.Parse(field.Value.Replace("#", "").Remove(0, 2).Remove(2), NumberStyles.HexNumber)) / 255;
						_writer.WriteFloat(red);
						break;
					case 'g':
						var green = Convert.ToSingle(int.Parse(field.Value.Replace("#", "").Remove(0, 4).Remove(2), NumberStyles.HexNumber)) / 255;
						_writer.WriteFloat(green);
						break;
					case 'b':
						var blue = Convert.ToSingle(int.Parse(field.Value.Replace("#", "").Remove(0, 6), NumberStyles.HexNumber)) / 255;
						_writer.WriteFloat(blue);
						break;
				}
			}
		}

        public void VisitReflexive(ReflexiveData field)
        {
            var values = new StructureValueCollection();
            values.SetInteger("entry count", (uint)field.Length);
            values.SetInteger("pointer", field.FirstEntryAddress);

            SeekToOffset(field.Offset);
            StructureWriter.WriteStructure(values, _reflexiveLayout, _writer);
        }

        public void WriteReflexiveChildren(ReflexiveData field)
        {
            if (field.CurrentIndex < 0 || !field.HasChildren)
                return;

            // Get the base address and convert it to an offset if we're writing to the file
            var newBaseOffset = field.FirstEntryAddress;
            if (_type == SaveType.File)
                newBaseOffset = (uint)_cache.MetaArea.PointerToOffset(newBaseOffset);

            // Save the old base offset and set the base offset to the reflexive's base
			var oldBaseOffset = _baseOffset;
            _baseOffset = newBaseOffset;

            // Write each page
			var _oldIndex = field.CurrentIndex;
			var _oldPokeTemplates = _pokeTemplateFields;
			for (var i = 0; i < field.Length; i++)
            {
                // If we're saving everything, then change the active page so the values get loaded from the file
                if (_changes == null && field.CurrentIndex != i)
                    field.CurrentIndex = i;

                // If we're not saving everything, then we can only poke template fields in reflexives
                // if the current indices all line up
                if (i != _oldIndex)
                    _pokeTemplateFields = false;

                // Get each field in the page and write it
				var page = field.Pages[i];
				for (var j = 0; j < page.Fields.Length; j++)
                {
					var pageField = page.Fields[j]; // The field in the page takes precedence over the field in the reflexive's template
                    if (pageField == null && (_changes == null || _pokeTemplateFields))
                        pageField = field.Template[j]; // Get it from the template
                    if (pageField != null)
                        WriteField(pageField);
                }

                // Advance to the next chunk
                _baseOffset += field.EntrySize;
                _pokeTemplateFields = _oldPokeTemplates;
            }
            if (field.CurrentIndex != _oldIndex)
                field.CurrentIndex = _oldIndex;

            // Restore the old base offset
            _baseOffset = oldBaseOffset;
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
            _writer.WriteUInt32(field.Value.Value);
        }

        public void VisitRawData(RawData field)
        {
            SeekToOffset(field.Offset);
            _writer.WriteBlock(FunctionHelpers.HexStringToBytes(field.Value), 0, field.Length);
        }

        public void VisitDataRef(DataRef field)
        {
			var values = new StructureValueCollection();
            values.SetInteger("size", (uint)field.Length);
            values.SetInteger("pointer", field.DataAddress);

            SeekToOffset(field.Offset);
            StructureWriter.WriteStructure(values, _dataRefLayout, _writer);

	        if (field.DataAddress == 0xFFFFFFFF || field.DataAddress <= 0) return;

	        // Go to the data location
			var offset = field.DataAddress;
	        if (_type == SaveType.File)
		        offset = (uint)_cache.MetaArea.PointerToOffset(offset);
	        _writer.SeekTo(offset);

	        // Write its data
	        _writer.WriteBlock(FunctionHelpers.HexStringToBytes(field.Value), 0, field.Length);
        }

        public void VisitTagRef(TagRefData field)
        {
            SeekToOffset(field.Offset);

            if (field.WithClass)
            {
                var values = new StructureValueCollection();
                if (field.Value != null)
                {
                    values.SetInteger("class magic", (uint)field.Value.RawTag.Class.Magic);
                    values.SetInteger("datum index", field.Value.RawTag.Index.Value);
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

	    private void SeekToOffset(uint offset)
        {
            _writer.SeekTo(_baseOffset + offset);
        }
	}
}