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

using Blamite.IO;

namespace Blamite.Flexibility
{
	/// <summary>
	///     Provides a means to write values to an IWriter based off of a predefined
	///     structure layout.
	/// </summary>
	public class StructureWriter : IStructureLayoutVisitor
	{
		private readonly long _baseOffset; // The offset that the writer was at when writing began
		private readonly StructureValueCollection _collection; // The values that are being written
		private readonly IWriter _writer; // The stream to write to
		private long _offset; // The offset that the writer is currently at

		private StructureWriter(StructureValueCollection values, IWriter writer)
		{
			_writer = writer;
			_baseOffset = writer.Position;
			_offset = _baseOffset;
			_collection = values;
		}

		public void VisitBasicField(string name, StructureValueType type, int offset)
		{
			// Skip over the field if it isn't in the value collection
			if (type == StructureValueType.Asciiz)
			{
				if (!_collection.HasString(name))
					return;
			}
			else if (!_collection.HasInteger(name))
			{
				return;
			}

			SeekWriter(offset);
			switch (type)
			{
				case StructureValueType.Byte:
					_writer.WriteByte((byte) _collection.GetInteger(name));
					_offset++;
					break;
				case StructureValueType.SByte:
					_writer.WriteSByte((sbyte) _collection.GetInteger(name));
					_offset++;
					break;
				case StructureValueType.UInt16:
					_writer.WriteUInt16((ushort) _collection.GetInteger(name));
					_offset += 2;
					break;
				case StructureValueType.Int16:
					_writer.WriteInt16((short) _collection.GetInteger(name));
					_offset += 2;
					break;
				case StructureValueType.UInt32:
					_writer.WriteUInt32(_collection.GetInteger(name));
					_offset += 4;
					break;
				case StructureValueType.Int32:
					_writer.WriteInt32((int) _collection.GetInteger(name));
					_offset += 4;
					break;
				case StructureValueType.Asciiz:
					_writer.WriteAscii(_collection.GetString(name));
					_offset = _writer.Position;
					break;
				case StructureValueType.Float32:
					_writer.WriteFloat(_collection.GetFloat(name));
					_offset += 4;
					break;
			}
		}

		public void VisitArrayField(string name, int offset, int count, StructureLayout entryLayout)
		{
			if (!_collection.HasArray(name))
				return;

			StructureValueCollection[] arrayValue = _collection.GetArray(name);
			for (int i = 0; i < count; i++)
			{
				_writer.SeekTo(_baseOffset + offset + i*entryLayout.Size);
				WriteStructure(arrayValue[i], entryLayout, _writer);
			}
		}

		public void VisitRawField(string name, int offset, int size)
		{
			if (!_collection.HasRaw(name))
				return;

			SeekWriter(offset);
			_writer.WriteBlock(_collection.GetRaw(name), 0, size);
			_offset += size;
		}

		public static void WriteStructure(StructureValueCollection values, StructureLayout layout, IWriter writer)
		{
			var structWriter = new StructureWriter(values, writer);
			layout.Accept(structWriter);

			if (layout.Size > 0)
				structWriter.SeekWriter(layout.Size);
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