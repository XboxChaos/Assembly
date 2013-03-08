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
using System.IO;

namespace Blamite.IO
{
    /// <summary>
    /// Reads big-endian data from a stream.
    /// Designed to support the various types that Bungie uses.
    /// </summary>
    public class EndianReader : IDisposable, IReader
    {
        private Stream _stream = null;
        private byte[] _buffer = new byte[8];
        private StringBuilder _currentString = new StringBuilder();
        private bool _bigEndian;

        /// <summary>
        /// Constructs a new EndianReader object given a base stream and an initial endianness.
        /// </summary>
        /// <param name="baseStream">The stream that data should be read from.</param>
        /// <param name="endianness">The endianness that should be used.</param>
        public EndianReader(Stream stream, Endian endianness)
        {
            _stream = stream;
            _bigEndian = (endianness == Endian.BigEndian);
        }

        /// <summary>
        /// The endianness to use when reading from the stream.
        /// </summary>
        public Endian Endianness
        {
            get
            {
                return _bigEndian ? Endian.BigEndian : Endian.LittleEndian;
            }
            set
            {
                _bigEndian = (value == Endian.BigEndian);
            }
        }

        /// <summary>
        /// Closes this EndianReader and the underlying stream.
        /// </summary>
        public void Close()
        {
            _stream.Close();
        }

        /// <summary>
        /// Disposes of this stream.
        /// <seealso cref="Close"/>
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// Reads a byte from the underlying stream and advances its position by 1.
        /// </summary>
        public byte ReadByte()
        {
            _stream.Read(_buffer, 0, 1);
            return _buffer[0];
        }

        /// <summary>
        /// Reads a signed byte from the underlying stream and advances its position by 1.
        /// </summary>
        public sbyte ReadSByte()
        {
            return (sbyte)ReadByte();
        }

        /// <summary>
        /// Reads a 16-bit unsigned integer from the underlying stream and advances its position by 2.
        /// </summary>
        public ushort ReadUInt16()
        {
            _stream.Read(_buffer, 0, 2);
            if (_bigEndian)
                return (ushort)((_buffer[0] << 8) | _buffer[1]);
            else
                return (ushort)((_buffer[1] << 8) | _buffer[0]);
        }

        /// <summary>
        /// Reads a 16-bit signed integer from the underlying stream and advances its position by 2.
        /// </summary>
        public short ReadInt16()
        {
            return (short)ReadUInt16();
        }

        /// <summary>
        /// Reads a 32-bit unsigned integer from the underlying stream and advances its position by 4.
        /// </summary>
        public uint ReadUInt32()
        {
            _stream.Read(_buffer, 0, 4);
            if (_bigEndian)
                return (uint)((_buffer[0] << 24) | (_buffer[1] << 16) | (_buffer[2] << 8) | _buffer[3]);
            else
                return (uint)((_buffer[3] << 24) | (_buffer[2] << 16) | (_buffer[1] << 8) | _buffer[0]);
        }

        /// <summary>
        /// Reads a 32-bit signed integer from the underlying stream and advances its position by 4.
        /// </summary>
        public int ReadInt32()
        {
            return (int)ReadUInt32();
        }

        /// <summary>
        /// Reads a 64-bit unsigned integer from the underlying stream and advances its position by 8.
        /// </summary>
        /// <returns>The value that was read</returns>
        public ulong ReadUInt64()
        {
            /*_stream.Read(_buffer, 0, 8);
            return (ulong)((_buffer[0] << 56) | (_buffer[1] << 48) | (_buffer[2] << 40) | (_buffer[3] << 32) |
                           (_buffer[4] << 24) | (_buffer[5] << 16) | (_buffer[6] << 8) | _buffer[7]);*/
            ulong one = (ulong)ReadUInt32();
            ulong two = (ulong)ReadUInt32();
            if (_bigEndian)
                return (one << 32) | two;
            else
                return (two << 32) | one;
        }

        /// <summary>
        /// Reads a 64-bit signed integer from the underlying stream and advances its position by 8.
        /// </summary>
        /// <returns>The value that was read</returns>
        public long ReadInt64()
        {
            /*_stream.Read(_buffer, 0, 8);
            return (long)((_buffer[0] << 56) | (_buffer[1] << 48) | (_buffer[2] << 40) | (_buffer[3] << 32) |
                          (_buffer[4] << 24) | (_buffer[5] << 16) | (_buffer[6] << 8) | _buffer[7]);*/
            return (long)ReadUInt64();
        }

