using Blamite.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.TagEditor.Buffering
{
	/// <summary>
	/// A <see cref="TagBufferSource"/> which loads and caches tag buffer data from a stream.
	/// </summary>
	public class StreamTagBufferSource : TagBufferSource
	{
		private Dictionary<object, TagBufferSource> _linkedSources = new Dictionary<object, TagBufferSource>();
		private TagBuffer _cache;
		private IStreamManager _streamManager;
		private SegmentPointer _location;
		private ulong _size;

		/// <summary>
		/// Initializes a new instance of the <see cref="StreamTagBufferSource"/> class.
		/// </summary>
		/// <param name="stream">The stream manager to use.</param>
		/// <param name="location">The location of the data within the stream.</param>
		/// <param name="size">The size of the data in bytes.</param>
		public StreamTagBufferSource(IStreamManager stream, SegmentPointer location, ulong size)
		{
			_streamManager = stream;
			_location = location;
			_size = size;
		}

		/// <summary>
		/// Gets or sets the location where data will be read from.
		/// Changing this value will invalidate the buffer cache.
		/// </summary>
		public SegmentPointer Location
		{
			get { return _location; }
			set
			{
				_location = value;
				InvalidateCache();
			}
		}

		/// <summary>
		/// Gets the currently-active tag buffer.
		/// </summary>
		/// <returns>
		/// The currently-active tag buffer, or <c>null</c> if none.
		/// </returns>
		public override TagBuffer GetActiveBuffer()
		{
			if (_cache == null)
				ReadBuffer();
			return _cache;
		}

		/// <summary>
		/// Invalidates the cache, causing the next buffer retrieval to read from the stream.
		/// </summary>
		public void InvalidateCache()
		{
			if (_cache != null)
			{
				_cache.Dispose();
				_cache = null;
			}
			OnActiveBufferChanged();
		}

		/// <summary>
		/// Reads the tag buffer from the stream and caches it.
		/// </summary>
		private void ReadBuffer()
		{
			byte[] data;
			Endian endianness;
			using (var reader = _streamManager.OpenRead())
			{
				endianness = reader.Endianness;
				reader.SeekTo(_location.AsOffset());
				data = reader.ReadBlock((int)_size);
			}
			_cache = new TagBuffer(data, endianness, _location);
		}
	}
}
