using System;

namespace XBDMCommunicator.XDevkitStub
{
	public class StubException : NotSupportedException
	{
		public StubException()
			: base("In order to use this feature, Assembly must be built with the XDevkit build configuration.")
		{ }
	}
}
