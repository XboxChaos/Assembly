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

namespace ExtryzeDLL.IO
{
    /// <summary>
    /// Encapsulates an EndianReader and EndianWriter to allow for easy bi-directional access to a binary stream.
    /// </summary>
    public class EndianStream : IDisposable, IReader, IWriter
    {
        /// <summary>
        /// Constructs a new EndianStream based off of a Stream object.
        /// </summary>
        /// <param name="stream">The Stream to use. It must at least be available for reading or writing.</param>
        /// <param name="endianness">The endianness to use when performing I/O operations.</param>
        public EndianStream(Stream stream, Endian endianness)
        {
            _stream = stream;
            if (stream.CanRead)
                _reader = new EndianReader(stream, endianness);
            if (stream.CanWrite)
                _writer = new EndianWriter(stream, endianness);

            if (_reader == null && _writer == null)
                throw new ArgumentException("The input stream cannot be read from or written to.");
        }

        public Endian Endianness
        {
            get
            {
                if (_reader != null)
                    return _reader.Endianness;
                return _writer.Endianness;
            }
            set
            {
                if (_reader != null)
                    _reader.Endianness = value;
                if (_writer != null)
                    _writer.Endianness = value;
            }
        }

        public void Close()
        {
            if (_reader != null)
                _reader.Close();
            if (_writer != null)
                _writer.Close();
        }

        public void Dispose()
        {
            Close();
        }

        public byte ReadByte()
        {
            return _reader.ReadByte();
        }

        public void WriteByte(byte b)
        {
            _writer.WriteByte(b);
        }

        public sbyte ReadSByte()
        {
            return _reader.ReadSByte();
        }

        public void WriteSByte(sbyte b)
        {
            _writer.WriteSByte(b);
        }

        public ushort ReadUInt16()
        {
            return _reader.ReadUInt16();
        }

        public void WriteUInt16(ushort value)
        {
            _writer.WriteUInt16(value);
        }

        public short ReadInt16()
        {
            return _reader.ReadInt16();
        }

        public void WriteInt16(short value)
        {
            _writer.WriteInt16(value);
        }

        public uint ReadUInt32()
        {
            return _reader.ReadUInt32();
        }

        public void WriteUInt32(uint value)
        {
            _writer.WriteUInt32(value);
        }

        public int ReadInt32()
        {
            return _reader.ReadInt32();
        }

        public void WriteInt32(int value)
        {
            _writer.WriteInt32(value);
        }

        public ulong ReadUInt64()
        {
            return _reader.ReadUInt64();
        }

        public void WriteUInt64(ulong value)
        {
            _writer.WriteUInt64(value);
        }

        public long ReadInt64()
        {
            return _reader.ReadInt64();
        }

        public void WriteInt64(long value)
        {
            _writer.WriteInt64(value);
        }

        public float ReadFloat()
        {
            return _reader.ReadFloat();
        }

        public void WriteFloat(float value)
        {
            _writer.WriteFloat(value);
        }

        public string ReadAscii()
        {
            return _reader.ReadAscii();
        }

        public string ReadAscii(int size)
        {
            return _reader.ReadAscii(size);
        }

        public void WriteAscii(string str)
        {
            _writer.WriteAscii(str);
        }

        public string ReadUTF8()
        {
            return _reader.ReadUTF8();
        }

        public string ReadUTF8(int size)
        {
            return _reader.ReadUTF8(size);
        }

        public string ReadUTF16()
        {
            return _reader.ReadUTF16();
        }

        public string ReadUTF16(int length)
        {
            return _reader.ReadUTF16(length);
        }

        public void WriteUTF16(string str)
        {
            _writer.WriteUTF16(str);
        }

        public byte[] ReadBlock(int size)
        {
            return _reader.ReadBlock(size);
        }

        public int ReadBlock(byte[] output, int offset, int size)
        {
            return _reader.ReadBlock(output, offset, size);
        }

        public void WriteBlock(byte[] data)
        {
            _writer.WriteBlock(data);
        }

        public void WriteBlock(byte[] data, int offset, int size)
        {
            _writer.WriteBlock(data, offset, size);
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
        /// Returns the current position of the stream.
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
        /// The stream that this EndianStream is based off of.
        /// </summary>
        public Stream BaseStream
        {
            get { return _stream; }
        }

        private Stream _stream = null;
        private EndianReader _reader = null;
        private EndianWriter _writer = null;
    }
}      
