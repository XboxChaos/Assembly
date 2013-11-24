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
	///     Interface for a stream which can be read from.
	/// </summary>
	public interface IReader : IBaseStream
	{
		/// <summary>
		///     Reads an array of bytes from the stream.
		/// </summary>
		/// <param name="output">The array to store the read bytes to.</param>
		/// <param name="offset">The starting index in the array to read to.</param>
		/// <param name="size">The number of bytes to read.</param>
		/// <returns>The number of bytes that were actually read.</returns>
		int ReadBlock(byte[] output, int offset, int size);

		/// <summary>
		///     Reads an array of bytes from the stream.
		/// </summary>
		/// <param name="size">The number of bytes to read.</param>
		/// <returns>The bytes that were read.</returns>
		byte[] ReadBlock(int size);

		/// <summary>
		///     Reads a byte from the stream.
		/// </summary>
		/// <returns>The byte that was read.</returns>
		byte ReadByte();

		/// <summary>
		///     Reads a signed byte from the stream.
		/// </summary>
		/// <returns>The signed byte that was read.</returns>
		sbyte ReadSByte();

		/// <summary>
		///     Reads a 16-bit signed integer from the stream.
		/// </summary>
		/// <returns>The 16-bit signed integer that was read.</returns>
		short ReadInt16();

		/// <summary>
		///     Reads a 16-bit unsigned integer from the stream.
		/// </summary>
		/// <returns>The 16-bit unsigned integer that was read.</returns>
		ushort ReadUInt16();

		/// <summary>
		///     Reads a 32-bit signed integer from the stream.
		/// </summary>
		/// <returns>The 32-bit signed integer that was read.</returns>
		int ReadInt32();

		/// <summary>
		///     Reads a 32-bit unsigned integer from the stream.
		/// </summary>
		/// <returns>The 32-bit unsigned integer that was read.</returns>
		uint ReadUInt32();

		/// <summary>
		///     Reads a 64-bit signed integer from the stream.
		/// </summary>
		/// <returns>The 64-bit signed integer that was read.</returns>
		long ReadInt64();

		/// <summary>
		///     Reads a 64-bit unsigned integer from the stream.
		/// </summary>
		/// <returns>The 64-bit unsigned integer that was read.</returns>
		ulong ReadUInt64();

		/// <summary>
		///     Reads a 32-bit floating-point value from the stream.
		/// </summary>
		/// <returns>The 32-bit floating-point value that was read.</returns>
		float ReadFloat();

		/// <summary>
		///     Reads a null-terminated ASCII string from the stream.
		/// </summary>
		/// <returns>The ASCII string that was read.</returns>
		string ReadAscii();

		/// <summary>
		///     Reads a fixed-size null-terminated ASCII string from the stream.
		/// </summary>
		/// <param name="size">The size of the string to read, including the null terminator.</param>
		/// <returns>The ASCII string that was read, with any 0 padding bytes stripped.</returns>
		string ReadAscii(int size);

		/// <summary>
		///     Reads a null-terminated UTF-8 string from the stream.
		/// </summary>
		/// <returns>The null-terminated UTF-8 string that was read.</returns>
		string ReadUTF8();

		/// <summary>
		///     Reads a fixed-size null-terminated UTF-8 string from the stream.
		/// </summary>
		/// <param name="size">The size in bytes of the string to read, including the null terminator.</param>
		/// <returns>The UTF-8 string that was read, with any padding bytes stripped.</returns>
		string ReadUTF8(int size);

		/// <summary>
		///     Reads a null-terminated UTF-16 string from the stream.
		/// </summary>
		/// <returns>The UTF-16 string that was read.</returns>
		string ReadUTF16();

		/// <summary>
		///     Reads a fixed-size null-terminated UTF-16 string from the stream.
		/// </summary>
		/// <param name="size">The size in bytes of the string to read, including the null terminator.</param>
		/// <returns>The UTF-16 string that was read, with any padding bytes stripped.</returns>
		string ReadUTF16(int size);
	}
}