namespace XBDMCommunicator.XDevkitStub
{
	public class XboxConsole
	{
		public XboxConsole()
		{
			throw new StubException();
		}

		public IXboxDebugTarget DebugTarget
		{
			get { throw new StubException(); }
		}

		public uint OpenConnection(object _)
		{
			throw new StubException();
		}

		public uint CloseConnection(uint _)
		{
			throw new StubException();
		}

		public string ConsoleType
		{
			get { throw new StubException(); }
		}

		public void SendTextCommand(uint code, string command, out string resp)
		{
			throw new StubException();
		}

		public void ScreenShot(string _)
		{
			throw new StubException();
		}
	}
}
