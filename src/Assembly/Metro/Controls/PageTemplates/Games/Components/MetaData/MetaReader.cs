using System;
using System.Collections.Generic;
using ExtryzeDLL.Blam;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.IO;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.Util;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public class MetaReader : IMetaFieldVisitor
    {
        private IStreamManager _streamManager;
        private IReader _reader;
        private uint _baseOffset = 0;
        private ICacheFile _cache;
        private StructureLayout _reflexiveLayout;
        private StructureLayout _tagRefLayout;
        private StructureLayout _dataRefLayout;
        private FieldChangeSet _ignoredFields;

        public MetaReader(IStreamManager streamManager, uint baseOffset, ICacheFile cache, BuildInformation buildInfo)
            : this(streamManager, baseOffset, cache, buildInfo, null)
        {
        }

        public MetaReader(IStreamManager streamManager, uint baseOffset, ICacheFile cache, BuildInformation buildInfo, FieldChangeSet ignore)
        {
            _streamManager = streamManager;
            _baseOffset = baseOffset;
            _cache = cache;
            _ignoredFields = ignore;

            // Load layouts
            _reflexiveLayout = buildInfo.GetLayout("reflexive");
            _tagRefLayout = buildInfo.GetLayout("tag reference");
            _dataRefLayout = buildInfo.GetLayout("data reference");
        }

        private void ReadField(MetaField field)
        {
            // Update the field's memory address
            ValueField valueField = field as ValueField;
            if (valueField != null)
                valueField.FieldAddress = _cache.MetaArea.OffsetToPointer((int)(_baseOffset + valueField.Offset));

            // Read its contents if it has changed (or if change detection is disabled)
            if (_ignoredFields == null || !_ignoredFields.HasChanged(field))
                field.Accept(this);

            // If it's a reflexive, read its children
            ReflexiveData reflexive = field as ReflexiveData;
            if (reflexive != null)
                ReadReflexiveChildren(reflexive);
        }

        public void ReadFields(IList<MetaField> fields)
        {
            bool opened = OpenReader();
            try
            {
                for (int i = 0; i < fields.Count; i++)
                    ReadField(fields[i]);
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
            try
            {
                // Calculate the base offset to read from
                uint oldBaseOffset = _baseOffset;
                int dataOffset = _cache.MetaArea.PointerToOffset(reflexive.FirstEntryAddress);
                _baseOffset = (uint)(dataOffset + reflexive.CurrentIndex * reflexive.EntrySize);

                ReflexivePage page = reflexive.Pages[reflexive.CurrentIndex];
                for (int i = 0; i < page.Fields.Length; i++)
                {
                    if (page.Fields[i] != null)
                        ReadField(page.Fields[i]);
                    else
                        ReadField(reflexive.Template[i]);
                }

                _baseOffset = oldBaseOffset;
            }
            finally
            {
                if (opened)
                    CloseReader();
            }
        }

        public void VisitBitfield(BitfieldData field)
        {
            SeekToOffset(field.Offset);
            switch (field.Type)
            {
                case BitfieldType.Bitfield8:
                    field.Value = (uint)_reader.ReadByte();
                    break;
                case BitfieldType.Bitfield16:
                    field.Value = (uint)_reader.ReadUInt16();
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
                    field.Value = (int)_reader.ReadSByte();
                    break;
                case EnumType.Enum16:
                    field.Value = (int)_reader.ReadInt16();
                    break;
                case EnumType.Enum32:
                    field.Value = _reader.ReadInt32();
                    break;
            }

            // Search for the corresponding option and select it
            EnumValue selected = null;
            foreach (EnumValue option in field.Values)
            {
                if (option.Value == field.Value)
                    selected = option;
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

			field.Value = "#";
			foreach(var formatChar in field.Format.ToCharArray())
				field.Value += (_reader.ReadByte().ToString("X2"));
		}
		public void VisitColourFloat(ColourData field)
		{
			SeekToOffset(field.Offset);

			field.Value = "#";
			foreach (var formatChar in field.Format.ToCharArray())
				field.Value += ((int)(_reader.ReadFloat() * 255)).ToString("X2");
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
            field.Value = _cache.StringIDs.StringIDToIndex(new StringID(_reader.ReadInt32()));
        }

        public void VisitRawData(RawData field)
        {
            SeekToOffset(field.Offset);
            field.Value = FunctionHelpers.BytesToHexLines(_reader.ReadBlock(field.Length), 24);
        }

        public void VisitDataRef(DataRef field)
        {
            SeekToOffset(field.Offset);
            StructureValueCollection values = StructureReader.ReadStructure(_reader, _dataRefLayout);

            int length = (int)values.GetInteger("size");
            uint pointer = values.GetInteger("pointer");
            field.DataAddress = pointer;

            // Check if the pointer is valid
            uint offset = (uint)_cache.MetaArea.PointerToOffset(pointer);
            int metaStartOff = _cache.MetaArea.Offset;
            int metaEndOff = metaStartOff + _cache.MetaArea.Size;
            if (length > 0 && offset >= metaStartOff && offset + field.Length <= metaEndOff)
            {
                field.Length = length;

                // Go to position
                _reader.SeekTo(offset);

                // Read Data
                byte[] data = _reader.ReadBlock(field.Length);

                // Convert to hex string
                field.Value = FunctionHelpers.BytesToHexLines(data, 24);
            }
            else
            {
                field.Length = 0;
            }
        }

        public void VisitTagRef(TagRefData field)
        {
            SeekToOffset(field.Offset);

            DatumIndex index;
            if (field.WithClass)
            {
                // Read the datum index based upon the layout
                StructureValueCollection values = StructureReader.ReadStructure(_reader, _tagRefLayout);
                index = new DatumIndex(values.GetInteger("datum index"));
            }
            else
            {
                // Just read the datum index at the current position
                index = DatumIndex.ReadFrom(_reader);
            }
            
            TagEntry tag = null;
            if (index.IsValid && index.Index < field.Tags.Entries.Count)
                tag = field.Tags.Entries[index.Index];

            if (tag != null)
            {
                field.Class = tag.ParentClass;
                field.Value = tag;
            }
            else
            {
                field.Class = null;
                field.Value = null;
            }
        }

        public void VisitFloat32(Float32Data field)
        {
            SeekToOffset(field.Offset);
            field.Value = _reader.ReadFloat();
        }

        public void VisitVector(VectorData field)
        {
            SeekToOffset(field.Offset);
            field.X = _reader.ReadFloat();
            field.Y = _reader.ReadFloat();
            field.Z = _reader.ReadFloat();
        }

	    public void VisitDegree(DegreeData field)
	    {
		    SeekToOffset(field.Offset);
		    field.Radian = _reader.ReadFloat();
	    }

	    public void VisitReflexive(ReflexiveData field)
        {
            SeekToOffset(field.Offset);
            StructureValueCollection values = StructureReader.ReadStructure(_reader, _reflexiveLayout);
            int length = (int)values.GetInteger("entry count");
            uint pointer = (uint)values.GetInteger("pointer");

            // Make sure the pointer looks valid
            int metaStartOff = _cache.MetaArea.Offset;
            int metaEndOff = metaStartOff + _cache.MetaArea.Size;
            int offset = _cache.MetaArea.PointerToOffset(pointer);
            if (offset < metaStartOff || offset + length * field.EntrySize > metaEndOff)
                length = 0;

            field.Length = length;
            if (pointer != field.FirstEntryAddress)
                field.FirstEntryAddress = pointer;
        }

        public void VisitReflexiveEntry(WrappedReflexiveEntry field)
        {
        }

        /// <summary>
        /// Opens the file for reading and sets _reader to the stream. Must be done before any I/O operations are performed.
        /// </summary>
        /// <returns>false if the file was already open.</returns>
        private bool OpenReader()
        {
            if (_reader == null)
            {
                _reader = new EndianReader(_streamManager.OpenRead(), _streamManager.SuggestedEndian);
                return true;
            }
            return false;
        }

        private void CloseReader()
        {
            if (_reader != null)
            {
                _reader.Close();
                _reader = null;
            }
        }

        private void SeekToOffset(uint offset)
        {
            _reader.SeekTo(_baseOffset + offset);
        }
	}
}
