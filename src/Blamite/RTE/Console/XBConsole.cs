using Blamite.IO;
using Blamite.RTE;
using System;

namespace Blamite.RTE.Console
{
	/// <summary>
	/// An original Xbox console.
	/// </summary>
	public class XbConsole : XConsole
	{
		public override Endian Endianness { get { return Endian.LittleEndian; } }
		public override int Port { get { return 731; } }
		public override RTEConnectionType ConnectionType { get { return RTEConnectionType.ConsoleXbox; } }

		public XbConsole(string identifier) : base(identifier) { }

		internal override byte[] ReadMemoryInternal(uint address, uint length, out uint bytesRead)
		{
			SendBinaryCommand(string.Format("getmem2 addr=0x{0} length=0x{1}", address.ToString("X2"), length.ToString("X2")),
				length, false, out byte[] result);

			bytesRead = (uint)result.Length;

			return result;
		}

		internal override void RebootInternal(RebootType rebootType)
		{
			switch (rebootType)
			{
				//old xbdm code used "reboot" for both cold and title?
				case RebootType.Cold:
					SendSimpleCommand("reboot cold");
					break;
				case RebootType.Soft:
					SendSimpleCommand("reboot warm");
					break;
				case RebootType.Title:
					string title = GetRunningTitleInternal();
					if (!string.IsNullOrEmpty(title))
						SendSimpleCommand("magicboot title=\"" + title + "\" debug");
					else
						SendSimpleCommand("reboot warm");//couldn't get the title, just do a soft boot

					break;
			}
		}
	}
}
