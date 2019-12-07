using System;

namespace XBDMCommunicator.XDevkitSnub
{
	public class XboxManager : IXboxManager
	{
		public XboxManager() => throw new NotSupportedException(SnubException.Message);

		public XboxConsole OpenConsole(string _ = null) => throw new NotSupportedException(SnubException.Message);
	}
}
