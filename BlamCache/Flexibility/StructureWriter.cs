/* Copyright 2012 Aaron Dierking, TJ Tunnell, Jordan Mueller, Alex Reed
 * 
 * This file is part of ExtryzeDLL.
 * 
 * Extryze is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Extryze is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ExtryzeDLL.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Flexibility
{
    /// <summary>
    /// Provides a means to write values to an IWriter based off of a predefined
    /// structure layout.
    /// </summary>
    public class StructureWriter : IStructureLayoutVisitor
    {
        private IWriter _writer;                         // The stream to write to
        private StructureLayout _layout;                 // The structure layout to follow
        private long _offset;                            // The offset that the writer is currently at
        private long _baseOffset;                        // The offset that the writer was at when writing began
        private StructureValueCollection _collection;    // The values that are being written

        public static void WriteStructure(StructureValueCollection values, StructureLayout layout, IWriter writer)
        {
            StructureWriter structWriter = new StructureWriter(values, layout, writer);
            layout.Accept(structWriter);
        }

        private StructureWriter(StructureValueCollection values, StructureLayout layout, IWriter writer)
        {
            _writer = writer;
            _layout = layout;
            _baseOffset = writer.Position;
            _offset = _baseOffset;
            _collection = values;
        }

        public void VisitBasicField(string name, StructureValueType type, int offset)
        {
            SeekWriter(offset);
            switch (type)
            {
                case StructureValueType.Byte:
                    _writer.WriteByte((byte)_collection.GetNumber(name));
                    _offset++;
                    break;
                case StructureValueType.SByte:
                    _writer.WriteSByte((sbyte)_collection.GetNumber(name));
                    _offset++;
                    break;
                case StructureValueType.UInt16:
                    _writer.WriteUInt16((ushort)_collection.GetNumber(name));
                    _offset += 2;
                    break;
                case StructureValueType.Int16:
                    _writer.WriteInt16((short)_collection.GetNumber(name));
                    _offset += 2;
                    break;
                case StructureValueType.UInt32:
                    _writer.WriteUInt32(_collection.GetNumber(name));
                    _offset += 4;
                    break;
                case StructureValueType.Int32:
                    _writer.WriteInt32((int)_collection.GetNumber(name));
                    _offset += 4;
                    break;
                case StructureValueType.Asciiz:
                    _writer.WriteAscii(_collection.GetString(name));
                    _offset = _writer.Position;
                    break;
            }
        }

        public void VisitArrayField(string name, int offset, int count, int entrySize, StructureLayout entryLayout)
        {
            StructureValueCollection[] arrayValue = _collection.GetArray(name);
            for (int i = 0; i < count; i++)
            {
                _writer.SeekTo(_baseOffset + offset + i * entrySize);
                WriteStructure(arrayValue[i], entryLayout, _writer);
            }
        }

        public void VisitRawField(string name, int offset, int size)
        {
            SeekWriter(offset);
            _writer.WriteBlock(_collection.GetRaw(name), 0, size);
            _offset += size;
        }

        private void SeekWriter(int offset)
        {
            // Seeking is SLOW - only seek if we have to
            if (_offset != _baseOffset + offset)
            {
                _offset = _baseOffset + offset;
                _writer.SeekTo(_offset);
            }
        }
    }
}
