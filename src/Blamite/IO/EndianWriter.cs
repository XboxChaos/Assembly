using System;
using System.IO;
using System.Text;

namespace Blamite.IO
{
	/// <summary>
	///     A stream which can be written to and whose endianness can be changed.
	/// </summary>
	public class EndianWriter : IWriter, IDisposable
	{
		private readonly byte[] _buffer = new byte[8];
		private readonly Stream _stream;
		private bool _bigEndian;

		/// <summary>
		///     Initializes a new instance of the <see cref="EndianWriter" /> class.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="endianness">The initial endianness to use when writing to the stream.</param>
		public EndianWriter(Stream stream, Endian endianness)
		{
			_stream = stream;
			_bigEndian = (endianness == Endian.BigEndian);
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
		///     Writes a byte to the stream.
		/// </summary>
		/// <param name="value">The byte to write.</param>
		public void WriteByte(byte value)
		{
			_buffer[0] = value;
			_stream.Write(_buffer, 0, 1);
		}

		/// <summary>
		///     Writes a signed byte to the stream.
		/// </summary>
		/// <param name="value">The signed byte to write.</param>
		public void WriteSByte(sbyte value)
		{
			WriteByte((byte) value);
		}

		/// <summary>
		///     Writes an unsigned 16-bit integer to the stream.
		/// </summary>
		/// <param name="value">The unsigned 16-bit integer to write.</param>
		public void WriteUInt16(ushort value)
		{
			if (_bigEndian)
			{
				_buffer[0] = (byte) (value >> 8);
				_buffer[1] = (byte) (value & 0xFF);
			}
			else
			{
				_buffer[0] = (byte) (value & 0xFF);
				_buffer[1] = (byte) (value >> 8);
			}
			_stream.Write(_buffer, 0, 2);
		}

		/// <summary>
		///     Writes a signed 16-bit integer to the stream.
		/// </summary>
		/// <param name="value">The signed 16-bit integer to write.</param>
		public void WriteInt16(short value)
		{
			WriteUInt16((ushort) value);
		}

		/// <summary>
		///     Writes an unsigned 32-bit integer to the stream.
		/// </summary>
		/// <param name="value">The unsigned 32-bit integer to write.</param>
		public void WriteUInt32(uint value)
		{
			if (_bigEndian)
			{
				_buffer[0] = (byte) (value >> 24);
				_buffer[1] = (byte) ((value >> 16) & 0xFF);
				_buffer[2] = (byte) ((value >> 8) & 0xFF);
				_buffer[3] = (byte) (value & 0xFF);
			}
			else
			{
				_buffer[0] = (byte) (value & 0xFF);
				_buffer[1] = (byte) ((value >> 8) & 0xFF);
				_buffer[2] = (byte) ((value >> 16) & 0xFF);
				_buffer[3] = (byte) (value >> 24);
			}
			_stream.Write(_buffer, 0, 4);
		}

		/// <summary>
		///     Writes a signed 32-bit integer to the stream.
		/// </summary>
		/// <param name="value">The signed 32-bit integer to write.</param>
		public void WriteInt32(int value)
		{
			WriteUInt32((uint) value);
		}

		/// <summary>
		///     Writes an unsigned 64-bit integer to the stream.
		/// </summary>
		/// <param name="value">The unsigned 64-bit integer to write.</param>
		public void WriteUInt64(ulong value)
		{
			if (_bigEndian)
			{
				_buffer[0] = (byte) (value >> 56);
				_buffer[1] = (byte) ((value >> 48) & 0xFF);
				_buffer[2] = (byte) ((value >> 40) & 0xFF);
				_buffer[3] = (byte) ((value >> 32) & 0xFF);
				_buffer[4] = (byte) ((value >> 24) & 0xFF);
				_buffer[5] = (byte) ((value >> 16) & 0xFF);
				_buffer[6] = (byte) ((value >> 8) & 0xFF);
				_buffer[7] = (byte) (value & 0xFF);
			}
			else
			{
				_buffer[0] = (byte) (value & 0xFF);
				_buffer[1] = (byte) ((value >> 8) & 0xFF);
				_buffer[2] = (byte) ((value >> 16) & 0xFF);
				_buffer[3] = (byte) ((value >> 24) & 0xFF);
				_buffer[4] = (byte) ((value >> 32) & 0xFF);
				_buffer[5] = (byte) ((value >> 40) & 0xFF);
				_buffer[6] = (byte) ((value >> 48) & 0xFF);
				_buffer[7] = (byte) (value >> 56);
			}
			_stream.Write(_buffer, 0, 8);
		}

		/// <summary>
		///     Writes a signed 64-bit integer to the stream.
		/// </summary>
		/// <param name="value">The signed 64-bit integer to write.</param>
		public void WriteInt64(long value)
		{
			WriteUInt64((ulong) value);
		}

		/// <summary>
		///     Writes a 32-bit floating-point value to the stream.
		/// </summary>
		/// <param name="value">The 32-bit floating-point value to write.</param>
		public void WriteFloat(float value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian == _bigEndian)
			{
				// Is there a faster way to do this?
				byte temp = bytes[0];
				bytes[0] = bytes[3];
				bytes[3] = temp;
				temp = bytes[1];
				bytes[1] = bytes[2];
				bytes[2] = temp;
			}
			_stream.Write(bytes, 0, bytes.Length);
		}

