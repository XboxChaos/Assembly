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
using System.IO;

namespace Blamite.IO
{
	/// <summary>
	///     A stream which can be both read from and written to and whose endianness can be changed.
	/// </summary>
	public class EndianStream : IDisposable, IStream
	{
		private readonly EndianReader _reader;
		private readonly Stream _stream;
		private readonly EndianWriter _writer;

		/// <summary>
		///     Initializes a new instance of the <see cref="EndianStream" /> class.
		/// </summary>
		/// <param name="stream">The stream to read from and write to.</param>
		/// <param name="endianness">The initial endianness to use when operating on the stream.</param>
		/// <exception cref="System.ArgumentException">Thrown if <paramref name="stream" /> is not both readable and writable.</exception>
		public EndianStream(Stream stream, Endian endianness)
		{
			if (!stream.CanRead || !stream.CanWrite)
				throw new ArgumentException("The input stream must be both readable and writable.");

			_stream = stream;
			_reader = new EndianReader(stream, endianness);
			_writer = new EndianWriter(stream, endianness);
		}

		/// <summary>
		///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Close();
		}

		/// <summary>
		///     Gets or sets the endianness used when reading/writing to/from the stream.
		/// </summary>
		public Endian Endianness
		{
			get { return _reader.Endianness; }
			set
			{
				_reader.Endianness = value;
				_writer.Endianness = value;
			}
		}

		/// <summary>
		///     Closes the stream, releasing any I/O resources it has acquired.
		/// </summary>
		public void Close()
		{
			_reader.Close();
			_writer.Close();
		}

		/// <summary>
		///     Reads a byte from the stream.
		/// </summary>
		/// <returns>
		///     The byte that was read.
		/// </returns>
		public byte ReadByte()
		{
			return _reader.ReadByte();
		}

		/// <summary>
		///     Writes the byte.
		/// </summary>
		/// <param name="b">The b.</param>
		public void WriteByte(byte b)
		{
			_writer.WriteByte(b);
		}

		/// <summary>
		///     Reads a signed byte from the stream.
		/// </summary>
		/// <returns>
		///     The signed byte that was read.
		/// </returns>
		public sbyte ReadSByte()
		{
			return _reader.ReadSByte();
		}

		/// <summary>
		///     Writes the S byte.
		/// </summary>
		/// <param name="b">The b.</param>
		public void WriteSByte(sbyte b)
		{
			_writer.WriteSByte(b);
		}

		/// <summary>
		///     Reads a 16-bit unsigned integer from the stream.
		/// </summary>
		/// <returns>
		///     The 16-bit unsigned integer that was read.
		/// </returns>
		public ushort ReadUInt16()
		{
			return _reader.ReadUInt16();
		}

		/// <summary>
		///     Writes an unsigned 16-bit integer to the stream.
		/// </summary>
		/// <param name="value">The unsigned 16-bit integer to write.</param>
		public void WriteUInt16(ushort value)
		{
			_writer.WriteUInt16(value);
		}

		/// <summary>
		///     Reads a 16-bit signed integer from the stream.
		/// </summary>
		/// <returns>
		///     The 16-bit signed integer that was read.
		/// </returns>
		public short ReadInt16()
		{
			return _reader.ReadInt16();
		}

		/// <summary>
		///     Writes a signed 16-bit integer to the stream.
		/// </summary>
		/// <param name="value">The signed 16-bit integer to write.</param>
		public void WriteInt16(short value)
		{
			_writer.WriteInt16(value);
		}

		/// <summary>
		///     Reads a 32-bit unsigned integer from the stream.
		/// </summary>
		/// <returns>
		///     The 32-bit unsigned integer that was read.
		/// </returns>
		public uint ReadUInt32()
		{
			return _reader.ReadUInt32();
		}

		/// <summary>
		///     Writes an unsigned 32-bit integer to the stream.
		/// </summary>
		/// <param name="value">The unsigned 32-bit integer to write.</param>
		public void WriteUInt32(uint value)
		{
			_writer.WriteUInt32(value);
		}

		/// <summary>
		///     Reads a 32-bit signed integer from the stream.
		/// </summary>
		/// <returns>
		///     The 32-bit signed integer that was read.
		/// </returns>
		public int ReadInt32()
		{
			return _reader.ReadInt32();
		}

		/// <summary>
		///     Writes a signed 32-bit integer to the stream.
		/// </summary>
		/// <param name="value">The signed 32-bit integer to write.</param>
		public void WriteInt32(int value)
		{
			_writer.WriteInt32(value);
		}

		/// <summary>
		///     Reads a 64-bit unsigned integer from the stream.
		/// </summary>
		/// <returns>
		///     The 64-bit unsigned integer that was read.
		/// </returns>
		public ulong ReadUInt64()
		{
			return _reader.ReadUInt64();
		}

		/// <summary>
		///     Writes an unsigned 64-bit integer to the stream.
		/// </summary>
		/// <param name="value">The unsigned 64-bit integer to write.</param>
		public void WriteUInt64(ulong value)
		{
			_writer.WriteUInt64(value);
		}

		/// <summary>
		///     Reads a 64-bit signed integer from the stream.
		/// </summary>
		/// <returns>
		///     The 64-bit signed integer that was read.
		/// </returns>
		public long ReadInt64()
		{
			return _reader.ReadInt64();
		}

		/// <summary>
		///     Writes a signed 64-bit integer to the stream.
		/// </summary>
		/// <param name="value">The signed 64-bit integer to write.</param>
		public void WriteInt64(long value)
		{
			_writer.WriteInt64(value);
		}

		/// <summary>
		///     Reads a 32-bit floating-point value from the stream.
		/// </summary>
		/// <returns>
		///     The 32-bit floating-point value that was read.
		/// </returns>
		public float ReadFloat()
		{
			return _reader.ReadFloat();
		}

