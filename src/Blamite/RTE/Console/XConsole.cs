using Blamite.IO;
using Blamite.RTE.Console.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Net;

namespace Blamite.RTE.Console
{
	public enum RebootType
	{
		/// <summary>
		/// Full reboot
		/// </summary>
		Cold,
		/// <summary>
		/// Soft reboot
		/// </summary>
		Soft,
		/// <summary>
		/// Reboot current title softly
		/// </summary>
		Title
	}

	/// <summary>
	/// Base class for communicating with an Xbox/360 console over network.
	/// </summary>
	public abstract class XConsole
	{
		public abstract Endian Endianness { get; }
		internal TcpClient _tcpc;
		internal NetworkStream _netStream;
		public abstract int Port { get; }
		public abstract RTEConnectionType ConnectionType { get; }
		private string _runtimeIP;

		public string Identifier { get; private set; }

		public ConsoleMemoryStream ConsoleStream { get; }

		public XConsole(string identifier)
		{
			UpdateIdentifier(identifier);
			ConsoleStream = new ConsoleMemoryStream(this);
		}

		public XConsole(string identifier, uint forceOffset)
		{
			UpdateIdentifier(identifier);
			ConsoleStream = new ConsoleMemoryStream(this, forceOffset);
		}

		public void UpdateIdentifier(string identifier)
		{
			_runtimeIP = null;
			Identifier = identifier;
		}

		#region public commands
		/// <summary>
		/// Attempts to connect to the console with the set IP Address
		/// </summary>
		/// <returns>If a connection was successful</returns>
		public bool Connect()
		{
			ConfirmIndentifier();
			if (_runtimeIP == null)
				return false;

			_tcpc = new TcpClient();
			_tcpc.Client.ReceiveTimeout = 1000;
			_tcpc.Client.SendTimeout = 1000;

			if (!_tcpc.ConnectAsync(_runtimeIP, Port).Wait(1000))
			{
				//throw out stored ip
				_runtimeIP = null;
				return false;
			}

			_netStream = _tcpc.GetStream();

			string resp = RecieveSimple();
			if (!resp.StartsWith("201-"))
				return false;

			return true;
		}

		/// <summary>
		/// Attempts to disconnect to the connected console
		/// </summary>
		public void Disconnect()
		{
			try
			{
				SendSimpleCommand("bye");
			}
			catch { }

			_netStream?.Close();
			_tcpc?.Close();
		}

		/// <summary>
		/// Tells the console to temporarily stop everything
		/// </summary>
		public bool Stop()
		{
			if (!Connect())
				return false;

			StopInternal();
			Disconnect();
			return true;
		}

		/// <summary>
		/// Tells the console to resume after a previous stop
		/// </summary>
		public bool Go()
		{
			if (!Connect())
				return false;

			GoInternal();
			Disconnect();
			return true;
		}

		/// <summary>
		/// Reboots the console using the given reboot type
		/// </summary>
		/// <param name="rebootType">The kind of reboot to perform</param>
		public bool Reboot(RebootType rebootType)
		{
			if (!Connect())
				return false;

			RebootInternal(rebootType);
			Disconnect();
			return true;
		}

		/// <summary>
		/// Reads data from the console's memory
		/// </summary>
		/// <param name="address">The location to read</param>
		/// <param name="length">The amount of bytes to read</param>
		/// <returns>The data, if read was successful</returns>
		public byte[] ReadMemory(uint address, uint length)
		{
			if (!Connect())
				return null;
		
			byte[] data = ReadMemoryInternal(address, length, out uint bytesRead);
		
			Disconnect();
			return data;
		}

		/// <summary>
		/// Writes data to the console's memory
		/// </summary>
		/// <param name="address">The location to write to</param>
		/// <param name="data">The bytes to write</param>
		/// <returns>If the write was successful</returns>
		public bool WriteMemory(uint address, int length, byte[] data)
		{
			if (!Connect())
				return false;
		
			bool result = WriteMemoryInternal(address, length, data, out uint bytesWritten);
		
			Disconnect();
			return result;
		}

