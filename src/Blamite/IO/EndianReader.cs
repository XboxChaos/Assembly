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
using System.IO;
using System.Linq;
using System.Text;

namespace Blamite.IO
{
	/// <summary>
	///     A stream which can be read from and whose endianness can be changed.
	/// </summary>
	public class EndianReader : IDisposable, IReader
	{
		private readonly byte[] _buffer = new byte[8];
		private readonly StringBuilder _currentString = new StringBuilder();
		private readonly Stream _stream;
		private bool _bigEndian;

		/// <summary>
		///     Initializes a new instance of the <see cref="EndianReader" /> class.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="endianness">The initial endianness to use when reading from the stream.</param>
		public EndianReader(Stream stream, Endian endianness)
		{
			_stream = stream;
			_bigEndian = (endianness == Endian.BigEndian);
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
			get { return _bigEndian ? Endian.BigEndian : Endian.LittleEndian; }
			set { _bigEndian = (value == Endian.BigEndian); }
		}

		/// <summary>
		///     Closes the stream, releasing any I/O resources it has acquired.
		/// </summary>
		public void Close()
		{
			_stream.Close();
		}

		/// <summary>
		///     Reads a byte from the stream.
		/// </summary>
		/// <returns>
		///     The byte that was read.
		/// </returns>
		public byte ReadByte()
		{
			_stream.Read(_buffer, 0, 1);
			return _buffer[0];
		}

		/// <summary>
		///     Reads a signed byte from the stream.
		/// </summary>
		/// <returns>
		///     The signed byte that was read.
		/// </returns>
		public sbyte ReadSByte()
		{
			return (sbyte) ReadByte();
		}

		/// <summary>
		///     Reads a 16-bit unsigned integer from the stream.
		/// </summary>
		/// <returns>
		///     The 16-bit unsigned integer that was read.
		/// </returns>
		public ushort ReadUInt16()
		{
			_stream.Read(_buffer, 0, 2);
			if (_bigEndian)
				return (ushort) ((_buffer[0] << 8) | _buffer[1]);
			return (ushort) ((_buffer[1] << 8) | _buffer[0]);
		}

		/// <summary>
		///     Reads a 16-bit signed integer from the stream.
		/// </summary>
		/// <returns>
		///     The 16-bit signed integer that was read.
		/// </returns>
		public short ReadInt16()
		{
			return (short) ReadUInt16();
		}

		/// <summary>
		///     Reads a 32-bit unsigned integer from the stream.
		/// </summary>
		/// <returns>
		///     The 32-bit unsigned integer that was read.
		/// </returns>
		public uint ReadUInt32()
		{
			_stream.Read(_buffer, 0, 4);
			if (_bigEndian)
				return (uint) ((_buffer[0] << 24) | (_buffer[1] << 16) | (_buffer[2] << 8) | _buffer[3]);
			return (uint) ((_buffer[3] << 24) | (_buffer[2] << 16) | (_buffer[1] << 8) | _buffer[0]);
		}

		/// <summary>
		///     Reads a 32-bit signed integer from the stream.
		/// </summary>
		/// <returns>
		///     The 32-bit signed integer that was read.
		/// </returns>
		public int ReadInt32()
		{
			return (int) ReadUInt32();
		}

		/// <summary>
		///     Reads a 64-bit unsigned integer from the stream.
		/// </summary>
		/// <returns>
		///     The 64-bit unsigned integer that was read.
		/// </returns>
		public ulong ReadUInt64()
		{
			/*_stream.Read(_buffer, 0, 8);
            return (ulong)((_buffer[0] << 56) | (_buffer[1] << 48) | (_buffer[2] << 40) | (_buffer[3] << 32) |
                           (_buffer[4] << 24) | (_buffer[5] << 16) | (_buffer[6] << 8) | _buffer[7]);*/
			ulong one = ReadUInt32();
			ulong two = ReadUInt32();
			if (_bigEndian)
				return (one << 32) | two;
			return (two << 32) | one;
		}

