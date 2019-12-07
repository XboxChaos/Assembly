using System;

namespace XBDMCommunicator.XDevkitSnub
{
	class XboxDebugTarget : IXboxDebugTarget
	{
		public void GetMemory(uint pos, uint count, byte[] buffer, out uint bytesRead)
			=> throw new NotSupportedException(SnubException.Message);

		public void Go(out bool alreadyStopped)
			=> throw new NotSupportedException(SnubException.Message);

		public void SetMemory(uint pos, uint count, byte[] buffer, out uint bytesWritten)
			=> throw new NotSupportedException(SnubException.Message);

		public void Stop(out bool alreadyStopped) => throw new NotSupportedException(SnubException.Message);
	}
}
