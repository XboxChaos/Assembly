using System;

namespace XBDMCommunicator.XDevkitSnub
{
	public class XboxConsole
	{
		public XboxConsole() => throw new NotSupportedException(SnubException.Message);

		public IXboxDebugTarget DebugTarget
		{
			get { throw new NotSupportedException(SnubException.Message); }
		}

		public uint OpenConnection(object _) => throw new NotSupportedException(SnubException.Message);

		public uint CloseConnection(uint _) => throw new NotSupportedException(SnubException.Message);

		public string ConsoleType => throw new NotSupportedException(SnubException.Message);

		public void SendTextCommand(uint code, string command, out string resp)
			=> throw new NotSupportedException(SnubException.Message);

		public void ScreenShot(string _) => throw new NotSupportedException(SnubException.Message);
	}
}