		/// <summary>
		///     Writes an ASCII string to the stream, followed by a null terminator.
		/// </summary>
		/// <param name="str">The ASCII string to write.</param>
		public void WriteAscii(string str)
		{
			byte[] bytes = Encoding.GetEncoding(28591).GetBytes(str);// using Latin1 for the full 8bit range
			WriteBlock(bytes);
			WriteByte(0);
		}

        /// <summary>
        ///     Writes an ASCII string to the stream and pads it with trailing null bytes to the specified length.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="length"></param>
        public void WriteAscii(string str, int length)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(str);

            if(bytes.Length >= length)
            {
                throw new ArgumentException("The strings exceeds the specified length.");
            }

            WriteBlock(bytes);
            int padding = length - bytes.Length;
            for(int i = 0; i < padding; i++)
            {
                WriteByte(0);
            }
        }

        /// <summary>
        ///     Writes a Windows-1252 string to the stream, followed by a null terminator.
        /// </summary>
        /// <param name="str">The Windows-1252 string to write.</param>
        public void WriteWin1252(string str)
        {
            var enc = Encoding.GetEncoding(1252);
            byte[] bytes = enc.GetBytes(str);
            WriteBlock(bytes);
            WriteByte(0);
        }

        /// <summary>
        ///     Writes a UTF-8 string to the stream, followed by a null terminator.
        /// </summary>
        /// <param name="str">The UTF-8 string to write.</param>
        public void WriteUTF8(string str)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(str);
			WriteBlock(bytes);
			WriteByte(0);
		}

		/// <summary>
		///     Writes a UTF-16 string to the stream, followed by a null terminator.
		/// </summary>
		/// <param name="str">The UTF-16 string to write.</param>
		public void WriteUTF16(string str)
		{
			foreach (char ch in str)
				WriteInt16((short) ch);
			WriteInt16(0x0000);
		}

		/// <summary>
		///     Writes an array of bytes to the stream.
		/// </summary>
		/// <param name="data">The bytes to write.</param>
		public void WriteBlock(byte[] data)
		{
			_stream.Write(data, 0, data.Length);
		}

		/// <summary>
		///     Writes an array of bytes to the stream.
		/// </summary>
		/// <param name="data">The bytes to write.</param>
		/// <param name="offset">The starting index in the array to write.</param>
		/// <param name="size">The number of bytes to write.</param>
		public void WriteBlock(byte[] data, int offset, int size)
		{
			_stream.Write(data, offset, size);
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
		///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			_stream.Dispose();
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
