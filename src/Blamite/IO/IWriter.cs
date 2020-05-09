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
        ///     Writes an ASCII string to the stream and pads it with trailing null bytes to the specified length.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="length"></param>
        void WriteAscii(string str, int length);

        /// <summary>
        ///     Writes a Windows-1252 string to the stream, followed by a null terminator.
        /// </summary>
        /// <param name="str">The Windows-1252 string to write.</param>
        void WriteWin1252(string str);

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