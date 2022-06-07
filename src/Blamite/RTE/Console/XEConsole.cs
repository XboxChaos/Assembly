using Blamite.IO;
using Blamite.RTE;
using System;
using System.IO;

namespace Blamite.RTE.Console
{
	/// <summary>
	/// An Xbox 360 console.
	/// </summary>
	public class XEConsole : BaseConsole
	{
		public override Endian Endianness { get { return Endian.BigEndian; } }
		public override int Port { get { return 730; } }
		public override RTEConnectionType ConnectionType { get { return RTEConnectionType.ConsoleXbox360; } }

		public XEConsole(string identifier) : base(identifier) { }

		internal override byte[] ReadMemoryInternal(uint address, uint length, out uint bytesRead)
		{
			SendBinaryCommand(string.Format("getmemex addr=0x{0} length=0x{1}", address.ToString("X2"), length.ToString("X2")),
				length, true, out byte[] result);

			bytesRead = (uint)result.Length;

			return result;
		}

		internal override void RebootInternal(RebootType rebootType)
		{
			switch (rebootType)
			{
				//old xbdm code used "reboot" for both cold and title?
				case RebootType.Cold:
					SendSimpleCommand("magicboot cold");
					break;
				case RebootType.Soft:
					SendSimpleCommand("magicboot");
					break;
				case RebootType.Title:
					string title = GetRunningTitleInternal();
					if (!string.IsNullOrEmpty(title))
					{
						string dir = Path.GetDirectoryName(title);
						SendSimpleCommand("magicboot title=\"" + title + "\" directory=\"" + dir + "\"");
					}
					else
						SendSimpleCommand("magicboot");//couldn't get the title, just do a soft boot

					break;
			}
		}
	}
}
