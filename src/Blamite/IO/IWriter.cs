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

namespace Blamite.IO
{
	/// <summary>
	///     Interface for a stream which can be written to.
	/// </summary>
	public interface IWriter : IBaseStream
	{
		/// <summary>
		///     Writes an array of bytes to the stream.
		/// </summary>
		/// <param name="data">The bytes to write.</param>
		void WriteBlock(byte[] data);

		/// <summary>
		///     Writes an array of bytes to the stream.
		/// </summary>
		/// <param name="data">The bytes to write.</param>
		/// <param name="offset">The starting index in the array to write.</param>
		/// <param name="size">The number of bytes to write.</param>
		void WriteBlock(byte[] data, int offset, int size);

		/// <summary>
		///     Writes a byte to the stream.
		/// </summary>
		/// <param name="value">The byte to write.</param>
		void WriteByte(byte value);

		/// <summary>
		///     Writes a signed byte to the stream.
		/// </summary>
		/// <param name="value">The signed byte to write.</param>
		void WriteSByte(sbyte value);

		/// <summary>
		///     Writes a signed 16-bit integer to the stream.
		/// </summary>
		/// <param name="value">The signed 16-bit integer to write.</param>
		void WriteInt16(short value);

		/// <summary>
		///     Writes an unsigned 16-bit integer to the stream.
		/// </summary>
		/// <param name="value">The unsigned 16-bit integer to write.</param>
		void WriteUInt16(ushort value);

		/// <summary>
		///     Writes a signed 32-bit integer to the stream.
		/// </summary>
		/// <param name="value">The signed 32-bit integer to write.</param>
		void WriteInt32(int value);

		/// <summary>
		///     Writes an unsigned 32-bit integer to the stream.
		/// </summary>
		/// <param name="value">The unsigned 32-bit integer to write.</param>
		void WriteUInt32(uint value);

		/// <summary>
		///     Writes a signed 64-bit integer to the stream.
		/// </summary>
		/// <param name="value">The signed 64-bit integer to write.</param>
		void WriteInt64(long value);

		/// <summary>
		///     Writes an unsigned 64-bit integer to the stream.
		/// </summary>
		/// <param name="value">The unsigned 64-bit integer to write.</param>
		void WriteUInt64(ulong value);

		/// <summary>
		///     Writes a 32-bit floating-point value to the stream.
		/// </summary>
		/// <param name="value">The 32-bit floating-point value to write.</param>
		void WriteFloat(float value);

		/// <summary>
		///     Writes an ASCII string to the stream, followed by a null terminator.
		/// </summary>
		/// <param name="str">The ASCII string to write.</param>
		void WriteAscii(string str);

		/// <summary>
		///     Writes a UTF-8 string to the stream, followed by a null terminator.
		/// </summary>
		/// <param name="str">The UTF-8 string to write.</param>
		void WriteUTF8(string str);

		/// <summary>
		///     Writes a UTF-16 string to the stream, followed by a null terminator.
		/// </summary>
		/// <param name="str">The UTF-16 string to write.</param>
		void WriteUTF16(string str);
	}
}