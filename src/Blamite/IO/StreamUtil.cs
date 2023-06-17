using System;
using System.IO;

namespace Blamite.IO
{
	public sealed class StreamUtil
	{
		/// <summary>
		///     Copies data between two different streams.
		/// </summary>
		/// <param name="input">The stream to read from.</param>
		/// <param name="output">The stream to copy the read data to.</param>
		public static void Copy(IReader input, IWriter output)
		{
			// http://stackoverflow.com/questions/230128/best-way-to-copy-between-two-stream-instances-c-sharp
			const int BufferSize = 81920;
			var buffer = new byte[BufferSize];
			int read;
			while ((read = input.ReadBlock(buffer, 0, BufferSize)) > 0)
				output.WriteBlock(buffer, 0, read);
		}

		/// <summary>
		///     Copies data between two different streams.
		/// </summary>
		/// <param name="input">The stream to read from.</param>
		/// <param name="output">The stream to copy the read data to.</param>
		/// <param name="size">The size of the data to copy.</param>
		public static void Copy(IReader input, IWriter output, int size)
		{
			const int BufferSize = 81920;
			var buffer = new byte[BufferSize];
			while (size > 0)
			{
				int read = input.ReadBlock(buffer, 0, Math.Min(BufferSize, size));
				output.WriteBlock(buffer, 0, read);
				size -= BufferSize;
			}
		}

		/// <summary>
		///     Copies data between two different streams.
		/// </summary>
		/// <param name="input">The stream to read from.</param>
		/// <param name="output">The stream to copy the read data to.</param>
		/// <param name="size">The size of the data to copy.</param>
		public static void Copy(Stream input, Stream output, int size)
		{
			const int BufferSize = 81920;
			var buffer = new byte[BufferSize];
			while (size > 0)
			{
				int read = input.Read(buffer, 0, Math.Min(BufferSize, size));
				output.Write(buffer, 0, read);
				size -= BufferSize;
			}
		}

		/// <summary>
		///     Copies data between two locations in the same stream.
		///     The source and destination areas may overlap.
		/// </summary>
		/// <param name="stream">The stream to copy data in.</param>
		/// <param name="originalPos">The position of the block of data to copy.</param>
		/// <param name="targetPos">The position to copy the block to.</param>
		/// <param name="size">The number of bytes to copy.</param>
		public static void Copy(IStream stream, long originalPos, long targetPos, long size)
		{
			if (size == 0)
				return;
			if (size < 0)
				throw new ArgumentException("The size of the data to copy must be >= 0");

			const int BufferSize = 81920;
			var buffer = new byte[BufferSize];
			long remaining = size;
			while (remaining > 0)
			{
				var read = (int) Math.Min(BufferSize, remaining);

				if (targetPos > originalPos)
					stream.SeekTo(originalPos + remaining - read);
				else
					stream.SeekTo(originalPos + size - remaining);

				stream.ReadBlock(buffer, 0, read);

				if (targetPos > originalPos)
					stream.SeekTo(targetPos + remaining - read);
				else
					stream.SeekTo(targetPos + size - remaining);

				stream.WriteBlock(buffer, 0, read);
				remaining -= read;
			}
		}

		/// <summary>
		///     Inserts space into a stream by copying everything back by a certain number of bytes.
		/// </summary>
		/// <param name="stream">The stream to insert space into.</param>
		/// <param name="size">The size of the space to insert.</param>
		/// <param name="fill">The byte to fill the inserted space with. See <see cref="Fill" />.</param>
		public static void Insert(IStream stream, uint size, byte fill)
		{
			if (size == 0)
				return;
			if (size < 0)
				throw new ArgumentException("The size of the data to insert must be >= 0");

			long startPos = stream.Position;
			if (startPos < stream.Length)
			{
				Copy(stream, startPos, startPos + size, stream.Length - startPos);
				stream.SeekTo(startPos);
			}
			Fill(stream, fill, size);
		}

		/// <summary>
		///     Fills a section of a stream with a repeating byte.
		/// </summary>
		/// <param name="writer">The IWriter to fill a section of.</param>
		/// <param name="b">The byte to fill the section with.</param>
		/// <param name="size">The size of the section to fill.</param>
		public static void Fill(IWriter writer, byte b, uint size)
		{
			if (size == 0)
				return;
			if (size < 0)
				throw new ArgumentException("The size of the data to insert must be >= 0");

			const int BufferSize = 81920;
			var buffer = new byte[BufferSize];
			long pos = writer.Position;
			long endPos = pos + size;

			// Fill the buffer
			if (b != 0)
			{
				for (int i = 0; i < buffer.Length; i++)
					buffer[i] = b;
			}

			// Write it
			while (pos < endPos)
			{
				writer.WriteBlock(buffer, 0, (int) Math.Min(endPos - pos, BufferSize));
				pos += BufferSize;
			}
		}

		/// <summary>
		///     Expands an area in a stream to ensure that it is large enough to hold a given amount of data.
		/// </summary>
		/// <param name="stream">The stream to expand.</param>
		/// <param name="startOffset">The start offset of the area that needs to be expanded.</param>
		/// <param name="originalEndOffset">The original end offset of the area that needs to be expanded.</param>
		/// <param name="requestedSize">The size of the data that needs to fit in the defined area.</param>
		/// <param name="pageSize">The size of each page that should be injected into the stream.</param>
		/// <returns>The number of bytes inserted into the stream at originalEndOffset (or startOffset if it's greater).</returns>
		public static int MakeFreeSpace(IStream stream, long startOffset, long originalEndOffset, long requestedSize,
			int pageSize)
		{
			originalEndOffset = Math.Max(originalEndOffset, startOffset);

			// Calculate the number of bytes that the requested size overflows the area by,
			// and then insert pages if necessary
			var overflow = (int) (startOffset + requestedSize - originalEndOffset);
			if (overflow > 0)
				return InsertPages(stream, originalEndOffset, overflow, pageSize);
			return 0;
		}

		/// <summary>
		///     Inserts empty pages into a stream so that a specified amount of data can fit, pushing everything past them back.
		/// </summary>
		/// <param name="stream">The stream to insert pages into.</param>
		/// <param name="offset">The offset to insert the pages at.</param>
		/// <param name="minSpace">The minimum amount of free space that needs to be available after the pages have been inserted.</param>
		/// <param name="pageSize">The size of each page to insert.</param>
		/// <returns>The number of bytes that were inserted into the stream at the specified offset.</returns>
		/// <seealso cref="Insert" />
		public static int InsertPages(IStream stream, long offset, int minSpace, int pageSize)
		{
			// Round the minimum space up to the next multiple of the page size
			minSpace = (minSpace + pageSize - 1) & ~(pageSize - 1);

			// Push the data back by that amount
			stream.SeekTo(offset);
			Insert(stream, (uint)minSpace, 0);

			return minSpace;
		}
	}
}