        /// <summary>
        /// Reads a 32-bit float from the underlying stream and advances its position by 4.
        /// </summary>
        /// <returns>The float value that was read.</returns>
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
        /// Changes the position of the underlying stream.
        /// </summary>
        /// <param name="offset">The new offset.</param>
        /// <returns>true on success.</returns>
        public bool SeekTo(long offset)
        {
            if (offset < 0)
                return false;
            _stream.Seek(offset, SeekOrigin.Begin);
            return true;
        }

        /// <summary>
        /// Skips over a number of bytes in the underlying stream.
        /// </summary>
        /// <param name="count">The number of bytes to skip.</param>
        public void Skip(long count)
        {
            _stream.Seek(count, SeekOrigin.Current);
        }

        /// <summary>
        /// Reads a null-terminated ASCII string.
        /// </summary>
        /// <returns>The string that was read.</returns>
        public string ReadAscii()
        {
            _currentString.Clear();
            int ch;
            while (true)
            {
                ch = _stream.ReadByte();
                if (ch == 0 || ch == -1)
                    break;
                _currentString.Append((char)ch);
            }
            return _currentString.ToString();
        }

        /// <summary>
        /// Reads an ASCII string of a specific size. Null terminators will be taken into account.
        /// The position of the underlying stream will be advanced by the string size.
        /// </summary>
        /// <param name="size">The size of the string to be read.</param>
        /// <returns>The string that was read.</returns>
        public unsafe string ReadAscii(int size)
        {
            sbyte[] chars = new sbyte[size];
            string result;
            fixed (sbyte* str = chars)
            {
                _stream.Read((byte[])(Array)chars, 0, size);
                result = new string(str);
            }
            return result;
        }

        /// <summary>
        /// Read a null-terminated UTF8 string.
        /// </summary>
        /// <returns>Returns the string that was read.</returns>
        public unsafe string ReadUTF8()
        {
            List<sbyte> chars = new List<sbyte>();
            sbyte ch;
            while (true)
            {
                ch = ReadSByte();
                if (ch == 0)
                    break;
                else
                    chars.Add(ch);
            }

            sbyte[] charss = chars.ToArray<sbyte>();
            fixed (sbyte* prt = charss)
                return new string(prt, 0, chars.Count, Encoding.UTF8);
        }

        public unsafe string ReadUTF8(int size)
        {
            sbyte[] chars = new sbyte[size];
            string result;
            fixed (sbyte* str = chars)
            {
                _stream.Read((byte[])(Array)chars, 0, size);
                result = new string(str, 0, size, Encoding.UTF8);
            }
            return result;
        }

        /// <summary>
        /// Reads a null-terminated UTF16-encoded string.
        /// </summary>
        /// <returns>The string that was read.</returns>
        public string ReadUTF16()
        {
            _currentString.Clear();
            int ch;
            while (true)
            {
                ch = ReadInt16();
                if (ch == 0)
                    break;
                _currentString.Append((char)ch);
            }
            return _currentString.ToString();
        }

        public string ReadUTF16(int size)
        {
            _currentString.Clear();
            int ch;
            while (_currentString.Length * 2 < size)
            {
                ch = ReadInt16();
                if (ch == 0)
                    break;
                _currentString.Append((char)ch);
            }
            Skip(size - _currentString.Length * 2);
            return _currentString.ToString();
        }

        /// <summary>
        /// Reads a block of data from the stream and returns it.
        /// </summary>
        /// <param name="size">The number of bytes to read</param>
        /// <returns>The bytes that were read</returns>
        public byte[] ReadBlock(int size)
        {
            byte[] result = new byte[size];
            _stream.Read(result, 0, size);
            return result;
        }

        /// <summary>
        /// Reads a block of data from the stream into an existing array.
        /// </summary>
        /// <param name="output">The array to read data into</param>
        /// <param name="offset">The array index to start reading to</param>
        /// <param name="size">The number of bytes to read</param>
        /// <returns>The number of bytes actually read</returns>
        public int ReadBlock(byte[] output, int offset, int size)
        {
            return _stream.Read(output, offset, size);
        }

        /// <summary>
        /// Returns whether or not we are at the end of the stream.
        /// </summary>
        public bool EOF
        {
            get
            {
                return (Position >= Length);
            }
        }

        /// <summary>
        /// Returns the current position of the reader.
        /// </summary>
        public long Position
        {
            get
            {
                return _stream.Position;
            }
        }

        /// <summary>
        /// Returns the total length of the stream.
        /// </summary>
        public long Length
        {
            get
            {
                return _stream.Length;
            }
        }

        /// <summary>
        /// The stream that this EndianReader is based off of.
        /// </summary>
        public Stream BaseStream
        {
            get { return _stream; }
        }
    }
}      
