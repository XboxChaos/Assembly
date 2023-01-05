using System;
using System.Collections.Generic;
using Blamite.IO;

namespace Blamite.Patching
{
	public static class DataComparer
	{
		/// <summary>
		///     Compares parts of two streams.
		/// </summary>
		/// <param name="originalReader">The stream open on the original file.</param>
		/// <param name="originalOffset">The start offset of the data to compare in the original file.</param>
		/// <param name="originalSize">The size of the data to compare in the original file.</param>
		/// <param name="newReader">The stream open on the modified file.</param>
		/// <param name="newOffset">The start offset of the data to compare in the modified file.</param>
		/// <param name="newSize">The size of the data to compare in the modified file.</param>
		/// <param name="extraDataAtEnd">
		///     true if extra data in the modified file will be found at the end of the area to compare,
		///     or false if it will be found at the beginning.
		/// </param>
		/// <returns>The differences between the two parts of the files.</returns>
		public static List<DataChange> CompareData(IReader originalReader, uint originalOffset, uint originalSize,
			IReader newReader, uint newOffset, uint newSize, bool extraDataAtEnd)
		{
			var results = new List<DataChange>();

			const int BufferSize = 0x1000;
			var oldBuffer = new byte[BufferSize];
			var newBuffer = new byte[BufferSize];

			uint sizeDiff = newSize - originalSize;
			if (sizeDiff < 0)
				throw new NotSupportedException("Comparing shrunk segments is not supported yet");

			// Account for added data
			if (sizeDiff > 0)
			{
				if (extraDataAtEnd)
					newReader.SeekTo(newOffset + originalSize);
				else
					newReader.SeekTo(newOffset);

				var offset = (uint) (newReader.Position - newOffset);
				byte[] data = newReader.ReadBlock((int)sizeDiff);
				results.Add(new DataChange(offset, data));
			}

			// Now handle differences
			originalReader.SeekTo(originalOffset);
			if (extraDataAtEnd)
				newReader.SeekTo(newOffset);
			else
				newReader.SeekTo(newOffset + sizeDiff);

			var originalEndOffset = (uint) (originalOffset + originalSize);
			var newEndOffset = (uint) (newOffset + newSize);
			var bufferOffset = (uint) (newReader.Position - newOffset);

			while (originalOffset < originalEndOffset && newOffset < newEndOffset)
			{
				int diffStartIndex = 0;
				uint diffOffset = 0;
				int diffSize = 0;

				// Read the meta in large blocks and then compare the blocks
				originalReader.ReadBlock(oldBuffer, 0, (int) Math.Min(BufferSize, originalEndOffset - originalOffset));
				newReader.ReadBlock(newBuffer, 0, (int) Math.Min(BufferSize, newEndOffset - newOffset));
				int bufferLength = Math.Min(oldBuffer.Length, newBuffer.Length);

				for (int i = 0; i < bufferLength; i++)
				{
					if (oldBuffer[i] != newBuffer[i])
					{
						if (diffSize == 0)
						{
							diffStartIndex = i;
							diffOffset = (uint) (bufferOffset + i);
						}
						diffSize++;
					}
					else if (diffSize > 0)
					{
						// Found a complete difference region - build data for the change and add it
						results.Add(BuildChange(newBuffer, diffStartIndex, diffOffset, diffSize));
						diffSize = 0;
					}
				}

				// Handle differences at the end of the buffer
				if (diffSize > 0)
					results.Add(BuildChange(newBuffer, diffStartIndex, diffOffset, diffSize));

				// Advance to the next block
				bufferOffset += (uint) bufferLength;
				originalOffset += (uint) oldBuffer.Length;
				newOffset += (uint) newBuffer.Length;
			}

			return results;
		}

		private static DataChange BuildChange(byte[] diffBuffer, int diffStartIndex, uint diffOffset, int diffSize)
		{
			// Copy the differing bytes to a buffer
			var diff = new byte[diffSize];
			Array.Copy(diffBuffer, diffStartIndex, diff, 0, diffSize);

			return new DataChange(diffOffset, diff);
		}
	}
}