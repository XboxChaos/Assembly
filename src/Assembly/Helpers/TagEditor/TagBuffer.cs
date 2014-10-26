using Blamite.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.TagEditor
{
	/// <summary>
	/// Caches tag data read from a stream.
	/// </summary>
	public class TagBuffer : IDisposable
	{
		private MemoryStream _memStream;

		/// <summary>
		/// Initializes a new instance of the <see cref="TagBuffer"/> class from tag data.
		/// </summary>
		/// <param name="data">The tag data to store in the buffer.</param>
		/// <param name="endianness">The endianness of the data.</param>
		/// <param name="sourceLocation">The source location.</param>
		public TagBuffer(byte[] data, Endian endianness, SegmentPointer sourceLocation)
		{
			_memStream = new MemoryStream(data);
			Stream = new EndianStream(_memStream, endianness);
			Location = sourceLocation;
		}

		/// <summary>
		/// Gets the location that the tag buffer originated from in the map file.
		/// </summary>
		public SegmentPointer Location { get; private set; }

		/// <summary>
		/// Gets a stream which can be used to read data stored in the tag buffer.
		/// </summary>
		public IStream Stream { get; private set; }

		/// <summary>
		/// Creates a deep clone of the tag buffer such that editing the clone will not edit this buffer.
		/// </summary>
		/// <returns>The clone.</returns>
		public TagBuffer DeepClone()
		{
			var cloneData = new byte[_memStream.Length];
			Buffer.BlockCopy(_memStream.GetBuffer(), 0, cloneData, 0, cloneData.Length);
			return new TagBuffer(cloneData, Stream.Endianness, Location);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (Stream != null)
				{
					Stream.Dispose();
					Stream = null;
				}
			}
		}

		/// <summary>
		/// Finalizes an instance of the <see cref="TagBuffer"/> class.
		/// </summary>
		~TagBuffer()
		{
			Dispose(false);
		}
	}
}
