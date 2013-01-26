using Assembly.Helpers;
using ExtryzeDLL.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Util;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    class MetaWriter : IMetaFieldVisitor
    {
        private IWriter _writer;
        private uint _baseOffset;
        private ICacheFile _cache;
        private SaveType _type;
        private FieldChangeSet _changes;

        private bool _pokeTemplateFields = true;

        public enum SaveType { File, Memory }

        /// <summary>
        /// Save meta to the Blam Cache File
        /// </summary>
        public MetaWriter(IWriter writer, uint baseOffset, ICacheFile cache, SaveType type, FieldChangeSet changes)
        {
            _writer = writer;
            _baseOffset = baseOffset;
            _cache = cache;
            _type = type;
            _changes = changes;
        }

        public void WriteFields(IEnumerable<MetaField> fields)
        {
            foreach (MetaField field in fields)
                WriteField(field);
        }

        private void WriteField(MetaField field)
        {
            if (_changes == null || _changes.HasChanged(field))
                field.Accept(this);

            ReflexiveData reflexive = field as ReflexiveData;
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

        public void VisitReflexive(ReflexiveData field)
        {
            SeekToOffset(field.Offset);
            _writer.WriteInt32(field.Length);
            _writer.WriteUInt32(field.FirstEntryAddress);
        }

        public void WriteReflexiveChildren(ReflexiveData field)
        {
            if (field.CurrentIndex < 0 || !field.HasChildren)
                return;

            // Get the base address and convert it to an offset if we're writing to the file
            uint newBaseOffset = field.FirstEntryAddress;
            if (_type == SaveType.File)
                newBaseOffset = _cache.MetaPointerConverter.AddressToOffset(newBaseOffset);

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
                    MetaField pageField = page.Fields[j]; // The field in the page takes precedence over the field in the reflexive's template
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
            _writer.WriteAscii(field.Value);
        }

        public void VisitStringID(StringIDData field)
        {
            SeekToOffset(field.Offset);
            _writer.WriteInt32(_cache.StringIDs.IndexToStringID(field.Value).Value);
        }

        public void VisitRawData(RawData field)
        {
            SeekToOffset(field.Offset);
            _writer.WriteBlock(FunctionHelpers.HexStringToBytes(field.Value), 0, field.Length);
        }

        public void VisitDataRef(DataRef field)
        {
            SeekToOffset(field.Offset);
            _writer.WriteInt32(field.Length); // Data size
            _writer.Skip(2 * 4); // Skip two unused values
            _writer.WriteUInt32(field.Address); // Address

            if (field.Address != 0xFFFFFFFF && field.Address > 0)
            {
                // Go to the data location
                uint offset = field.Address;
                if (_type == SaveType.File)
                    offset = _cache.MetaPointerConverter.AddressToOffset(offset);
                _writer.SeekTo(offset);

                // Write its data
                _writer.WriteBlock(FunctionHelpers.HexStringToBytes(field.Value), 0, field.Length);
            }
        }

        public void VisitTagRef(TagRefData field)
        {
            SeekToOffset(field.Offset);

            if (field.WithClass)
            {
                if (field.Value == null)
                    _writer.WriteInt32(-1);
                else
                    _writer.WriteInt32(field.Value.RawTag.Class.Magic); // Class magic
                _writer.Skip(2 * 4); // Skip two unused values
            }

            if (field.Value == null)
                _writer.WriteUInt32(0xFFFFFFFF);
            else
                _writer.WriteUInt32(field.Value.RawTag.Index.Value); // Tag datum
        }

        public void VisitVector(VectorData field)
        {
            SeekToOffset(field.Offset);
            _writer.WriteFloat(field.X);
            _writer.WriteFloat(field.Y);
            _writer.WriteFloat(field.Z);
        }

        private void SeekToOffset(uint offset)
        {
            _writer.SeekTo(_baseOffset + offset);
        }
    }
}