		/// <summary>
		///     Writes a 32-bit floating-point value to the stream.
		/// </summary>
		/// <param name="value">The 32-bit floating-point value to write.</param>
		public void WriteFloat(float value)
		{
			_writer.WriteFloat(value);
		}

		/// <summary>
		///     Reads a null-terminated ASCII string from the stream.
		/// </summary>
		/// <returns>
		///     The ASCII string that was read.
		/// </returns>
		public string ReadAscii()
		{
			return _reader.ReadAscii();
		}

		/// <summary>
		///     Reads a fixed-size null-terminated ASCII string from the stream.
		/// </summary>
		/// <param name="size">The size of the string to read, including the null terminator.</param>
		/// <returns>
		///     The ASCII string that was read, with any 0 padding bytes stripped.
		/// </returns>
		public string ReadAscii(int size)
		{
			return _reader.ReadAscii(size);
		}

		/// <summary>
		///     Writes an ASCII string to the stream, followed by a null terminator.
		/// </summary>
		/// <param name="str">The ASCII string to write.</param>
		public void WriteAscii(string str)
		{
			_writer.WriteAscii(str);
		}

		/// <summary>
		///     Reads a null-terminated UTF-8 string from the stream.
		/// </summary>
		/// <returns>
		///     The null-terminated UTF-8 string that was read.
		/// </returns>
		public string ReadUTF8()
		{
			return _reader.ReadUTF8();
		}

		/// <summary>
		///     Reads a fixed-size null-terminated UTF-8 string from the stream.
		/// </summary>
		/// <param name="size">The size in bytes of the string to read, including the null terminator.</param>
		/// <returns>
		///     The UTF-8 string that was read, with any padding bytes stripped.
		/// </returns>
		public string ReadUTF8(int size)
		{
			return _reader.ReadUTF8(size);
		}

		/// <summary>
		///     Writes a UTF-8 string to the stream, followed by a null terminator.
		/// </summary>
		/// <param name="str">The UTF-8 string to write.</param>
		public void WriteUTF8(string str)
		{
			_writer.WriteUTF8(str);
		}

		/// <summary>
		///     Reads a null-terminated UTF-16 string from the stream.
		/// </summary>
		/// <returns>
		///     The UTF-16 string that was read.
		/// </returns>
		public string ReadUTF16()
		{
			return _reader.ReadUTF16();
		}

		/// <summary>
		///     Reads a fixed-size null-terminated UTF-16 string from the stream.
		/// </summary>
		/// <param name="size">The size in bytes of the string to read, including the null terminator.</param>
		/// <returns>
		///     The UTF-16 string that was read, with any padding bytes stripped.
		/// </returns>
		public string ReadUTF16(int size)
		{
			return _reader.ReadUTF16(size);
		}

		/// <summary>
		///     Writes a UTF-16 string to the stream, followed by a null terminator.
		/// </summary>
		/// <param name="str">The UTF-16 string to write.</param>
		public void WriteUTF16(string str)
		{
			_writer.WriteUTF16(str);
		}

		/// <summary>
		///     Reads an array of bytes from the stream.
		/// </summary>
		/// <param name="size">The number of bytes to read.</param>
		/// <returns>
		///     The bytes that were read.
		/// </returns>
		public byte[] ReadBlock(int size)
		{
			return _reader.ReadBlock(size);
		}

		/// <summary>
		///     Reads an array of bytes from the stream.
		/// </summary>
		/// <param name="output">The array to store the read bytes to.</param>
		/// <param name="offset">The starting index in the array to read to.</param>
		/// <param name="size">The number of bytes to read.</param>
		/// <returns>
		///     The number of bytes that were actually read.
		/// </returns>
		public int ReadBlock(byte[] output, int offset, int size)
		{
			return _reader.ReadBlock(output, offset, size);
		}

		/// <summary>
		///     Writes an array of bytes to the stream.
		/// </summary>
		/// <param name="data">The bytes to write.</param>
		public void WriteBlock(byte[] data)
		{
			_writer.WriteBlock(data);
		}

		/// <summary>
		///     Writes an array of bytes to the stream.
		/// </summary>
		/// <param name="data">The bytes to write.</param>
		/// <param name="offset">The starting index in the array to write.</param>
		/// <param name="size">The number of bytes to write.</param>
		public void WriteBlock(byte[] data, int offset, int size)
		{
			_writer.WriteBlock(data, offset, size);
		}

		/// <summary>
		///     Seeks to an offset in the stream.
		/// </summary>
		/// <param name="offset">The offset to move the stream pointer to.</param>
		/// <returns>
		///     true if the seek was successful.
		/// </returns>
		public bool SeekTo(long offset)
		{
			if (offset < 0)
				return false;
			_stream.Seek(offset, SeekOrigin.Begin);
			return true;
		}

		/// <summary>
		///     Skips over a number of bytes in the stream.
		/// </summary>
		/// <param name="count">The number of bytes to skip.</param>
		public void Skip(long count)
		{
			_stream.Seek(count, SeekOrigin.Current);
		}

		/// <summary>
		///     Gets whether or not the stream pointer is at the end of the stream.
		/// </summary>
		public bool EOF
		{
			get { return (Position >= Length); }
		}

		/// <summary>
		///     Gets the current position of the stream pointer.
		/// </summary>
		public long Position
		{
			get { return _stream.Position; }
		}

		/// <summary>
		///     Gets the length of the stream in bytes.
		/// </summary>
		public long Length
		{
			get { return _stream.Length; }
		}

		/// <summary>
		///     Gets the base Stream object the stream was constructed from.
		/// </summary>
		public Stream BaseStream
		{
			get { return _stream; }
		}
	}
}