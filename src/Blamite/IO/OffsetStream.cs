using System.IO;

namespace Blamite.IO
{
	/// <summary>
	///     A wrapper for a Stream which shifts its seek offsets by a specified amount.
	/// </summary>
	public class OffsetStream : Stream
	{
		private readonly Stream _baseStream;
		private readonly long _offset;

		/// <summary>
		///     Constructs a new OffsetStream based off of another stream.
		/// </summary>
		/// <param name="baseStream">The underlying stream.</param>
		/// <param name="offset">The offset that should correspond to position 0 in the base stream.</param>
		public OffsetStream(Stream baseStream, long offset)
		{
			_baseStream = baseStream;
			_offset = offset;
		}

		public override bool CanRead
		{
			get { return _baseStream.CanRead; }
		}

		public override bool CanSeek
		{
			get { return _baseStream.CanSeek; }
		}

		public override bool CanWrite
		{
			get { return _baseStream.CanWrite; }
		}

		public override long Length
		{
			get { return _baseStream.Length - _offset; }
		}

		public override long Position
		{
			get { return _baseStream.Position - _offset; }
			set { _baseStream.Position = value + _offset; }
		}

		public override bool CanTimeout
		{
			get { return _baseStream.CanTimeout; }
		}

		public override int ReadTimeout
		{
			get { return _baseStream.ReadTimeout; }
			set { _baseStream.ReadTimeout = value; }
		}

		public override int WriteTimeout
		{
			get { return _baseStream.WriteTimeout; }
			set { _baseStream.WriteTimeout = value; }
		}

		public override void Flush()
		{
			_baseStream.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _baseStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			if (origin == SeekOrigin.Begin)
				return _baseStream.Seek(offset + _offset, SeekOrigin.Begin);
			return _baseStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			_baseStream.SetLength(value + _offset);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_baseStream.Write(buffer, offset, count);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				_baseStream.Dispose();
		}
	}
}