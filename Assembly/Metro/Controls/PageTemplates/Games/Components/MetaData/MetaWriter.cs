using Assembly.Helpers;
using ExtryzeDLL.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.ThirdGen;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    class MetaWriter : IMetaFieldVisitor
    {
        private IWriter _writer;
        private ICacheFile _cache;
        private SaveType _type;
        private bool _onlyUpdateChanged;

        public enum SaveType { Cache, Memory }


        /// <summary>
        /// Save meta to the Blam Cache File
        /// </summary>
        public MetaWriter(IWriter writer, ICacheFile cache, SaveType type, bool onlyUpdateChanged)
        {
            _writer = writer;
            _cache = cache;
            _type = type;
            _onlyUpdateChanged = onlyUpdateChanged;
        }

        public void Poke(IEnumerable<MetaField> fields)
        {
            foreach (MetaField field in fields)
                field.Accept(this);
        }

        public void VisitBitfield(BitfieldData field)
        {
            if (field.HasChanged || !_onlyUpdateChanged)
            {
                if (_type == SaveType.Cache)
                    _writer.SeekTo(field.CacheOffset);
                else
                    _writer.SeekTo(field.MemoryAddress);
                switch (field.Type)
                {
                    case BitfieldType.Bitfield8:
                        _writer.WriteByte((byte)field.Value);
                        break;
                    case BitfieldType.Bitfield16:
                        _writer.WriteUInt16((UInt16)field.Value);
                        break;
                    case BitfieldType.Bitfield32:
                        _writer.WriteUInt32((UInt32)field.Value);
                        break;
                }
            }
        }

        public void VisitComment(CommentData field) { /* Comments don't need to be written... ;x */ }

        public void VisitEnum(EnumData field)
        {
            if (field.HasChanged || !_onlyUpdateChanged)
            {
                if (_type == SaveType.Cache)
                    _writer.SeekTo(field.CacheOffset);
                else
                    _writer.SeekTo(field.MemoryAddress);
                switch (field.Type)
                {
                    case EnumType.Enum8:
                        _writer.WriteByte((byte)field.Value);
                        break;
                    case EnumType.Enum16:
                        _writer.WriteInt16((Int16)field.Value);
                        break;
                    case EnumType.Enum32:
                        _writer.WriteInt32((Int32)field.Value);
                        break;
                }
            }
        }

        public void VisitUint8(Uint8Data field)
        {
            if (field.HasChanged || !_onlyUpdateChanged)
            {
                if (_type == SaveType.Cache)
                    _writer.SeekTo(field.CacheOffset);
                else
                    _writer.SeekTo(field.MemoryAddress);

                _writer.WriteByte(field.Value);
            }
        }
        public void VisitInt8(Int8Data field)
        {
            if (field.HasChanged || !_onlyUpdateChanged)
            {
                if (_type == SaveType.Cache)
                    _writer.SeekTo(field.CacheOffset);
                else
                    _writer.SeekTo(field.MemoryAddress);

                _writer.WriteByte((byte)field.Value);
            }
        }
        public void VisitUint16(Uint16Data field)
        {
            if (field.HasChanged || !_onlyUpdateChanged)
            {
                if (_type == SaveType.Cache)
                    _writer.SeekTo(field.CacheOffset);
                else
                    _writer.SeekTo(field.MemoryAddress);

                _writer.WriteUInt16(field.Value);
            }
        }
        public void VisitInt16(Int16Data field)
        {
            if (field.HasChanged || !_onlyUpdateChanged)
            {
                if (_type == SaveType.Cache)
                    _writer.SeekTo(field.CacheOffset);
                else
                    _writer.SeekTo(field.MemoryAddress);

                _writer.WriteInt16(field.Value);
            }
        }
        public void VisitUint32(Uint32Data field)
        {
            if (field.HasChanged || !_onlyUpdateChanged)
            {
                if (_type == SaveType.Cache)
                    _writer.SeekTo(field.CacheOffset);
                else
                    _writer.SeekTo(field.MemoryAddress);

                _writer.WriteUInt32(field.Value);
            }
        }
        public void VisitInt32(Int32Data field)
        {
            if (_type == SaveType.Cache)
                _writer.SeekTo(field.CacheOffset);
            else
                _writer.SeekTo(field.MemoryAddress);

            _writer.WriteInt32(field.Value);
        }
        public void VisitFloat32(Float32Data field)
        {
            if (field.HasChanged || !_onlyUpdateChanged)
            {
                if (_type == SaveType.Cache)
                    _writer.SeekTo(field.CacheOffset);
                else
                    _writer.SeekTo(field.MemoryAddress);

                _writer.WriteFloat(field.Value);
            }
        }
        public void VisitVector(VectorData field)
        {
            if (field.HasChanged || !_onlyUpdateChanged)
            {
                if (_type == SaveType.Cache)
                    _writer.SeekTo(field.CacheOffset);
                else
                    _writer.SeekTo(field.MemoryAddress);

                _writer.WriteFloat(field.X);
                _writer.WriteFloat(field.Y);
                _writer.WriteFloat(field.Z);
            }
        }

        public void VisitString(StringData field)
        {
            if (field.HasChanged || !_onlyUpdateChanged)
            {
                if (_type == SaveType.Cache)
                    _writer.SeekTo(field.CacheOffset);
                else
                    _writer.SeekTo(field.MemoryAddress);

                _writer.WriteAscii(field.Value);
            }
        }
        public void VisitStringID(StringIDData field)
        {
            if (field.HasChanged || !_onlyUpdateChanged)
            {
                if (_type == SaveType.Cache)
                    _writer.SeekTo(field.CacheOffset);
                else
                    _writer.SeekTo(field.MemoryAddress);

                _writer.WriteInt32(_cache.StringIDs.IndexToStringID(field.Value).Value);
            }
        }

        public void VisitRawData(RawData field)
        {
            if (field.HasChanged || !_onlyUpdateChanged)
            {
                if (_type == SaveType.Cache)
                    _writer.SeekTo(field.CacheOffset);
                else
                    _writer.SeekTo(field.MemoryAddress);

                _writer.WriteBlock(ExtryzeDLL.Util.FunctionHelpers.HexStringToBytes(field.Value));
            }
        }
        public void VisitDataRef(DataRef field)
        {
            if (field.HasChanged || !_onlyUpdateChanged)
            {
                string finalValue = field.Value.PadRight(field.Length, '0');

                byte[] data = ExtryzeDLL.Util.FunctionHelpers.HexStringToBytes(finalValue);

                if (_type == SaveType.Cache)
                    _writer.SeekTo(field.DataCacheOffset);
                else
                    _writer.SeekTo(field.DataMemoryAddress);

                _writer.WriteBlock(data);
            }
        }

        public void VisitTagRef(TagRefData field)
        {
            if (field.HasChanged || !_onlyUpdateChanged)
            {
                if (_type == SaveType.Cache)
                    _writer.SeekTo(field.CacheOffset);
                else
                    _writer.SeekTo(field.MemoryAddress);

                if (field.WithClass)
                {
                    if (field.Class != null)
                        _writer.WriteInt32(field.Class.RawClass.Magic);
                    else
                        _writer.WriteInt32(-1);
                    _writer.Skip(8);
                }

                if (field.Class == null)
                {
                    // Write null tagref
                    _writer.WriteUInt32(0xFFFFFFFF);
                }
                else
                {
                    // Write the non-null tagref
                    _writer.WriteUInt32(field.Value.RawTag.Index.Value);
                }
            }
        }

        public void VisitReflexive(ReflexiveData field)
        {
            if (field.HasChildren)
                ReadCurrentItems(field);
        }

        public void VisitReflexiveEntry(WrappedReflexiveEntry field)
        {
            /* Ignore this. */
        }

        private void ReadCurrentItems(ReflexiveData reflexive)
        {
            ReflexivePage page = reflexive.Pages[reflexive.CurrentIndex];
            for (int i = 0; i < page.Fields.Length; i++)
            {
                if (page.Fields[i] != null)
                    page.Fields[i].Accept(this);
                else
                    reflexive.Template[i].Accept(this);
            }
        }
    }
}