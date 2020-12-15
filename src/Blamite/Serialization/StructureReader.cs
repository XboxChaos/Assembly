using Blamite.Blam;
using Blamite.Blam.Util;
using Blamite.IO;
using System;
using System.Runtime.InteropServices;

namespace Blamite.Serialization
{
	/// <summary>
	///     Provides a means to read values from an IReader based off of a predefined
	///     structure layout.
	/// </summary>
	public class StructureReader : IStructureLayoutVisitor
	{
		protected readonly long _baseOffset; // The offset that the reader was at when reading began
		protected readonly StructureValueCollection _collection; // The values that have been read so far
		protected readonly IReader _reader; // The stream to read from
		protected long _offset; // The offset that the reader is currently at

		/// <summary>
		///     (private) Constructs a new StructureReader.
		/// </summary>
		/// <param name="reader">The IReader to read from.</param>
		protected StructureReader(IReader reader)
		{
			_reader = reader;
			_baseOffset = reader.Position;
			_offset = _baseOffset;
			_collection = new StructureValueCollection();
		}

		/// <summary>
		///     Reads a basic value from the stream and adds it to the value
		///     collection which is currently being built.
		/// </summary>
		/// <param name="name">The name to store the value under.</param>
		/// <param name="type">The type of the value to read.</param>
		/// <param name="offset">The value's offset (in bytes) from the beginning of the structure.</param>
		/// <seealso cref="IStructureLayoutVisitor.VisitBasicField" />
		public void VisitBasicField(string name, StructureValueType type, int offset)
		{
			SeekReader(offset);
			switch (type)
			{
				case StructureValueType.Byte:
					_collection.SetInteger(name, _reader.ReadByte());
					_offset++;
					break;
				case StructureValueType.SByte:
					_collection.SetInteger(name, (uint) _reader.ReadSByte());
					_offset++;
					break;
				case StructureValueType.UInt16:
					_collection.SetInteger(name, _reader.ReadUInt16());
					_offset += 2;
					break;
				case StructureValueType.Int16:
					_collection.SetInteger(name, (uint) _reader.ReadInt16());
					_offset += 2;
					break;
				case StructureValueType.UInt32:
					_collection.SetInteger(name, _reader.ReadUInt32());
					_offset += 4;
					break;
				case StructureValueType.Int32:
					_collection.SetInteger(name, (uint) _reader.ReadInt32());
					_offset += 4;
					break;
				case StructureValueType.UInt64:
					_collection.SetInteger(name, _reader.ReadUInt64());
					_offset += 8;
					break;
				case StructureValueType.Int64:
					_collection.SetInteger(name, (ulong)_reader.ReadInt64());
					_offset += 8;
					break;
				case StructureValueType.Asciiz:
					_collection.SetString(name, _reader.ReadAscii());
					_offset = _reader.Position;
					break;
				case StructureValueType.Float32:
					_collection.SetFloat(name, _reader.ReadFloat());
					_offset += 4;
					break;
			}
		}

		/// <summary>
		///     Reads an array of values from the stream and adds it to the value
		///     collection which is currently being built.
		/// </summary>
		/// <param name="name">The name to store the array under.</param>
		/// <param name="offset">The array's offset (in bytes) from the beginning of the structure.</param>
		/// <param name="count">The number of elements to read into the array.</param>
		/// <param name="entryLayout">The layout to follow for each entry in the array.</param>
		public void VisitArrayField(string name, int offset, int count, StructureLayout entryLayout)
		{
			var arrayValue = new StructureValueCollection[count];
			for (int i = 0; i < count; i++)
			{
				_reader.SeekTo(_baseOffset + offset + i*entryLayout.Size);
				arrayValue[i] = ReadStructure(_reader, entryLayout);
			}
			_collection.SetArray(name, arrayValue);
		}

		/// <summary>
		///     Reads an raw byte array from the stream and adds it to the value
		///     collection which is currently being built.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		/// <param name="size">The size of the raw data to read.</param>
		public void VisitRawField(string name, int offset, int size)
		{
			SeekReader(offset);
			byte[] data = _reader.ReadBlock(size);
			_collection.SetRaw(name, data);
			_offset += data.Length;
		}

		/// <summary>
		///     Reads a structure from the stream and adds it to the value
		///     collection which is currently being built.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		/// <param name="layout">The layout of the data in the structure.</param>
		public void VisitStructField(string name, int offset, StructureLayout layout)
		{
			SeekReader(offset);
			var values = ReadStructure(_reader, layout);
			_collection.SetStruct(name, values);
			
			if (layout.Size > 0)
				_offset += layout.Size;
			else
				_offset = _reader.Position;
		}

		/// <summary>
		///     Reads a structure from a stream by following a predefined structure layout.
		/// </summary>
		/// <param name="reader">The IReader to read the structure from.</param>
		/// <param name="layout">The structure layout to follow.</param>
		/// <returns>A collection of the values that were read.</returns>
		/// <seealso cref="StructureLayout" />
		public static StructureValueCollection ReadStructure(IReader reader,StructureLayout layout)
		{
			var structReader = new StructureReader(reader);
			layout.Accept(structReader);
			if (layout.Size > 0)
				structReader.SeekReader(layout.Size);

			return structReader._collection;
		}

		protected void SeekReader(int offset)
		{
			// Seeking is SLOW - only seek if we actually have to
			if (_offset != _baseOffset + offset)
			{
				_offset = _baseOffset + offset;
				_reader.SeekTo(_offset);
			}
		}
	}
}