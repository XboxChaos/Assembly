﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Blamite.RTE.PC.Native
{
	/// <summary>
	///     A Stream which reads/writes another process's memory.
	/// </summary>
	public class ProcessModuleMemoryStream : Stream
	{
		private readonly Process _process;
		private readonly ProcessModule _processModule;

		/// <summary>
		///     Constructs a new ProcessMemoryStream that accesses the memory of a specified process.
		/// </summary>
		/// <param name="process">The process to access the memory of.</param>
		/// <param name="module">The process module to access the memory of (from the given process)</param>
		public ProcessModuleMemoryStream(Process process, ProcessModule module)
		{
			_process = process;

			_processModule = module;

			Position = (long)_processModule.BaseAddress;
		}

		/// <summary>
		///     Gets the process that the stream operates on.
		/// </summary>
		public Process BaseProcess
		{
			get { return _process; }
		}

		public ProcessModule BaseModule
		{
			get { return _processModule; }
		}

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
			get { return uint.MaxValue; /* FIXME: This is really hackish... */ }
		}

		public override long Position { get; set; }

		public override void Flush()
		{
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
				case SeekOrigin.Begin:
					Position = offset;
					break;

				case SeekOrigin.Current:
					Position += offset;
					break;

				case SeekOrigin.End:
					Position = uint.MaxValue - offset;
					break;
			}
			return Position;
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override unsafe int Read(byte[] buffer, int offset, int count)
		{
			UIntPtr bytesRead;

			count = Math.Min(count, buffer.Length - offset); // Make sure we don't overflow the buffer
			fixed (byte* pBuffer = buffer)
			{
				// This requires unsafe mode just to make the buffer operation faster
				// Otherwise we have to duplicate it and slow everything down
				// An alternative probably needs to be made in case a program doesn't want to use it...
				ReadProcessMemory(_process.Handle, (IntPtr) Position, pBuffer + offset, (UIntPtr) count, out bytesRead);
			}

			Position += (long) bytesRead;
			return (int) bytesRead;
		}

		public override unsafe void Write(byte[] buffer, int offset, int count)
		{
			UIntPtr bytesWritten;

			count = Math.Min(count, buffer.Length - offset); // Make sure we don't read beyond the buffer
			fixed (byte* pBuffer = buffer)
			{
				// This requires unsafe mode just to make the buffer operation faster
				// Otherwise we have to duplicate it and slow everything down
				// An alternative probably needs to be made in case a program doesn't want to use it...
				WriteProcessMemory(_process.Handle, (IntPtr) Position, pBuffer + offset, (UIntPtr) count, out bytesWritten);
			}

			Position += (long) bytesWritten;
		}

		#region Native Functions

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern unsafe bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte* lpBuffer,
			UIntPtr nSize, out UIntPtr lpNumberOfBytesRead);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern unsafe bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte* lpBuffer,
			UIntPtr nSize, out UIntPtr lpNumberOfBytesWritten);

		#endregion Native Functions
	}
}