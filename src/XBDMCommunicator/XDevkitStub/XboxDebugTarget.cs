namespace XBDMCommunicator.XDevkitStub
{
	class XboxDebugTarget : IXboxDebugTarget
	{
		public void GetMemory(uint pos, uint count, byte[] buffer, out uint bytesRead)
		{
			throw new StubException();
		}

		public void Go(out bool alreadyStopped)
		{
			throw new StubException();
		}

		public void SetMemory(uint pos, uint count, byte[] buffer, out uint bytesWritten)
		{
			throw new StubException();
		}

		public void Stop(out bool alreadyStopped)
		{
			throw new StubException();
		}
	}
}
