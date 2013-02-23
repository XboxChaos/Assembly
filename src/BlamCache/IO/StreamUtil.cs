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

namespace ExtryzeDLL.IO
{
    public sealed class StreamUtil
    {
        /// <summary>
        /// Copies data between two different streams.
        /// </summary>
        /// <param name="input">The stream to read from.</param>
        /// <param name="output">The stream to copy the read data to.</param>
        public static void Copy(IReader input, IWriter output)
        {
            // http://stackoverflow.com/questions/230128/best-way-to-copy-between-two-stream-instances-c-sharp
            const int BufferSize = 0x1000;
            byte[] buffer = new byte[BufferSize];
            int read;
            while ((read = input.ReadBlock(buffer, 0, BufferSize)) > 0)
                output.WriteBlock(buffer, 0, read);
        }

        /// <summary>
        /// Copies data between two different streams.
        /// </summary>
        /// <param name="input">The stream to read from.</param>
        /// <param name="output">The stream to copy the read data to.</param>
        /// <param name="size">The size of the data to copy.</param>
        public static void Copy(IReader input, IWriter output, int size)
        {
            const int BufferSize = 0x1000;
            byte[] buffer = new byte[BufferSize];
            while (size > 0)
            {
                int read = input.ReadBlock(buffer, 0, Math.Min(BufferSize, size));
                output.WriteBlock(buffer, 0, read);
                size -= BufferSize;
            }
        }

        /// <summary>
        /// Copies data between two different streams.
        /// </summary>
        /// <param name="input">The stream to read from.</param>
        /// <param name="output">The stream to copy the read data to.</param>
        /// <param name="size">The size of the data to copy.</param>
        public static void Copy(Stream input, Stream output, int size)
        {
            const int BufferSize = 0x1000;
            byte[] buffer = new byte[BufferSize];
            while (size > 0)
            {
                int read = input.Read(buffer, 0, Math.Min(BufferSize, size));
                output.Write(buffer, 0, read);
                size -= BufferSize;
            }
        }

        /// <summary>
        /// Inserts space into a stream by copying everything back by a certain number of bytes.
        /// </summary>
        /// <param name="stream">The stream to insert space into.</param>
        /// <param name="size">The size of the space to insert.</param>
        /// <param name="fill">The byte to fill the inserted space with. See <see cref="Fill"/>.</param>
        public static void Insert(IStream stream, int size, byte fill)
        {
            if (size == 0)
                return;
            if (size < 0)
                throw new ArgumentException("The size of the data to insert must be >= 0");
            if (stream.Position == stream.Length)
                return; // Seeking past the end automatically increases the file size

            const int BufferSize = 0x1000;
            byte[] buffer = new byte[BufferSize];
            int endPos = (int)stream.Position;
            int oldLength = (int)stream.Length;
            int pos = Math.Max(endPos, oldLength - BufferSize);
            while (pos >= endPos)
            {
                int read = Math.Min(BufferSize, oldLength - pos);
                stream.SeekTo(pos);
                stream.ReadBlock(buffer, 0, read);

                stream.SeekTo(pos + size);
                stream.WriteBlock(buffer, 0, read);
                pos -= read;
            }

            stream.SeekTo(endPos);
            Fill(stream, fill, size);
        }

        /// <summary>
        /// Fills a section of a stream with a repeating byte.
        /// </summary>
        /// <param name="writer">The IWriter to fill a section of.</param>
        /// <param name="b">The byte to fill the section with.</param>
        /// <param name="size">The size of the section to fill.</param>
        public static void Fill(IWriter writer, byte b, int size)
        {
            if (size == 0)
                return;
            if (size < 0)
                throw new ArgumentException("The size of the data to insert must be >= 0");
            if (writer.Position == writer.Length)
                return;

            const int BufferSize = 0x1000;
            byte[] buffer = new byte[BufferSize];
            long length = writer.Length;
            long pos = writer.Position;
            int filled = 0;

            // Fill the buffer
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = b;

            // Write it
            while (filled < size)
            {
                writer.WriteBlock(buffer, 0, (int)Math.Min(length - pos, BufferSize));
                filled += BufferSize;
                pos += BufferSize;
            }
        }

        /// <summary>
        /// Expands an area in a stream to ensure that it is large enough to hold a given amount of data.
        /// </summary>
        /// <param name="stream">The stream to expand.</param>
        /// <param name="startOffset">The start offset of the area that needs to be expanded.</param>
        /// <param name="originalEndOffset">The original end offset of the area that needs to be expanded.</param>
        /// <param name="requestedSize">The size of the data that needs to fit in the defined area.</param>
        /// <param name="pageSize">The size of each page that should be injected into the stream.</param>
        /// <returns>The number of bytes inserted into the stream at originalEndOffset (or startOffset if it's greater).</returns>
        public static int MakeFreeSpace(IStream stream, long startOffset, long originalEndOffset, long requestedSize, int pageSize)
        {
            originalEndOffset = Math.Max(originalEndOffset, startOffset);

            // Calculate the number of bytes that the requested size overflows the area by,
            // and then insert pages if necessary
            int overflow = (int)(startOffset + requestedSize - originalEndOffset);
            if (overflow > 0)
                return InsertPages(stream, originalEndOffset, overflow, pageSize);
            return 0;
        }

        /// <summary>
        /// Inserts empty pages into a stream so that a specified amount of data can fit, pushing everything past them back.
        /// </summary>
        /// <param name="stream">The stream to insert pages into.</param>
        /// <param name="offset">The offset to insert the pages at.</param>
        /// <param name="minSpace">The minimum amount of free space that needs to be available after the pages have been inserted.</param>
        /// <param name="pageSize">The size of each page to insert.</param>
        /// <returns>The number of bytes that were inserted into the stream at the specified offset.</returns>
        /// <seealso cref="Insert"/>
        public static int InsertPages(IStream stream, long offset, int minSpace, int pageSize)
        {
            // Round the minimum space up to the next multiple of the page size
            minSpace = (minSpace + pageSize - 1) & ~(pageSize - 1);

            // Push the data back by that amount
            stream.SeekTo(offset);
            StreamUtil.Insert(stream, minSpace, 0);

            return minSpace;
        }
    }
}      