		/// <summary>
		/// Grabs a screenshot of the console's frame buffer
		/// </summary>
		/// <param name="freezeDuring">Whether to send additional stop and go commands to improve screenshot quality</param>
		/// <returns>A container object for the screenshot, or null.</returns>
		public Screenshot GetScreenshot(bool freezeDuring = false)
		{
			if (!Connect())
				return null;

			// Stop the Console
			if (freezeDuring)
				StopInternal();

			Screenshot shot = GetScreenshotInternal();

			// Start the Console
			if (freezeDuring)
				GoInternal();

			Disconnect();
			return shot;
		}

		/// <summary>
		/// Gets the path of the currently running title on the console
		/// </summary>
		/// <returns></returns>
		public string GetRunningTitle()
		{
			if (!Connect())
				return null;

			string title = GetRunningTitleInternal();
			Disconnect();
			return title;
		}

		/// <summary>
		/// Sets the console's time with the system time
		/// </summary>
		public bool SetSystemTime()
		{
			if (!Connect())
				return false;

			SetSystemTimeInternal();
			Disconnect();
			return true;
		}
		#endregion

		#region internal commands
		protected void StopInternal()
		{
			SendSimpleCommand("stop");
		}

		protected void GoInternal()
		{
			SendSimpleCommand("go");
		}

		internal abstract byte[] ReadMemoryInternal(uint address, uint length, out uint bytesRead);

		internal bool WriteMemoryInternal(uint address, int length, byte[] data, out uint bytesWritten)
		{
			int bufferMax = 240;
			bytesWritten = 0;

			while (bytesWritten < length)
			{
				int remainderTest = length - (int)bytesWritten;

				byte[] buffer = new byte[((remainderTest < bufferMax) ? remainderTest : bufferMax)];
				Array.Copy(data, bytesWritten, buffer, 0, (remainderTest < bufferMax) ? remainderTest : bufferMax);
				uint adjustedAddr = address + bytesWritten;

				string response = SendSimpleCommand(string.Format("setmem addr=0x{0} data={1}", adjustedAddr.ToString("X2"), BitConverter.ToString(buffer).Replace("-", "")));
				if (response == null)
					return false;

				string[] returnSplit = SplitStringResponse(response);

				int parsedVal = int.Parse(returnSplit[2]);
				if (parsedVal == 0)
					return false;

				bytesWritten += (uint)parsedVal;
			}

			return true;
		}

		protected Screenshot GetScreenshotInternal()
		{
			if (!TrySendCommand("screenshot"))
				return null;

			using (BinaryReader br = new BinaryReader(_netStream, Encoding.Default, true))
			{
				string response = ReadStringFromStream(br);

				if (!response.StartsWith("203-"))
					throw new Exception("Expected a binary response but didn't get one.");

				string combinedInfo = ReadStringFromStream(br);

				FormattedResponse resp = new FormattedResponse();
				resp.ParseNumberValues(combinedInfo);

				Screenshot shot = new Screenshot(resp);
				uint length = (uint)shot.FrameBufferSize;

				byte[] binary = RecieveBinary(length);

				shot.Data = binary;

				return shot;
			}
		}

		protected string GetRunningTitleInternal()
		{
			string[] info = SendListCommand("xbeinfo running");

			FormattedResponse resp = new FormattedResponse();
			resp.ParseNumberValues(info[0]);
			resp.ParseStringValue(info[1]);

			return resp.FindStringValue("name");
		}

		internal abstract void RebootInternal(RebootType rebootType);

		internal void SetSystemTimeInternal()
		{
			long time = DateTime.Now.ToFileTime();

			SendSimpleCommand(string.Format("setsystime clockhi=0x{0:X8} clocklo=0x{1:X8}", time >> 32, time & 0xFFFFFFFF));
		}
		#endregion