		/// <summary>
		///     Reads a 64-bit signed integer from the stream.
		/// </summary>
		/// <returns>
		///     The 64-bit signed integer that was read.
		/// </returns>
		public long ReadInt64()
		{
			/*_stream.Read(_buffer, 0, 8);
            return (long)((_buffer[0] << 56) | (_buffer[1] << 48) | (_buffer[2] << 40) | (_buffer[3] << 32) |
                          (_buffer[4] << 24) | (_buffer[5] << 16) | (_buffer[6] << 8) | _buffer[7]);*/
			return (long) ReadUInt64();
		}

		/// <summary>
		///     Reads a 32-bit floating-point value from the stream.
		/// </summary>
		/// <returns>
		///     The 32-bit floating-point value that was read.
		/// </returns>
		public float ReadFloat()
		{
			_stream.Read(_buffer, 0, 4);
			if (BitConverter.IsLittleEndian == _bigEndian)
			{
				// Flip the bytes
				// Is there a faster way to do this?
				byte temp = _buffer[0];
				_buffer[0] = _buffer[3];
				_buffer[3] = temp;
				temp = _buffer[1];
				_buffer[1] = _buffer[2];
				_buffer[2] = temp;
			}
			return BitConverter.ToSingle(_buffer, 0);
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
		///     Reads a null-terminated ASCII string from the stream.
		/// </summary>
		/// <returns>
		///     The ASCII string that was read.
		/// </returns>
		public string ReadAscii()
		{
			_currentString.Clear();
			int ch;
			while (true)
			{
				ch = _stream.ReadByte();
				if (ch == 0 || ch == -1)
					break;
				_currentString.Append((char) ch);
			}
			return _currentString.ToString();
		}

		/// <summary>
		///     Reads a fixed-size null-terminated ASCII string from the stream.
		/// </summary>
		/// <param name="size">The size of the string to read, including the null terminator.</param>
		/// <returns>
		///     The ASCII string that was read, with any 0 padding bytes stripped.
		/// </returns>
		public unsafe string ReadAscii(int size)
		{
			var chars = new sbyte[size];
			string result;
			fixed (sbyte* str = chars)
			{
				_stream.Read((byte[]) (Array) chars, 0, size);
				result = new string(str);
			}
			return result;
		}

		/// <summary>
		///     Reads a null-terminated UTF-8 string from the stream.
		/// </summary>
		/// <returns>
		///     The null-terminated UTF-8 string that was read.
		/// </returns>
		public unsafe string ReadUTF8()
		{
			var chars = new List<sbyte>();
			sbyte ch;
			while (true)
			{
				ch = ReadSByte();
				if (ch == 0)
					break;
				chars.Add(ch);
			}

			sbyte[] charss = chars.ToArray<sbyte>();
			fixed (sbyte* prt = charss)
				return new string(prt, 0, chars.Count, Encoding.UTF8);
		}

		/// <summary>
		///     Reads a fixed-size null-terminated UTF-8 string from the stream.
		/// </summary>
		/// <param name="size">The size in bytes of the string to read, including the null terminator.</param>
		/// <returns>
		///     The UTF-8 string that was read, with any padding bytes stripped.
		/// </returns>
		public unsafe string ReadUTF8(int size)
		{
			var chars = new sbyte[size];
			string result;
			fixed (sbyte* str = chars)
			{
				_stream.Read((byte[]) (Array) chars, 0, size);
				result = new string(str, 0, size, Encoding.UTF8);
			}
			return result;
		}

		/// <summary>
		///     Reads a null-terminated UTF-16 string from the stream.
		/// </summary>
		/// <returns>
		///     The UTF-16 string that was read.
		/// </returns>
		public string ReadUTF16()
		{
			_currentString.Clear();
			int ch;
			while (true)
			{
				ch = ReadInt16();
				if (ch == 0)
					break;
				_currentString.Append((char) ch);
			}
			return _currentString.ToString();
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
			_currentString.Clear();
			int ch;
			while (_currentString.Length*2 < size)
			{
				ch = ReadInt16();
				if (ch == 0)
					break;
				_currentString.Append((char) ch);
			}
			Skip(size - _currentString.Length*2);
			return _currentString.ToString();
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
			var result = new byte[size];
			_stream.Read(result, 0, size);
			return result;
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
			return _stream.Read(output, offset, size);
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