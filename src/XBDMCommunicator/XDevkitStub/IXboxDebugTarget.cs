namespace XBDMCommunicator.XDevkitStub
{
	public interface IXboxDebugTarget
	{
		void Stop(out bool alreadyStopped);

		void GetMemory(uint pos, uint count, byte[] buffer, out uint bytesRead);

		void SetMemory(uint pos, uint count, byte[] buffer, out uint bytesWritten);

		void Go(out bool alreadyStopped);
	}
}