		#region basic command handling
		protected bool TrySendCommand(string command)
		{
			byte[] cmdBytes = Encoding.ASCII.GetBytes(command + "\r\n");

			try
			{
				int sent = _tcpc.Client.Send(cmdBytes);
				if (sent == cmdBytes.Length)
					return true;
			}
			catch { }

			return false;
		}

		protected string SendSimpleCommand(string command)
		{
			if (TrySendCommand(command))
				return RecieveSimple();
			else
				return null;
		}

		protected string RecieveSimple()
		{
			using (BinaryReader br = new BinaryReader(_netStream, Encoding.Default, true))
			{
				return ReadStringFromStream(br);
			}
		}

		protected string[] SendListCommand(string command)
		{
			if (!TrySendCommand(command))
				return null;

			using (BinaryReader br = new BinaryReader(_netStream, Encoding.Default, true))
			{
				List<string> contents = new List<string>();

				string response = ReadStringFromStream(br);

				if (!response.StartsWith("202-"))
					throw new Exception("Expected a list response but didn't get one.");

				while (true)
				{
					string item = ReadStringFromStream(br);
					if (item == ".")
						break;

					contents.Add(item);
				}

				return contents.ToArray();
			}
		}

		protected string SendBinaryCommand(string command, uint expectedLength, bool prefixed, out byte[] binary)
		{
			binary = null;
			string response = "";

			if (!TrySendCommand(command))
				return null;

			int offset = prefixed ? 2 : 0;

			using (BinaryReader br = new BinaryReader(_netStream, Encoding.Default, true))
			{
				response = ReadStringFromStream(br);

				if (!response.StartsWith("203-"))
					throw new Exception("Expected a further response but didn't get one.");

				binary = RecieveBinary(expectedLength, offset);
			}

			return response;
		}

		protected byte[] RecieveBinary(uint length, int prefixOffset = 0)
		{
			byte[] binary = new byte[length];

			uint recievedBytes = 0;
			while (recievedBytes < length)
			{
				byte[] buffer = new byte[1026];

				int bytesRead = _netStream.Read(buffer, 0, buffer.Length);

				Array.Copy(buffer, prefixOffset, binary, recievedBytes, bytesRead - prefixOffset);

				recievedBytes += (uint)(bytesRead - prefixOffset);
			}
			return binary;
		}
		#endregion

		#region command helpers
		protected string ReadStringFromStream(BinaryReader br)
		{
			string output = "";
			char c;

			for (int j = 0; j < 512; j++)
			{
				c = br.ReadChar();
				if (c == 0x0D)
				{
					char d = br.ReadChar();
					if (d == 0x0A)
						break;
				}

				output += c.ToString();
			}

			return output;
		}

		protected string[] SplitStringResponse(string response, string splitBy = " ")
		{
			return response.Split(splitBy.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
		}

		#endregion

		private void ConfirmIndentifier()
		{
			if (_runtimeIP == null)
			{
				if (string.IsNullOrEmpty(Identifier))
					return;

				if (IsIPAddress(Identifier))
					_runtimeIP = Identifier;
				else
				{
					string resolved = ResolveConsoleName(Identifier, Port);
					if (!string.IsNullOrEmpty(resolved))
						_runtimeIP = resolved;
				}
			}
		}

		private static bool IsIPAddress(string ip)
		{
			string[] split = ip.Split('.');
			if (split.Length == 4)
				return IPAddress.TryParse(ip, out _);

			return false;
		}

		private static string ResolveConsoleName(string name, int port)
		{
			UdpClient udpc = new UdpClient();
			udpc.EnableBroadcast = true;
			udpc.Client.ReceiveTimeout = 1000;
			byte[] namebytes = Encoding.ASCII.GetBytes("\0\0" + name);
			namebytes[0] = 1;
			namebytes[1] = (byte)name.Length;
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Broadcast, port);

			udpc.Send(namebytes, namebytes.Length, endpoint);

			try
			{
				byte[] data = udpc.Receive(ref endpoint);
				if (data.Length > 0)
					return endpoint.Address.ToString();
			}
			catch { }
			finally
			{
				udpc.Close();
			}

			return null;
		}
	}
}
