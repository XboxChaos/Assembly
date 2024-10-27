using System;
using System.IO;
using System.Timers;

namespace Blamite.RTE.Console
{
	public class ConsoleMemoryStream : Stream
	{
		// Private Modifiers
		private readonly XConsole _console;
		private uint? _cachedAddress;
		private byte[] _cachedData;

		private Timer _staleTimer;

		private uint _forceOffset;

		public ConsoleMemoryStream(XConsole console, uint forceOffset = 0)
		{
			_console = console;
			Position = 0;
			_forceOffset = forceOffset;

			_staleTimer = new Timer();
			_staleTimer.Interval = 5000;//5 seconds
			_staleTimer.Elapsed += StaleTimer_Elapsed;
			_staleTimer.AutoReset = true;
		}

		// IO Functions
		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return true; }
		}

		public override bool CanWrite
		{
			get { return true; }
		}

		public override long Length
		{
			get { return 0x100000000; }
		}

		public override sealed long Position { get; set; }

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			//now with caching to match xdevkit/xbdm and avoid excess commands being sent

			count = Math.Min(count, buffer.Length - offset); // Make sure we don't overflow the buffer

			if (count == 0)
				return 0;

			//stop the timer so data isnt cleared mid-read
			_staleTimer.Stop();

			//check if cached
			uint clippedAddress = (uint)Position & 0xFFFFF000;
			uint clippedLength = ((uint)count + 0x1000) & 0xFFFFF000;
			uint cacheOffset = (uint)Position - clippedAddress;

			if (_cachedAddress.HasValue && _cachedAddress == clippedAddress)
			{
				if (cacheOffset + count < _cachedData.Length)
				{
					Buffer.BlockCopy(_cachedData, (int)cacheOffset, buffer, offset, count);
					Position += count;
					return count;
				}
			}

			//cache didnt match, nuke it
			ClearCache();

			uint bytesRead;
			if (!_console.Connect())
			{
				_staleTimer.Start();
				return 0;
			}
				
			var tempBuffer = _console.ReadMemoryInternal(clippedAddress, clippedLength, out bytesRead);
			if (bytesRead == 0)
			{
				_staleTimer.Start();
				_console.Disconnect();
				return 0;
			}

			Buffer.BlockCopy(tempBuffer, (int)cacheOffset, buffer, offset, count);
			Position += count;

			//cache the data
			_cachedAddress = clippedAddress;
			_cachedData = tempBuffer;

			_staleTimer.Start();
			_console.Disconnect();

			return (int)bytesRead;

		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
				case SeekOrigin.Begin:
					Position = offset + _forceOffset;
					break;
		
				case SeekOrigin.Current:
					Position += offset;
					break;
		
				case SeekOrigin.End:
					Position = 0x100000000 - offset;
					break;
			}
			return Position;
		}

		public override void SetLength(long value)
		{
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			count = Math.Min(count, buffer.Length - offset); // Make sure we don't read beyond the buffer

			if (count == 0 || !_console.Connect())
				return;

			byte[] pokeArray = buffer;
			if (offset != 0)
			{
				// Offset isn't 0, so copy into a second buffer before poking
				pokeArray = new byte[count];
				Buffer.BlockCopy(buffer, offset, pokeArray, 0, count);
			}
		
			uint bytesWritten;
			_console.WriteMemoryInternal((uint)Position, count, pokeArray, out bytesWritten);
			Position += bytesWritten;

			_console.Disconnect();
		}

		private void StaleTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			ClearCache();
		}

		private void ClearCache()
		{
			_cachedAddress = null;
			_cachedData = null;
		}

	}
}
